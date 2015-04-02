using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

// TODO split this file

namespace SimpleScene
{
     #region Particle Field Generators
    /// <summary>
    /// Generates a number of particles with 3D coordinates
    /// </summary>
    public abstract class ParticlesFieldGenerator 
    {
        static protected int c_maxTriesFactor = 10; // 10 * numObjects means exhausted all tries to generate

        public delegate bool NewParticleDelegate(int id, Vector3 pos); // return true for "accept"

        protected Random m_rand = new Random();

        public virtual void SetSeed(int seed) 
        {
            m_rand = new Random(seed);
        }

        abstract public void Generate(int numParticles,
                                      NewParticleDelegate newPartDel); // density is in particles per cubic unit
    }

    public class ParticlesSphereGenerator : ParticlesFieldGenerator
    {
		public float Radius;
		public Vector3 Center;

		public ParticlesSphereGenerator()
		{
			Radius = 1f;
			Center = Vector3.Zero;
		}

        public ParticlesSphereGenerator (Vector3 center, float radius)
        {
            Radius = radius;
            Center = center;
        }

        public override void Generate (int numParticles, NewParticleDelegate newPartDel)
        {
            int numTries = 0;
            for (int i = 0; i < numParticles; ++i) {
                bool accepted = false;
                while (!accepted) {
                    if (numTries >= numParticles * c_maxTriesFactor) {
                        // This is somewhat of a hack to add a failsafe for the random strategy of 
                        // fitting things in. Currently we just give up if we tried too many time with 
                        // no luck. This can happen when trying to fit too much into a small space.
                        // In the future a smarter packing strategy may be employed before giving up.
                        // todo: print something
						System.Console.WriteLine (
							"Too many rejections when generating a field. " + 
							"Giving up after " + numTries + " fitting attempts.");
                        return;
                    }
					++numTries;
                    float theta = (float)(2.0 * Math.PI * m_rand.NextDouble());
                    float alpha = (float)(Math.PI * (m_rand.NextDouble() - 0.5));
                    float r = Radius * (float)m_rand.NextDouble();
                    float z = r * (float)Math.Sin(alpha);
                    float r_xy = r * (float)Math.Cos(alpha);
                    float x = r_xy * (float)Math.Cos(theta);
                    float y = r_xy * (float)Math.Sin(theta);
                    accepted = newPartDel(i, Center + new Vector3(x, y, z));
                }
            }
        }
    }

    public class ParticlesBoxGenerator : ParticlesFieldGenerator
    {
        protected readonly Vector3 m_center;
        protected readonly Vector3 m_dimmensions;

        public ParticlesBoxGenerator(Vector3 center, float cubeDiameter) 
        {
            m_dimmensions = new Vector3 (cubeDiameter);
            m_center = center;
        }

        public ParticlesBoxGenerator(Vector3 center, Vector3 dimmensions)
        {
            m_dimmensions = dimmensions;
            m_center = center;
        }

        public override void Generate (int numParticles, NewParticleDelegate newPartDel)
        {
            int numTries = 0;
            for (int i = 0; i < numParticles; ++i) {
                bool accepted = false;
                while (!accepted) {
                    if (numTries >= numParticles * c_maxTriesFactor) {
                        // This is somewhat of a hack to add a failsafe for the random strategy of 
                        // fitting things in. Currently we just give up if we tried too many time with 
                        // no luck. This can happen when trying to fit too much into a small space.
                        // In the future a smarter packing strategy may be employed before giving up.
                        // TODO: print something
						System.Console.WriteLine (
							"Too many rejections when generating a field. " + 
							"Giving up after " + numTries + " fitting attempts.");
                        return;
                    }
					++numTries;
                    float x = (float)(m_dimmensions.X * (m_rand.NextDouble() - 0.5));
                    float y = (float)(m_dimmensions.Y * (m_rand.NextDouble() - 0.5));
                    float z = (float)(m_dimmensions.Z * (m_rand.NextDouble() - 0.5));
                    accepted = newPartDel(i, m_center + new Vector3(x, y, z));
                }
            }
        }
    }

    public class ParticlesRingGenerator : ParticlesFieldGenerator
    {
        static protected float c_eps = 0.001f;
                                                    // means iterated too much while trying to validate
        protected readonly ParticlesPlaneGenerator m_planeGenerator;
        protected readonly Vector3 m_ringCenter;
        protected readonly float m_ringRadius;
        protected readonly float m_sectionStart;
        protected readonly float m_sectionEnd;

        protected readonly Vector3 m_ringZAxis;
        protected readonly Vector3 m_ringXAxis; // if up is (0, 0, 1) this will be (1, 0, 0)
        protected readonly Vector3 m_ringYAxis; // if up is (0, 0, 1) this will be (0, 1, 0)
        
        private float m_ringTheta;
        private Vector3 m_newPos;

        public ParticlesRingGenerator(ParticlesPlaneGenerator planeGenerator,
                                      Vector3 ringCenter, Vector3 up, float ringRadius,
                                      float sectionStart = 0.0f, 
                                      float sectionEnd = (float)(2.0*Math.PI))
        {
            m_ringCenter = ringCenter;
            m_ringZAxis = up.Normalized();
            m_ringRadius = ringRadius;
            m_sectionStart = sectionStart;
            m_sectionEnd = sectionEnd;
            m_planeGenerator = planeGenerator;

            if (Math.Abs(m_ringZAxis.X) < c_eps && Math.Abs(m_ringZAxis.Y) < c_eps) {
                m_ringXAxis = new Vector3(1.0f, 0.0f, 0.0f);
            } else {
                m_ringXAxis = new Vector3(m_ringZAxis.Y, -m_ringZAxis.X, 0.0f);
                m_ringXAxis.Normalize();
            }
            m_ringYAxis = Vector3.Cross(m_ringXAxis, m_ringZAxis);
        }

        public override void SetSeed(int seed)
        {
            base.SetSeed(seed);
            m_planeGenerator.SetSeed(seed);
        }

        public override void Generate(int numParticles, NewParticleDelegate newPartDeleg)
        {
            int numTries = 0;
            for (int i = 0; i < numParticles; ++i) {
                bool accepted = false;
                while (!accepted) {
                    if (numTries >= numParticles * c_maxTriesFactor) {
                        // This is somewhat of a hack to add a failsafe for the random strategy of 
                        // fitting things in. Currently we just give up if we tried too many time with 
                        // no luck. This can happen when trying to fit too much into a small space.
                        // In the future a smarter packing strategy may be employed before giving up.
                        // TODO: print something
						System.Console.WriteLine (
							"Too many rejections when generating a field. " + 
							"Giving up after " + numTries + " fitting attempts.");
                        return;
                    }
					++numTries;
                    m_ringTheta = m_sectionStart + (float)m_rand.NextDouble() * (m_sectionEnd - m_sectionStart);
                    m_planeGenerator.Generate(1, onNewPlaneParticle);
                    accepted = newPartDeleg(i, m_newPos);
                }
            }
        }

        private bool onNewPlaneParticle(int id, Vector2 slicePos)
        {
            Vector3 toSliceDir = m_ringXAxis * (float)Math.Cos(m_ringTheta)
                               + m_ringYAxis * (float)Math.Sin(m_ringTheta);
            Vector3 sliceCenter = m_ringCenter + toSliceDir * m_ringRadius;
            m_newPos = sliceCenter
                     + toSliceDir * slicePos.X
                     + m_ringZAxis * slicePos.Y;
            return true;
        }
    }
    #endregion

}
