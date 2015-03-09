using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using System.Drawing;

using SimpleScene.Util;

namespace SimpleScene
{
    public class SSParticle
    {
        public const int c_maxSupportedSpritePresets = 8;

        public float Life = 1f;
        public Vector3 Pos = new Vector3(0f);
        public Vector3 Vel = new Vector3(1f);
        public Vector3 Orientation;
        public Vector3 AngularVelocity;
        public float MasterScale = 1f;
        public Vector3 ComponentScale = new Vector3(1f);
        public Color4 Color = Color4.White;
        public float Mass = 1.0f;
		public float RotationalInnertia = 1.0f;
		public float Drag = 0.0f;
		public float RotationalDrag = 0.0f;
        public float ViewDepth = float.PositiveInfinity;

        // when not -1 (255) means use sprite location preset as a source of UV for the current particle
		//public byte SpriteIndex = byte.MaxValue;

        // when not NaN means the values are used as a sorce of UV for the current particles
        public RectangleF SpriteRect = new RectangleF (0f, 0f, 1f, 1f);
        // ^ if both indexed and custom uv values are specified they will be added in the shader
        // TODO orientation, effector mask

		public ushort EffectorMask = ushort.MaxValue;
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
        protected static readonly SSAttributeVec3 c_notAPosition = new SSAttributeVec3(new Vector3 (float.NaN));
        protected static Random s_rand = new Random(); // for quicksorting

		/// <summary>
		/// Defines a unit step for the thic particle system's simulation. 
		/// Higher values result in a more quanitized simulation but more processing
		/// </summary>
        public float SimulationStep = .010f;

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
        protected SSAttributeVec3[] m_positions;
		protected SSAttributeVec2[] m_orientationsXY;
		protected SSAttributeFloat[] m_orientationsZ;
        protected SSAttributeColor[] m_colors;
        protected SSAttributeFloat[] m_masterScales;
        protected SSAttributeVec2[] m_componentScalesXY;
		protected SSAttributeFloat[] m_componentScalesZ;

		//protected SSAttributeByte[] m_spriteIndices;
        protected SSAttributeFloat[] m_spriteOffsetsU;
        protected SSAttributeFloat[] m_spriteOffsetsV;
        protected SSAttributeFloat[] m_spriteSizesU;
        protected SSAttributeFloat[] m_spriteSizesV;
        #endregion

        #region particle data used for the simulation only
        protected Vector3[] m_velocities;
        protected Vector3[] m_angularVelocities;
        protected float[] m_masses;
		protected float[] m_rotationalInnertias;
		protected float[] m_drags;
		protected float[] m_rotationalDrags;
        protected float[] m_viewDepths;
		protected byte[] m_effectorMasksLow;
		protected byte[] m_effectorMasksHigh;
        // TODO Orientation
        // TODO Texture Coord
        #endregion

        protected float m_radius;
        protected float m_timeDeltaAccumulator;

        public int Capacity { get { return m_capacity; } }
        public int ActiveBlockLength { get { return m_activeBlockLength; } }
        public float Radius { get { return m_radius; } }
        public SSAttributeVec3[] Positions { get { return m_positions; } }
		public SSAttributeVec2[] OrientationsXY { get { return m_orientationsXY; } }
		public SSAttributeFloat[] OrientationsZ { get { return m_orientationsZ; } }
        public SSAttributeColor[] Colors { get { return m_colors; } }
        public SSAttributeFloat[] MasterScales { get { return m_masterScales; } }
        public SSAttributeVec2[] ComponentScalesXY { get { return m_componentScalesXY; } }
		public SSAttributeFloat[] ComponentScalesZ { get { return m_componentScalesZ; } }
		//public SSAttributeByte[] SpriteIndices { get { return m_spriteIndices; } }
        public SSAttributeFloat[] SpriteOffsetsU { get { return m_spriteOffsetsU; ; } }
        public SSAttributeFloat[] SpriteOffsetsV { get { return m_spriteOffsetsV; } }
        public SSAttributeFloat[] SpriteSizesU { get { return m_spriteSizesU; } }
        public SSAttributeFloat[] SpriteSizesV { get { return m_spriteSizesV; } }

        public SSParticleSystem (int capacity)
        {
            m_capacity = capacity;
            m_lives = new float[m_capacity];
            Reset();
        }

