using System;

namespace SimpleScene
{
    public abstract class SSParticleEffector
    {
        public abstract void Simulate (SSParticle particle, float deltaT);
        // For example, particle.Vel += new Vector3(1f, 0f, 0f) * dT simulates 
        // acceleration on the X axis. Multiple effectors will combine their 
        // acceleration effect to determine the final velocity of the particle.

        public virtual void Reset() { }
    }


    public class SSCustomForceParticleEffector
    {

    }

}

