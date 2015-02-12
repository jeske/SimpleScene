using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;

using SimpleScene.Util;

namespace SimpleScene
{
    public class SSParticle
    {
		public static readonly Color4 c_defaultColor = Color4.White;
		public const float c_defaultMass = 1f;

        public float Life = 0f;
        public Vector3 Pos = new Vector3(0f);
        public float MasterScale = 1f;
        public Vector3 ComponentScale = new Vector3(1f);
        public Vector3 Vel = new Vector3(0f);
        public Color4 Color = c_defaultColor;
		public float Mass = c_defaultMass;
        public float ViewDepth = float.PositiveInfinity;

        // TODO texture coord, orientation

        #if false
        public Particle (float life, Vector3 pos, Vector3 vel, Color4 color, float mass) 
		{
        // TODO: scale
			Life = life;
			Pos = pos;
			Vel = vel;
			Color = color;
			Mass = mass;
		}
        #endif
    }

    /// <summary>
    /// Particle system simulates a number of particles.
    /// Particles are emitted by SSParticleEmitter's, which are responsible for assigning particle properties
    /// (inial position, velocity, color, etc.)
    /// Particles' position is updated during simulation to advance by their individual velocity
    /// For more advanced effects on particles (simulate gravity, fields, etc.) SSParticleEffector's are used
    /// </summary>

    public class SSParticleSystem
    {
        protected static readonly SSAttributePos c_notAPosition = new SSAttributePos(new Vector3 (float.NaN));
        protected static Random s_rand = new Random(); // for quicksorting

        // TODO bounding sphere or cube
        protected List<SSParticleEmitter> m_emitters = new List<SSParticleEmitter> ();
        protected List<SSParticleEffector> m_effectors = new List<SSParticleEffector> ();

        protected readonly int m_capacity;
        protected int m_numParticles = 0;

        #region required particle data
        protected readonly float[] m_lives;  // unused particles will be hacked to not draw
        protected int m_nextIdxToWrite;      // change to UintPtr?
        protected int m_nextIdxToOverwrite;
        protected int m_activeBlockLength;
        #endregion

        #region particle data sent to the GPU
        protected SSAttributePos[] m_positions;
        protected SSAttributeColor[] m_colors;
        protected SSAttributeMasterScale[] m_masterScales;
        protected SSAttributeComponentScale[] m_componentScales;
        #endregion

        #region particle data used for the simulation only
        protected Vector3[] m_velocities;
        protected float[] m_masses;
        protected float[] m_viewDepths;
        // TODO Orientation
        // TODO Texture Coord
        #endregion

        public int Capacity { get { return m_capacity; } }
        public int ActiveBlockLength { get { return m_activeBlockLength; } }
        public SSAttributePos[] Positions { get { return m_positions; } }
        public SSAttributeColor[] Colors { get { return m_colors; } }
        public SSAttributeMasterScale[] MasterScales { get { return m_masterScales; } }
        public SSAttributeComponentScale[] ComponentScales { get { return m_componentScales; } }

        public SSParticleSystem (int capacity)
        {
            m_capacity = capacity;
            m_lives = new float[m_capacity];
            Reset();
        }

        public void Reset()
        {
            m_nextIdxToWrite = 0;
            m_nextIdxToOverwrite = 0;
            m_activeBlockLength = 0;

            m_positions = new SSAttributePos[1];
            m_masterScales = new SSAttributeMasterScale[1];
            m_componentScales = new SSAttributeComponentScale[1];
            m_colors = new SSAttributeColor[1];
            m_velocities = new Vector3[1];
            m_masses = new float[1];
            m_viewDepths = new float[1];

            writeParticle(0, new SSParticle ()); // fill in default values

            foreach (SSParticleEmitter emitter in m_emitters) {
                emitter.Reset();
            }
            foreach (SSParticleEffector effector in m_effectors) {
                effector.Reset();
            }
        }

        public void EmitAll()
        {
            foreach (SSParticleEmitter e in m_emitters) {
                e.EmitParticles(storeNewParticle);
            }
        }

