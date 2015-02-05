using System;
using SimpleScene.Util;

namespace SimpleScene
{
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
}

