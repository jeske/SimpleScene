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
        public Vector3 Vel = new Vector3(0f);
        public Vector3 Scale = new Vector3(1f);
        public Color4 Color = c_defaultColor;
		public float Mass = c_defaultMass;
        // TODO texture coord, orientation, scale

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
        #endregion

        #region particle data used for the simulation only
        protected Vector3[] m_velocities;
        protected float[] m_masses;
        // TODO Orientation
        // TODO Scale
        // TODO Texture Coord
        #endregion

        public int Capacity { get { return m_capacity; } }
        public int ActiveBlockLength { get { return m_activeBlockLength; } }
        public SSAttributePos[] Positions { get { return m_positions; } }
        public SSAttributeColor[] Colors { get { return m_colors; } }

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
            m_positions [0].Position = new Vector3 (0f);

            m_colors = new SSAttributeColor[1];
            m_colors [0].Color = SSParticle.c_defaultColor.ToArgb();

            m_velocities = new Vector3[1];
            m_velocities [0] = new Vector3 (0f);

            m_masses = new float[1];
            m_masses [0] = SSParticle.c_defaultMass;

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
                e.EmitParticles(writeNewParticle);
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
            foreach (SSParticleEmitter emitter in m_emitters) {
                emitter.Simulate(timeDelta, writeNewParticle);
            }

            SSParticle p = new SSParticle();
            for (int i = 0; i < m_numParticles; ++i) {
                readParticle(i, p);
                if (p.Life > 0f) {
                    // Alive particle
                    p.Life -= timeDelta;
                    if (p.Life > 0f) {
                        // Still alive. Update position and run through effectors
                        p.Pos += p.Vel * timeDelta;
                        foreach (SSParticleEffector effector in m_effectors) {
                            effector.Simulate(p, timeDelta);
                        }
                    } else {
                        // Particle just died. Hack to not draw?
                        p.Pos = new Vector3 (float.PositiveInfinity);
                        if (m_numParticles == m_capacity) {
                            // released slot will be the next one to be written to
                            m_nextIdxToWrite = i;
                        }
                        if (i == m_activeBlockLength-1) {
                            // reduction in the active particles block
                            while (p.Life < 0f) {
                                --m_activeBlockLength;
                                readParticle(m_activeBlockLength - 1, p);
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
                    writeParticle(i, p);
                }
            }
        }

        protected virtual void writeNewParticle(SSParticle newParticle)
        {
            int writeIdx;
            if (m_numParticles == m_capacity) {
                writeIdx = m_nextIdxToOverwrite;
                m_nextIdxToOverwrite = nextIdx(m_nextIdxToWrite);
            } else {
                while (true) {
                    float life = m_lives [m_nextIdxToWrite];
                    if (life < 0f) {
                        break;
                    }
                    m_nextIdxToWrite = nextIdx(m_nextIdxToWrite);
                }
                writeIdx = m_nextIdxToWrite;
                m_nextIdxToWrite = nextIdx(m_nextIdxToWrite);
                m_numParticles++;
            }
            writeParticle(writeIdx, newParticle);
            if (writeIdx + 1 >= m_activeBlockLength) {
                m_activeBlockLength = writeIdx + 1;
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
            if (p.Life <= 0f) return;

            p.Pos = readData(m_positions, idx).Position;
            int colorData = readData(m_colors, idx).Color;
            p.Color = new Color4(
                (byte)((colorData & 0xFF00) >> 8),        // R
                (byte)((colorData & 0xFF0000) >> 16),     // G
                (byte)((colorData & 0xFF000000) >> 24),   // B
                (byte)(colorData & 0xFF)                  // A
            );
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
                    for (int i = 0; i < m_capacity; ++i) {
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
            writeDataIfNeeded(ref m_colors, idx, 
                              new SSAttributeColor(p.Color.ToArgb()));
            writeDataIfNeeded(ref m_velocities, idx, p.Vel);
            writeDataIfNeeded(ref m_masses, idx, p.Mass);
        }
    }
}

