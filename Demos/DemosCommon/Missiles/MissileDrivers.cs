using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public interface ISSpaceMissileEjectionDriver
    {
        // Note the position is already assigned by field generator
        void init(SSpaceMissileData missile, Vector3 clusterInitPos, Vector3 clusterInitVel,
            out Vector3 dir, out Vector3 velocity, out float pitchVel, out float yawVel);
        void updateExecution(SSpaceMissileData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel);
    }

    public interface ISSpaceMissilePursuitDriver
    {
        void updateExecution(SSpaceMissileData missile, float timeElapsed, out Vector3 latax);
        float estimateTimeNeededToHit(SSpaceMissileData missile); 
    }

    public class SSimpleMissileEjectionDriver : ISSpaceMissileEjectionDriver
    {
        public void init(SSpaceMissileData missile, Vector3 clusterInitPos, Vector3 clusterInitVel,
            out Vector3 dir, out Vector3 velocity, out float pitchVel, out float yawVel)
        {
            var cluster = missile.cluster;
            dir = (missile.position - clusterInitPos).Normalized();


            //dir = Vector3.UnitZ;
            //up = Vector3.UnitY;
            //Vector3 dummy;
            //OpenTKHelper.TwoPerpAxes(dir, out dummy, out up);

            var mParams = cluster.parameters;

            velocity = dir * mParams.ejectionVelocity;
            //pitchVel = (float)SSpaceMissilesVisualSimulation.rand.NextDouble() * mParams.ejectionMaxRotationVel;
            //yawVel = (float)SSpaceMissilesVisualSimulation.rand.NextDouble() * mParams.ejectionMaxRotationVel;
            pitchVel = 0f;
            yawVel = 0f;
        }

        public void updateExecution(SSpaceMissileData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchAcc, ref float yawAcc)
        {
            //pitchAcc = 0.2f;
            //thrustAcc += 2f * timeElapsed;
        }
    }

    /// <summary>
    /// http://www.moddb.com/members/blahdy/blogs/gamedev-introduction-to-proportional-navigation-part-i
    /// </summary>
    public class SProportionalNavigationPursuitDriver : ISSpaceMissilePursuitDriver
    {
        public void updateExecution(SSpaceMissileData missile, float timeElapsed, out Vector3 latax)
        {
            // TODO adjust things (thrust?) so that distanceToTarget = closing velocity * timeToHit

            var mParams = missile.cluster.parameters;

            Vector3 rtmOld = _computeRtm(missile.cluster.oldTargetPos, missile.posOlder);
            Vector3 rtmNew = _computeRtm(missile.cluster.target.position, missile.posNewer);
            Vector3 losDelta = rtmNew - rtmOld;
            //float losRate = losDelta.LengthFast / timeElapsed;
            float losRate = losDelta.LengthFast;
            latax = mParams.navigationGain * losRate * losRate * rtmNew; // A = rtmNew * N * Vc * losRate
        }

        public float estimateTimeNeededToHit(SSpaceMissileData missile)
        {
            // TODO
            return 100f;
        }

        protected static Vector3 _computeRtm(Vector3 targetPos, Vector3 missilePos)
        {
            return (targetPos - missilePos).Normalized();
        }
    }
}