        public void AddEmitter(SSParticleEmitter emitter)
        {
            m_emitters.Add(emitter);
        }

        public void AddEffector(SSParticleEffector effector)
        {
            m_effectors.Add(effector);
        }

        public void Simulate(float timeDelta)
        {
            SSParticle p = new SSParticle ();
            for (int i = 0; i < m_activeBlockLength; ++i) {
                if (isAlive(i)) {
                    // Alive particle
                    m_lives [i] -= timeDelta;
                    if (isAlive(i)) {
                        // Still alive. Update position and run through effectors
                        readParticle(i, p);
                        p.Pos += p.Vel * timeDelta;
                        foreach (SSParticleEffector effector in m_effectors) {
                            effector.Simulate(p, timeDelta);
                        }
                        writeParticle(i, p);
                    } else {
                        // Particle just died. Hack to not draw?
                        writeDataIfNeeded(ref m_positions, i, c_notAPosition);
                        if (m_numParticles == m_capacity || i < m_nextIdxToWrite) {
                            // released slot will be the next one to be written to
                            m_nextIdxToWrite = i;
                        }
                        if (i == m_activeBlockLength - 1) {
                            // reduction in the active particles block
                            while (m_activeBlockLength > 0 && isDead(m_activeBlockLength - 1)) {
                                --m_activeBlockLength;
                            }
                        }
                        --m_numParticles;
                        if (m_numParticles == 0) {
                            // all particles gone. reset write and overwrite locations for better packing
                            m_nextIdxToWrite = 0;
                            m_nextIdxToOverwrite = 0;
                            m_activeBlockLength = 0;
                        }
                    }
                }
            }

            foreach (SSParticleEmitter emitter in m_emitters) {
                emitter.Simulate(timeDelta, storeNewParticle);
            }
        }