		/// <summary>
		/// This class tries faithfully to be resettable. One of the benefits may be being able to reset a
		/// particle system if working in a game editor.
		/// 
		/// Overriding this may be a good idea if you want dervied class to reset its member additions in 
		/// sync with the basic particle system reset. Should you chose to do so make sure to call
		/// this base function.
		/// </summary>
		public virtual void Reset()
        {
            m_nextIdxToWrite = 0;
            m_nextIdxToOverwrite = 0;
            m_activeBlockLength = 0;
			m_numParticles = 0;

			m_radius = 0f;
			m_timeDeltaAccumulator = 0f;

            m_positions = new SSAttributeVec3[1];
			m_orientationsXY = new SSAttributeVec2[1];
			m_orientationsZ = new SSAttributeFloat[1];
            m_masterScales = new SSAttributeFloat[1];
            m_componentScalesXY = new SSAttributeVec2[1];
			m_componentScalesZ = new SSAttributeFloat[1];
            m_colors = new SSAttributeColor[1];

			//m_spriteIndices = new SSAttributeByte[1];
            m_spriteOffsetsU = new SSAttributeFloat[1];
            m_spriteOffsetsV = new SSAttributeFloat[1];
            m_spriteSizesU = new SSAttributeFloat[1];
            m_spriteSizesV = new SSAttributeFloat[1];

            m_velocities = new Vector3[1];
            m_angularVelocities = new Vector3[1];
            m_masses = new float[1];
			m_rotationalInnertias = new float[1];
			m_drags = new float[1];
			m_rotationalDrags = new float[1];
            m_viewDepths = new float[1];
			m_effectorMasksLow = new byte[1];
			m_effectorMasksHigh = new byte[1];

            writeParticle(0, new SSParticle ()); // fill in default values
            for (int i = 0; i < m_capacity; ++i) {
                m_lives [i] = 0f;
            }

            foreach (SSParticleEmitter emitter in m_emitters) {
                emitter.Reset();
            }
            foreach (SSParticleEffector effector in m_effectors) {
                effector.Reset();
            }
        }

		public virtual void EmitAll()
        {
            foreach (SSParticleEmitter e in m_emitters) {
                e.EmitParticles(storeNewParticle);
            }
        }

		public virtual void Simulate(float timeDelta)
        {
            timeDelta += m_timeDeltaAccumulator;
            while (timeDelta >= SimulationStep) {
                simulateStep();
                timeDelta -= SimulationStep;
            }
            m_timeDeltaAccumulator = timeDelta;
        }

		public virtual void UpdateCamera(ref Matrix4 modelView, ref Matrix4 projection) { }

		public virtual void AddEmitter(SSParticleEmitter emitter)
        {
            emitter.Reset();
            m_emitters.Add(emitter);
        }

		public virtual void AddEffector(SSParticleEffector effector)
        {
            effector.Reset();
            m_effectors.Add(effector);
        }

