using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;


namespace SimpleScene
{
    public class Particle
    {
        public Vector3 Pos = new Vector3(0f);
        public Vector3 Vel = new Vector3(0f);
    }

    public delegate void ParticleReceiver<P>(P newParticle) where P : Particle;

    public class ColoredParticle : Particle
    {
        public Color Color = Color.White;
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
        protected List<P> m_particles = new List<P>();
        protected List<ParticleEffector<P>> m_effectors = new List<ParticleEffector<P>> ();

        public ParticleSystem ()
        {
        }


    }
}

