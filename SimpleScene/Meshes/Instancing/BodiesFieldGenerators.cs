using System;
using System.Collections.Generic;
using OpenTK;
using SimpleScene.Util.ssBVH;

namespace SimpleScene
{
    #region Bodies Generators
    /// <summary>
    /// Generates a number of particles/bodies with 3D coordinates and orientation
    /// </summary>
    //[Serializable]
    public class BodiesFieldGenerator 
    {
        public enum OrientPolicy { None, Random, AwayFromCenter };

        public delegate bool NewBodyDelegate(int id, float scale, Vector3 pos, Quaternion orient);

        private readonly ParticlesFieldGenerator m_partFieldGen;
        private readonly float m_bodyRadius;
        private readonly float m_bodyScaleMin;
        private readonly float m_bodyScaleMax;
		private readonly float m_safetyDistance;
        private readonly OrientPolicy m_orientPolicy;
        private Random m_rand = new Random();
        private NewBodyDelegate m_newBodyDel = null;
		private SSSphereBVH m_bodiesSoFar = null;
        private int m_id = 0;

        public BodiesFieldGenerator(ParticlesFieldGenerator partFieldGen,
            float bodyScaleMin=1.0f, float bodyScaleMax=1.0f, float bodyRadius=0.0f,
            float safetyDistance = 0.0f, OrientPolicy oriPolicy = OrientPolicy.Random)
        {
            m_partFieldGen = partFieldGen;
            m_bodyScaleMin = bodyScaleMin;
            m_bodyScaleMax = bodyScaleMax;
            m_bodyRadius = bodyRadius;
			m_safetyDistance = safetyDistance;
            m_orientPolicy = oriPolicy;
            SetSeed(0);
        }
        public void SetSeed(int seed)
        {
            m_rand = new Random(seed);
            m_partFieldGen.SetSeed(seed);
        }

        public void Generate(int numParticles, NewBodyDelegate newBodyDel)
        {
			m_bodiesSoFar = new SSSphereBVH();
            m_newBodyDel = newBodyDel;
            m_id = 0;

            m_partFieldGen.Generate(numParticles, onNewParticle);

            // Prepare the garbage
            //m_rand = null;
            m_newBodyDel = null;
            m_bodiesSoFar = null;
        }

        private bool onNewParticle(int id, Vector3 pos)
        {
            float scale = m_bodyScaleMin + (float)m_rand.NextDouble() * (m_bodyScaleMax - m_bodyScaleMin);

			SSSphere newBodyInfo;
			newBodyInfo.radius = m_bodyRadius * scale;
			newBodyInfo.center = pos;
            if (validate(newBodyInfo)) {
                Quaternion ori;
                switch (m_orientPolicy) {
                case OrientPolicy.Random:
                    ori = randomOrient();
                    break;
                case OrientPolicy.AwayFromCenter:
                    ori = OpenTKHelper.getRotationTo(Vector3.UnitZ, pos, Vector3.UnitZ);
                    break;
                case OrientPolicy.None:
                default:
                    ori = Quaternion.Identity;
                    break;
                }
                if (m_newBodyDel(m_id, scale, pos, ori)) {
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

        protected virtual bool validate(SSSphere newBodyInfo) 
        {
            // override to handle collision tests efficiently for a specific shape of a field
            // or add other clipping tests..
			newBodyInfo.radius += m_safetyDistance;
			if (m_bodyRadius == 0.0f) return true;

			List<ssBVHNode<SSSphere>> intersectList 
				= m_bodiesSoFar.traverse (newBodyInfo.ToAABB ());
			foreach (ssBVHNode<SSSphere> node in intersectList) {
				if (node.gobjects != null) {
					foreach (SSSphere sphere in node.gobjects) {
						if (newBodyInfo.IntersectsSphere (sphere)) {
							return false; // invalid
						}
					}
				}
            }
			m_bodiesSoFar.addObject (newBodyInfo);
            return true; // valid
        }
    }

    public class BodiesRingGenerator : BodiesFieldGenerator
    {
        public BodiesRingGenerator(ParticlesPlaneGenerator sliceGenerator,
            Vector3 ringCenter, Vector3 up, float ringRadius,
            float sectionStart = 0.0f,
            float sectionEnd = (float)(2.0*Math.PI),
            float bodyScaleMin = 1.0f, float bodyScaleMax = 1.0f, float bodyRadius = 0.0f,
            float safetyDistance = 0.0f, OrientPolicy oriPolicy = OrientPolicy.Random)
            : base(new ParticlesRingGenerator(sliceGenerator, 
                ringCenter, up, ringRadius, sectionStart, sectionEnd),
                bodyScaleMin, bodyScaleMax, bodyRadius, safetyDistance, oriPolicy)
        { }

        public BodiesRingGenerator(float ovalHorizontal, float ovalVertical,
            Vector3 ringCenter, Vector3 up, float ringRadius,
            float sectionStart = 0.0f,
            float sectionEnd = (float)(2.0*Math.PI),
            float bodyScaleMin = 1.0f, float bodyScaleMax = 1.0f, float bodyRadius = 0.0f,
            float safetyDistance = 0.0f, OrientPolicy oriPolicy = OrientPolicy.Random)
            : base(new ParticlesRingGenerator(new ParticlesOvalGenerator(ovalHorizontal, ovalVertical),
                ringCenter, up, ringRadius, sectionStart, sectionEnd),
                bodyScaleMin, bodyScaleMax, bodyRadius, safetyDistance, oriPolicy)
        { }
    }
    #endregion
}

