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
        // TODO orientation, scale

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

    public abstract class SSParticleEmitter
    {
        protected static Random s_rand = new Random ();

        public float MinEmissionInterval = 1.0f;
        public float MaxEmissionInterval = 1.0f;
        public float MinLife = 10f;
        public float MaxLife = 10f;
        public int ParticlesPerEmission = 1;

        private float m_timeSinceLastEmission;
        private float m_nextEmission;

        public SSParticleEmitter()
        {
            Reset();
        }

        public delegate void ReceiverHandler(SSParticle newParticle);

        public abstract void EmitParticles (int particleCount, ReceiverHandler receiver);

        public void Simulate(float deltaT, ReceiverHandler receiver) 
        {
            m_timeSinceLastEmission += deltaT;
            if (m_timeSinceLastEmission > m_nextEmission) {
                EmitParticles(ParticlesPerEmission, receiver);
                m_timeSinceLastEmission = 0f;
                m_nextEmission = Interpolate.Lerp(MinEmissionInterval, MaxEmissionInterval, 
                                                  (float)s_rand.NextDouble());
            }
        }

        public void Reset()
        {
            m_timeSinceLastEmission = 0f;
            m_nextEmission = float.NegativeInfinity;
        }
    }

    public abstract class SSParticleEffector
    {
        public abstract void Simulate (SSParticle particle, float deltaT);
        // For example, particle.Vel += new Vector3(1f, 0f, 0f) * dT simulates 
        // acceleration on the X axis. Multiple effectors will combine their 
        // acceleration effect to determine the final velocity of the particle.
    }

    public class SSParticleSystem
    {
        protected List<SSParticleEmitter> m_emitters = new List<SSParticleEmitter> ();
        protected List<SSParticleEffector> m_effectors = new List<SSParticleEffector> ();

        protected readonly int m_capacity;
        protected int m_numParticles = 0;

        #region required particle data
        protected readonly float[] m_lives;  // unused particles will be hacked to not draw
        #endregion

        #region particle data sent to the GPU
        protected SSAttributePos[] m_positions = new SSAttributePos[1];
        protected SSAttributeColor[] m_colors = new SSAttributeColor[1];
        #endregion

        #region particle data used for the simulation only
        protected Vector3[] m_velocities = new Vector3[1];
        protected float[] m_masses = new float[1];
        // TODO Orientation
        // TODO Scale
        #endregion

        public SSParticleSystem (int capacity)
        {
            m_capacity = capacity;
            m_lives = new float[m_capacity];

            m_positions [0].Position = new Vector3 (0f);
            m_colors [0].Color = SSParticle.c_defaultColor.ToArgb();

            m_velocities [0] = new Vector3 (0f);
            m_masses [0] = SSParticle.c_defaultMass;
        }

        public void AddEmitter(SSParticleEmitter emitter)
        {
            m_emitters.Add(emitter);
        }

        public void AddEffector(SSParticleEffector effector)
        {
            m_effectors.Add(effector);
        }

        protected virtual void NewParticleReceiver(SSParticle newParticle)
        {
            // TODO Implement
        }

        public void Simulate(float timeDelta)
        {
            foreach (SSParticleEmitter emitter in m_emitters) {
                emitter.Simulate(timeDelta, NewParticleReceiver);
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
                    }
                    writeParticle(i, p);
                }
            }
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

        protected void writeDataIfNeeded<T>(T[] array, int idx, T value) where T : IEquatable<T>
        {
            bool write = true;
            if (idx > 1 && array.Length == 1) {
                T masterVal = array [0];
                if (masterVal.Equals(value)) {
                    write = false;
                } else {
                    array = new T[m_capacity];
                    array [0] = masterVal;
                }
            }
            if (write) {
                array [idx] = value;
            }
        }

        protected virtual void writeParticle(int idx, SSParticle p) {
            m_lives [idx] = p.Life;
            writeDataIfNeeded(m_positions, idx, 
                              new SSAttributePos(p.Pos));
            writeDataIfNeeded(m_colors, idx, 
                              new SSAttributeColor(p.Color.ToArgb()));
            writeDataIfNeeded(m_velocities, idx, p.Vel);
            writeDataIfNeeded(m_masses, idx, p.Mass);
        }
    }
}

