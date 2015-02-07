using System;
using SimpleScene.Util;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{
    /// <summary>
    /// Emits particles on demand via EmitParticles(...) or periodically via Simulate(...)
    /// </summary>
    public abstract class SSParticleEmitter
    {
        public delegate void ReceiverHandler(SSParticle newParticle);

        protected static Random s_rand = new Random ();

        public float EmissionIntervalMin = 1.0f;
        public float EmissionIntervalMax = 1.0f;
        public float EmissionInterval {
            set { EmissionIntervalMin = EmissionIntervalMax = value; }
        }

        public float LifeMin = 1f;
        public float LifeMax = 1f;
        public float Life {
            set { LifeMin = LifeMax = value; }
        }

        public int ParticlesPerEmissionMin = 1;
        public int ParticlesPerEmissionMax = 1;
        public int ParticlesPerEmission {
            set { ParticlesPerEmissionMin = ParticlesPerEmissionMax = value; }
        }

        public float MasterScaleMin = 1f;
        public float MasterScaleMax = 1f;
        public float MasterScale {
            set { MasterScaleMin = MasterScaleMax = value; }
        }

        public Vector3 ComponentScaleMin = new Vector3(1f);
        public Vector3 ComponentScaleMax = new Vector3(1f);
        public Vector3 ComponentScale {
            set { ComponentScaleMin = ComponentScaleMax = value; }
        }

        public Vector3 VelocityComponentMin = new Vector3 (1f);
        public Vector3 VelocityComponentMax = new Vector3 (1f);
        public Vector3 Velocity {
            set { VelocityComponentMin = VelocityComponentMax = value; }
        }

        public Color4 ColorComponentMin = Color4.White;
        public Color4 ColorComponentMax = Color4.White;
        public Color4 Color {
            set { ColorComponentMin = ColorComponentMax = value; }
        }

        private float m_timeSinceLastEmission;
        private float m_nextEmission;

        public SSParticleEmitter()
        {
            Reset();
        }

        public virtual void Reset()
        {
            m_timeSinceLastEmission = 0f;
            m_nextEmission = float.NegativeInfinity;
        }

        public void EmitParticles(ReceiverHandler receiver)
        {
            int numToEmit = s_rand.Next(ParticlesPerEmissionMin, ParticlesPerEmissionMax);
            emitParticles(numToEmit, receiver);
        }

        public void Simulate(float deltaT, ReceiverHandler receiver) 
        {
            m_timeSinceLastEmission += deltaT;
            if (m_timeSinceLastEmission > m_nextEmission) {
                EmitParticles(receiver);
                m_timeSinceLastEmission = 0f;
                m_nextEmission = Interpolate.Lerp(EmissionIntervalMin, EmissionIntervalMax, 
                    (float)s_rand.NextDouble());
            }
        }

        /// <summary>
        /// Override by the derived classes to describe how new particles are emitted
        /// </summary>
        /// <param name="particleCount">Particle count.</param>
        /// <param name="receiver">Receiver.</param>
        protected abstract void emitParticles (int particleCount, ReceiverHandler receiver);

        /// <summary>
        /// To be used by derived classes for shared particle setup
        /// </summary>
        /// <param name="p">particle to setup</param>
        protected void configureNewParticle(SSParticle p)
        {
            p.Life = Interpolate.Lerp(LifeMin, LifeMax, 
                (float)s_rand.NextDouble());

            p.Vel.X = Interpolate.Lerp(VelocityComponentMin.X, VelocityComponentMax.X, 
                (float)s_rand.NextDouble());
            p.Vel.Y = Interpolate.Lerp(VelocityComponentMin.Y, VelocityComponentMax.Y, 
                (float)s_rand.NextDouble());
            p.Vel.Z = Interpolate.Lerp(VelocityComponentMin.Z, VelocityComponentMax.Z, 
                (float)s_rand.NextDouble());

            p.Color.R = Interpolate.Lerp(ColorComponentMin.R, ColorComponentMax.R,
                (float)s_rand.NextDouble());
            p.Color.G = Interpolate.Lerp(ColorComponentMin.G, ColorComponentMax.G,
                (float)s_rand.NextDouble());
            p.Color.B = Interpolate.Lerp(ColorComponentMin.B, ColorComponentMax.B,
                (float)s_rand.NextDouble());
            p.Color.A = Interpolate.Lerp(ColorComponentMin.A, ColorComponentMax.A,
                (float)s_rand.NextDouble());
        }
    }

    /// <summary>
    /// Emits via an instance of ParticlesFieldGenerator
    /// </summary>
    public class SSParticlesFieldEmitter : SSParticleEmitter
    {
        protected ParticlesFieldGenerator m_fieldGenerator;

        public SSParticlesFieldEmitter(ParticlesFieldGenerator fieldGenerator)
        {
            m_fieldGenerator = fieldGenerator;
        }

        public void SetSeed(int seed)
        {
            m_fieldGenerator.SetSeed(seed);
        }

        protected override void emitParticles (int particleCount, ReceiverHandler particleReceiver)
        {
            SSParticle newParticle = new SSParticle();
            ParticlesFieldGenerator.NewParticleDelegate fieldReceiver = (id, pos) => {
                configureNewParticle(newParticle);
                newParticle.Pos = pos;
                particleReceiver(newParticle);
                return true;
            };
            m_fieldGenerator.Generate(particleCount, fieldReceiver);
        }
    }
}

