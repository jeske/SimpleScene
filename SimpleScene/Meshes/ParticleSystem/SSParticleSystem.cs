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
        // TODO scale

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
        public int ParticlesPerEmission = 1;

        private float m_timeSinceLastEmission;
        private float m_nextEmission;

        public SSParticleEmitter()
        {
            Reset();
        }

        public delegate void SSParticleReceiver(SSParticle newParticle);

        public abstract void EmitParticles (int particleCount, SSParticleReceiver receiver);

        public void Simulate(float deltaT, SSParticleReceiver receiver) 
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
        public abstract void EffectParticle (SSParticle particle);
        // For example, particle.Vel += new Vector3(1f, 0f, 0f) simulates acceleration on the X axis
        // Multiple effectors will combine their acceleration effect to determine the final velocity of
        // the particle.
    }

    public class SSParticleSystem
    {
        protected List<SSParticleEmitter> m_emitters = new List<SSParticleEmitter> ();
        protected List<SSParticleEffector> m_effectors = new List<SSParticleEffector> ();

        protected readonly int m_capacity;
        protected int m_numParticles = 0;

        #region Required and Changing Particle Data
        protected readonly float[] m_lives;  // unused particles will be hacked to not draw
        protected readonly Vector3[] m_positions;
        #endregion

        #region Optional or Often Uniform Particle Data
        protected Vector3[] m_velocities = new Vector3[1];
        protected int[] m_colors = new int[1];
        protected float[] m_masses = new float[1];
        #endregion

        public SSParticleSystem (int capacity)
        {
            m_capacity = capacity;
            m_lives = new float[m_capacity];
            m_positions = new Vector3[m_capacity];

            m_velocities [0] = new Vector3 (0f);
            m_colors [0] = SSParticle.c_defaultColor.ToArgb();
            m_masses [0] = SSParticle.c_defaultMass;
        }

        public void Simulate(float timeDelta)
        {
            // TODO update particles:
            // - run through emitters
            // - run through effectors
        }

        protected T readData<T>(T[] array, int idx)
        {
            if (idx >= array.Length) {
                return array [0];
            } else {
                return array [idx];
            }
        }

        protected virtual SSParticle readParticle (int idx) {
            SSParticle p = new SSParticle ();
            p.Life = m_lives [idx];
            p.Pos = m_positions [idx];

            p.Vel = readData(m_velocities, idx);
            p.Mass = readData(m_masses, idx);

            int colorData = readData(m_colors, idx);
            p.Color = new Color4 (
                (colorData & 0xFF00) >> 8,        // R
                (colorData & 0xFF0000) >> 16,     // G
                (colorData & 0xFF000000) >> 24,   // B
                colorData & 0xFF);                // A
            return p;
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
            m_positions [idx] = p.Pos;

            writeDataIfNeeded(m_velocities, idx, p.Vel);
            writeDataIfNeeded(m_colors, idx, p.Color.ToArgb());
            writeDataIfNeeded(m_masses, idx, p.Mass);
        }
    }
}