        public void SortByDepth(ref Matrix4 viewMatrix)
        {
            if (m_numParticles == 0) return;

            for (int i = 0; i < m_activeBlockLength; ++i) {
                if (isAlive(i)) {
                    // Do the transform and store z of the result
                    Vector3 pos = readData(m_positions, i).Position;
                    pos = Vector3.Transform(pos, viewMatrix);
                    writeDataIfNeeded(ref m_viewDepths, i, pos.Z);
                } else {
                    // since we are doing a sort pass later, might as well make it so
                    // so the dead particles get pushed the back of the arrays
                    writeDataIfNeeded(ref m_viewDepths, i, float.PositiveInfinity);
                }
            }

            quickSort(0, m_activeBlockLength - 1);

            if (m_numParticles < m_capacity) {
                // update pointers to reflect dead particles that just got sorted to the back
                m_nextIdxToWrite = m_numParticles - 1;
                m_activeBlockLength = m_numParticles;
            }

            #if false
            // set color based on the index of the particle in the arrays
            SSAttributeColor[] debugColors = {
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Red)),
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Green)),
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Blue)),
                new SSAttributeColor(OpenTKHelper.Color4toRgba(Color4.Yellow))
            };

            for (int i = 0; i < m_activeBlockLength; ++i) {
                writeDataIfNeeded(ref m_colors, i, debugColors[i]);
            }
            #endif
        }

        protected virtual void storeNewParticle(SSParticle newParticle)
        {
            int writeIdx;
            if (m_numParticles == m_capacity) {
                writeIdx = m_nextIdxToOverwrite;
                m_nextIdxToOverwrite = nextIdx(m_nextIdxToOverwrite);
            } else {
                while (isAlive(m_nextIdxToWrite)) {
                    m_nextIdxToWrite = nextIdx(m_nextIdxToWrite);
                }
                writeIdx = m_nextIdxToWrite;
                if (writeIdx + 1 >= m_activeBlockLength) {
                    m_activeBlockLength = writeIdx + 1;
                }
                m_nextIdxToWrite = nextIdx(m_nextIdxToWrite);
                m_numParticles++;
            }
            writeParticle(writeIdx, newParticle);
        }

        protected int nextIdx(int idx) 
        {
            ++idx;
            if (idx == m_capacity) {
                idx = 0;
            }
            return idx;
        }

        protected bool isDead(int idx) {
            return m_lives [idx] <= 0f;
        }

        protected bool isAlive(int idx) {
            return !isDead(idx);
        }

        protected T readData<T>(T[] array, int idx)
        {
            if (idx >= array.Length) {
                return array [0];
            } else {
                return array [idx];
            }
        }

        protected virtual void readParticle (int idx, SSParticle p) {
            p.Life = m_lives [idx];
            p.Pos = readData(m_positions, idx).Position;
            p.ViewDepth = readData(m_viewDepths, idx);

            if (p.Life <= 0f) return; // the rest does not matter

            p.MasterScale = readData(m_masterScales, idx).Scale;
            p.ComponentScale = readData(m_componentScales, idx).Scale;
            p.Color = OpenTKHelper.RgbaToColor4(readData(m_colors, idx).Color);
            p.Vel = readData(m_velocities, idx);
            p.Mass = readData(m_masses, idx);
        }

        protected void writeDataIfNeeded<T>(ref T[] array, int idx, T value) where T : IEquatable<T>
        {
            bool write = true;
            if (idx > 0 && array.Length == 1) {
                T masterVal = array [0];
                if (masterVal.Equals(value)) {
                    write = false;
                } else {
                    // allocate the array to keep track of different values
                    array = new T[m_capacity];
                    for (int i = 0; i < m_activeBlockLength; ++i) {
                        array [i] = masterVal;
                    }
                }
            }
            if (write) {
                array [idx] = value;
            }
        }

        protected virtual void writeParticle(int idx, SSParticle p) {
            m_lives [idx] = p.Life;
            writeDataIfNeeded(ref m_positions, idx, 
                new SSAttributePos(p.Pos));
            writeDataIfNeeded(ref m_masterScales, idx,
                new SSAttributeMasterScale (p.MasterScale));
            writeDataIfNeeded(ref m_componentScales, idx,
                new SSAttributeComponentScale (p.ComponentScale));
            writeDataIfNeeded(ref m_colors, idx, 
                new SSAttributeColor(OpenTKHelper.Color4toRgba(p.Color)));
            writeDataIfNeeded(ref m_velocities, idx, p.Vel);
            writeDataIfNeeded(ref m_masses, idx, p.Mass);
            writeDataIfNeeded(ref m_viewDepths, idx, p.ViewDepth); 
        }

        // The alternative to re-implementing quicksort appears to be implementing IList interface with
        // about 10 function implementations needed, some of which may be messy or not make sense for the
        // current data structure. For now lets implement quicksort and see how it does
        protected void quickSort(int leftIdx, int rightIdx)
        {
            if (leftIdx < rightIdx) {
                int pi = quicksortPartition(leftIdx, rightIdx);
                quickSort(leftIdx, pi - 1);
                quickSort(pi + 1, rightIdx);
            }   
        }

        protected int quicksortPartition(int leftIdx, int rightIdx)
        {
            int pivot = s_rand.Next(leftIdx, rightIdx);
            particleSwap(pivot, rightIdx);
            int store = leftIdx;

            for (int i = leftIdx; i < rightIdx; ++i) {
                float iDepth = readData(m_viewDepths, i);
                float rightDepth = readData(m_viewDepths, rightIdx);
                // or <= ?
                if (iDepth < rightDepth) {
                    particleSwap(i, store);
                    store++;
                }
            }
            particleSwap(store, rightIdx);
            return store;
        }

        protected void particleSwap(int leftIdx, int rightIdx)
        {
            // TODO Consider swaping on a per component basis. 
            // It may have better peformance
            // But adds more per-component maintenance
            SSParticle leftParticle = new SSParticle ();
            SSParticle rightParticle = new SSParticle();
            readParticle(leftIdx, leftParticle);
            readParticle(rightIdx, rightParticle);

            // write in reverse
            writeParticle(leftIdx, rightParticle);
            writeParticle(rightIdx, leftParticle);
        }
    }
}

