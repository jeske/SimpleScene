using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace HZG.Utils
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
        protected readonly float m_radius;
        protected readonly Vector3 m_center;

        public ParticlesSphereGenerator (Vector3 center, float radius)
        {
            m_radius = radius;
            m_center = center;
        }

        public override void Generate (int numParticles, NewParticleDelegate newPartDel)
        {
            int numTries = 0;
            for (int i = 0; i < numParticles; ++i) {
                bool accepted = false;
                while (!accepted) {
                    ++numTries;
                    if (numTries > numParticles * c_maxTriesFactor) {
                        // This is somewhat of a hack to add a failsafe for the random strategy of 
                        // fitting things in. Currently we just give up if we tried too many time with 
                        // no luck. This can happen when trying to fit too much into a small space.
                        // In the future a smarter packing strategy may be employed before giving up.
                        // todo: print something
                        return;
                    }
                    float theta = (float)(2.0 * Math.PI * m_rand.NextDouble());
                    float alpha = (float)(Math.PI * (m_rand.NextDouble() - 0.5));
                    float r = m_radius * (float)m_rand.NextDouble();
                    float z = r * (float)Math.Sin(alpha);
                    float r_xy = r * (float)Math.Cos(alpha);
                    float x = r_xy * (float)Math.Cos(theta);
                    float y = r_xy * (float)Math.Sin(theta);
                    accepted = newPartDel(i, m_center + new Vector3(x, y, z));
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
                    ++numTries;
                    if (numTries > numParticles * c_maxTriesFactor) {
                        // This is somewhat of a hack to add a failsafe for the random strategy of 
                        // fitting things in. Currently we just give up if we tried too many time with 
                        // no luck. This can happen when trying to fit too much into a small space.
                        // In the future a smarter packing strategy may be employed before giving up.
                        // TODO: print something
                        return;
                    }
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
                    ++numTries;
                    if (numTries > numParticles * c_maxTriesFactor) {
                        // This is somewhat of a hack to add a failsafe for the random strategy of 
                        // fitting things in. Currently we just give up if we tried too many time with 
                        // no luck. This can happen when trying to fit too much into a small space.
                        // In the future a smarter packing strategy may be employed before giving up.
                        // TODO: print something
                        return;
                    }
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

    #region Bodies Generators
    /// <summary>
    /// Generates a number of particles/bodies with 3D coordinates and orientation
    /// </summary>
    public class BodiesFieldGenerator 
    {
        public delegate bool NewBodyDelegate(int id, float scale, Vector3 pos, Quaternion orient);

        private readonly ParticlesFieldGenerator m_partFieldGen;
        private float m_bodyRadius = 0.0f;
        private float m_bodyScaleMin = 1.0f;
        private float m_bodyScaleMax = 1.0f;
        private Random m_rand = new Random();
        private NewBodyDelegate m_newBodyDel = null;
        private List<BodyInfo> m_bodiesSoFar = null;
        private int m_id = 0;

        public BodiesFieldGenerator(ParticlesFieldGenerator partFieldGen,
                                    float bodyScaleMin=1.0f, float bodyScaleMax=1.0f, float bodyRadius=0.0f)
        {
            m_partFieldGen = partFieldGen;
            m_bodyScaleMin = bodyScaleMin;
            m_bodyScaleMax = bodyScaleMax;
            m_bodyRadius = bodyRadius;
        }
        public void SetSeed(int seed)
        {
            m_rand = new Random(seed);
            m_partFieldGen.SetSeed(seed);
        }

        public void Generate(int numParticles, NewBodyDelegate newBodyDel)
        {
            m_bodiesSoFar = new List<BodyInfo>();
            m_newBodyDel = newBodyDel;
            m_id = 0;

            m_partFieldGen.Generate(numParticles, onNewParticle);

            // Prepare the garbage
            m_rand = null;
            m_newBodyDel = null;
            m_bodiesSoFar = null;
        }

        private bool onNewParticle(int id, Vector3 pos)
        {
            float scale = m_bodyScaleMin + (float)m_rand.NextDouble() * (m_bodyScaleMax - m_bodyScaleMin);

            BodyInfo newBodyInfo;
            newBodyInfo.scaledRadius = m_bodyRadius * scale;
            newBodyInfo.pos = pos;
            if (validate(newBodyInfo)) {
                Quaternion randOri = randomOrient();
                if (m_newBodyDel(m_id, scale, pos, randOri)) {
                    ++m_id;
                    return true; // accept
                }
            }
            return false; // reject
        }

        protected Quaternion randomOrient()
        {
            // rotate a random angle around a random axis
            Vector3 axis = new Vector3((float)m_rand.NextDouble() - 0.5f,
                                       (float)m_rand.NextDouble() - 0.5f,
                                       (float)m_rand.NextDouble() - 0.5f);
            float angle = (float) (2.0 * Math.PI * (float)m_rand.NextDouble());
            return Quaternion.FromAxisAngle(axis, angle);
        }

        protected virtual bool validate(BodyInfo newBodyInfo) 
        {
            // override to handle collision tests efficiently for a specific shape of a field
            // or add other clipping tests..
            if (m_bodyRadius == 0.0f) return true;
            foreach (BodyInfo bi in m_bodiesSoFar) {
                if (newBodyInfo.Intersects(bi)) {
                    return false; // invalid
                }
            }
            m_bodiesSoFar.Add(newBodyInfo);
            return true; // valid
        }

        protected struct BodyInfo 
        {
            // In theory could be extended to include exact positioning of the body
            public Vector3 pos;
            public float scaledRadius;

            public bool Intersects (BodyInfo other) {
                float distSq = (other.pos - this.pos).LengthSquared;
                float addedRadSq = (other.scaledRadius + this.scaledRadius);
                addedRadSq *= addedRadSq;
                if (distSq <= addedRadSq) {
                    return true;
                } else {
                    return false;
                }
            }
        }

    }

    public class BodiesRingGenerator : BodiesFieldGenerator
    {
        public BodiesRingGenerator(ParticlesPlaneGenerator sliceGenerator,
                                   Vector3 ringCenter, Vector3 up, float ringRadius,
                                   float sectionStart = 0.0f,
                                   float sectionEnd = (float)(2.0*Math.PI),
                                   float bodyScaleMin = 1.0f, float bodyScaleMax = 1.0f, float bodyRadius = 0.0f)
            : base(new ParticlesRingGenerator(sliceGenerator, 
                                              ringCenter, up, ringRadius, sectionStart, sectionEnd),
                                              bodyScaleMin, bodyScaleMax, bodyRadius)
        { }

        public BodiesRingGenerator(float ovalHorizontal, float ovalVertical,
                                   Vector3 ringCenter, Vector3 up, float ringRadius,
                                   float sectionStart = 0.0f,
                                   float sectionEnd = (float)(2.0*Math.PI),
                                   float bodyScaleMin = 1.0f, float bodyScaleMax = 1.0f, float bodyRadius = 0.0f)
            : base(new ParticlesRingGenerator(new ParticlesOvalGenerator(ovalHorizontal, ovalVertical),
                                              ringCenter, up, ringRadius, sectionStart, sectionEnd),
                                              bodyScaleMin, bodyScaleMax, bodyRadius)
        { }
    }
    #endregion
}
