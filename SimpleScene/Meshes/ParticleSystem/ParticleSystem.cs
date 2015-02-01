using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;


namespace SimpleScene
{
    public class Particle
    {
        public float Life = 0f;
        public Vector3 Pos = new Vector3(0f);
        public Vector3 Vel = new Vector3(0f);

        virtual public void WriteParticle (ParticleSystem<Particle>ps, int idx)
        {
            ps.WriteLife(idx, Life);
            ps.WritePosition(idx, Pos);
            ps.WriteVelocity(idx, Vel);
        }

        virtual public void ReadParticle(ParticleSystem<Particle>ps, int idx)
        {
            Life = ps.ReadLife(idx);
            Pos = ps.ReadPosition(idx);
            Vel = ps.ReadVelocity(idx);
        }
    }

    public delegate void ParticleReceiver<P>(P newParticle);

    public class ColoredParticle : Particle
    {
        public Color Color = Color.White;

        public override void WriteParticle (ParticleSystem<Particle> ps, int idx)
        {
            base.WriteParticle(ps, idx);
            ps.WriteColor(idx, Color);
        }

        public override void ReadParticle (ParticleSystem<Particle> ps, int idx)
        {
            base.ReadParticle(ps, idx);
            Color = ps.ReadColor(idx);
        }
    }

    public class ParticleWithMass : Particle
    {
        public float Mass = 1f;
    }

    public abstract class ParticleEmitter<P> where P: Particle
    {
        public float MinEmitInterval = 1.0f;
        public float MaxEmitInterval = 1.0f;

        public abstract void EmitParticles (int numParticles, ParticleReceiver<P> receiver);
    }

    public abstract class ParticleEffector<P> where P: Particle
    {
        public abstract void EffectParticle (P particle);
        // For example, particle.Vel += new Vector3(1f, 0f, 0f) simulates acceleration on the X axis
        // Multiple effectors will combine their acceleration effect to determine the final velocity of
        // the particle.
    }

    public class ParticleSystem<P>
        where P : Particle
    {
        protected List<ParticleEmitter<P>> m_emitters = new List<ParticleEmitter<P>> ();
        protected List<ParticleEffector<P>> m_effectors = new List<ParticleEffector<P>> ();

        protected readonly int m_capacity;

        #region Required Particle Parameters
        protected readonly float[] m_lives;  // unused particles will be hacked to not draw
        protected readonly Vector3[] m_positions;
        protected readonly Vector3[] m_velocities;
        #endregion

        #region Optional Particle Parameters
        protected int[] m_colors = null;
        protected float[] m_masses = null;
        #endregion

        internal void WriteLife(int idx, float life)
        {
            m_lives [idx] = life;
        }

        internal float ReadLife(int idx) 
        {
            return m_lives [idx];
        }

        internal void WritePosition(int idx, Vector3 position)
        {
            m_positions [idx] = position;
        }

        internal Vector3 ReadPosition(int idx)
        {
            return m_positions[idx];
        }

        internal void WriteVelocity(int idx, Vector3 velocity)
        {
            m_velocities [idx] = velocity;
        }

        internal Vector3 ReadVelocity(int idx)
        {
            return m_velocities[idx];
        }

        internal void WriteColor(int idx, Color color)
        {
            if (m_colors == null) {
                m_colors = new int[m_capacity];
            }
            m_colors [idx] = color.ToArgb();
        }

        internal Color ReadColor(int idx)
        {
            return Color.FromArgb (m_colors [idx]);
        }

        public ParticleSystem (int capacity)
        {
            m_capacity = capacity;
            m_lives = new float[m_capacity];
            m_positions = new Vector3[m_capacity];
            m_velocities = new Vector3[m_capacity];
        }


    }
}