        public void SortByDepth(ref Matrix4 viewMatrix)
        {
            if (m_numParticles == 0) return;

            for (int i = 0; i < m_activeBlockLength; ++i) {
                if (isAlive(i)) {
                    // Do the transform and store z of the result
                    Vector3 pos = readData(m_positions, i).Value;
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

		protected virtual void simulateStep()
        {
            m_radius = 0f;
            foreach (SSParticleEmitter emitter in m_emitters) {
                emitter.Simulate(SimulationStep, storeNewParticle);
            }
			foreach (SSParticleEffector effector in m_effectors) {
				effector.SimulateSelf (SimulationStep);
			}

            SSParticle p = new SSParticle ();
            for (int i = 0; i < m_activeBlockLength; ++i) {
                if (isAlive(i)) {
                    // Alive particle
                    m_lives [i] -= SimulationStep;
                    if (isAlive(i)) {
                        // Still alive. Update position and run through effectors
                        readParticle(i, p);
						p.Vel -= p.Drag * p.Vel / p.Mass;
						p.AngularVelocity -= p.RotationalDrag * p.AngularVelocity / p.RotationalInnertia;
                        p.Pos += p.Vel * SimulationStep;
                        p.Orientation += p.AngularVelocity * SimulationStep;
                        foreach (SSParticleEffector effector in m_effectors) {
							effector.SimulateParticleEffect(p, SimulationStep);
                        }
                        writeParticle(i, p);
                        float distFromOrogin = p.Pos.Length;
                        if (distFromOrogin > m_radius) {
                            m_radius = distFromOrogin;
                        }
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
                        }                        --m_numParticles;
                        if (m_numParticles == 0) {
                            // all particles gone. reset write and overwrite locations for better packing
                            m_nextIdxToWrite = 0;
                            m_nextIdxToOverwrite = 0;
                            m_activeBlockLength = 0;
                        }
                    }
                }
            }
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

			float distFromOrogin = newParticle.Pos.Length;
			if (distFromOrogin > m_radius) {
				m_radius = distFromOrogin;
			}
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
            p.Pos = readData(m_positions, idx).Value;
			p.Orientation.Xy = readData(m_orientationsXY, idx).Value;
			p.Orientation.Z = readData (m_orientationsZ, idx).Value;
            p.ViewDepth = readData(m_viewDepths, idx);

            if (p.Life <= 0f) return; // the rest does not matter

            p.MasterScale = readData(m_masterScales, idx).Value;
			p.ComponentScale.Xy = readData(m_componentScalesXY, idx).Value;
			p.ComponentScale.Z = readData (m_componentScalesZ, idx).Value;
			p.Color = Color4Helper.RgbaToColor4(readData(m_colors, idx).Color);

			//p.SpriteIndex = readData(m_spriteIndices, idx).Value;
            p.SpriteRect.X = readData(m_spriteOffsetsU, idx).Value;
            p.SpriteRect.Y = readData(m_spriteOffsetsV, idx).Value;
            p.SpriteRect.Width = readData(m_spriteSizesU, idx).Value;
            p.SpriteRect.Height = readData(m_spriteSizesV, idx).Value;

            p.Vel = readData(m_velocities, idx);
            p.AngularVelocity = readData(m_angularVelocities, idx);
            p.Mass = readData(m_masses, idx);
			p.RotationalInnertia = readData (m_rotationalInnertias, idx);
			p.Drag = readData (m_drags, idx);
			p.RotationalDrag = readData (m_rotationalDrags, idx);
			p.EffectorMask = (ushort)((int)readData (m_effectorMasksHigh, idx) << 8
							 	    | (int)readData (m_effectorMasksLow, idx));
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
			writeDataIfNeeded(ref m_positions, idx, new SSAttributeVec3(p.Pos));
			writeDataIfNeeded(ref m_orientationsXY, idx, new SSAttributeVec2 (p.Orientation.Xy));
			writeDataIfNeeded(ref m_orientationsZ, idx, new SSAttributeFloat (p.Orientation.Z));
			writeDataIfNeeded(ref m_masterScales, idx, new SSAttributeFloat (p.MasterScale));
			writeDataIfNeeded(ref m_componentScalesXY, idx, new SSAttributeVec2 (p.ComponentScale.Xy));
			writeDataIfNeeded(ref m_componentScalesZ, idx, new SSAttributeFloat (p.ComponentScale.Z));
			writeDataIfNeeded(ref m_colors, idx, new SSAttributeColor(Color4Helper.Color4toRgba(p.Color)));

			//writeDataIfNeeded(ref m_spriteIndices, idx, new SSAttributeByte(p.SpriteIndex));
            writeDataIfNeeded(ref m_spriteOffsetsU, idx, new SSAttributeFloat(p.SpriteRect.X));
            writeDataIfNeeded(ref m_spriteOffsetsV, idx, new SSAttributeFloat(p.SpriteRect.Y));
            writeDataIfNeeded(ref m_spriteSizesU, idx, new SSAttributeFloat(p.SpriteRect.Width));
            writeDataIfNeeded(ref m_spriteSizesV, idx, new SSAttributeFloat(p.SpriteRect.Height));

            writeDataIfNeeded(ref m_velocities, idx, p.Vel);
            writeDataIfNeeded(ref m_angularVelocities, idx, p.AngularVelocity);
            writeDataIfNeeded(ref m_masses, idx, p.Mass);
			writeDataIfNeeded(ref m_rotationalInnertias, idx, p.RotationalInnertia);
			writeDataIfNeeded(ref m_drags, idx, p.Drag);
			writeDataIfNeeded(ref m_rotationalDrags, idx, p.RotationalDrag);
            writeDataIfNeeded(ref m_viewDepths, idx, p.ViewDepth);
			writeDataIfNeeded(ref m_effectorMasksHigh, idx, (byte)((p.EffectorMask & 0xFF00) >> 8));
			writeDataIfNeeded(ref m_effectorMasksLow, idx, (byte)(p.EffectorMask & 0xFF));
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
                if (iDepth <= rightDepth) {
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

