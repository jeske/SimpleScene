using System;
using OpenTK;

namespace SimpleScene
{
    #region Particle Plane Generators
    /// <summary>
    /// Generates a number of particles with 2D coordinates
    /// </summary>
    public abstract class ParticlesPlaneGenerator
    {
        public delegate bool NewParticleDelegate(int id, Vector2 pos); // return true for accept

        protected Random m_rand = new Random();

        public void SetSeed(int seed) {
            m_rand = new Random(seed);
        }

        abstract public void Generate(int numParticles, NewParticleDelegate newPartDel);
    }

    public class ParticlesOvalGenerator : ParticlesPlaneGenerator
    {
        private readonly float m_horizontalMax;
        private readonly float m_verticalMax;

        public ParticlesOvalGenerator(float horizontalMax, float verticalMax)
        {
            m_horizontalMax = horizontalMax;
            m_verticalMax = verticalMax;
        }

        public override void Generate(int numParticles, NewParticleDelegate newPartDel)
        {
            for (int i = 0; i < numParticles; ++i) {
                bool accepted = false;
                while (!accepted) {
                    float r = (float)m_rand.NextDouble();
                    float theta = (float)(m_rand.NextDouble() * 2.0 * Math.PI);
                    float x = r * (float)Math.Cos(theta) * m_horizontalMax;
                    float y = r * (float)Math.Sin(theta) * m_verticalMax;
                    accepted = newPartDel(i, new Vector2(x, y));
                }
            }
        }
    }
    #endregion
}

