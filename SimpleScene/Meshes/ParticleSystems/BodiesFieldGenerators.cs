using System;
using System.Collections.Generic;
using OpenTK;

namespace SimpleScene
{
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

