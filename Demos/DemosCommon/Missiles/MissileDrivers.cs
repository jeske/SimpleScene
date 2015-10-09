using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public interface ISSpaceMissileEjectionDriver
    {
        // Note the position is already assigned by field generator
        void init(SSpaceMissileData missile, Vector3 clusterInitPos, Vector3 clusterInitVel,
            out Vector3 dir, out Vector3 up, out Vector3 velocity, out float pitchVel, out float yawVel);
        void update(SSpaceMissileData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel);
    }

    public interface ISSpaceMissilePursuitDriver
    {
        void update(SSpaceMissileData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel);
        float estimateTimeNeededToHit(SSpaceMissileData missile); 
    }

    public class SSimpleMissileEjectionDriver : ISSpaceMissileEjectionDriver
    {
        public void init(SSpaceMissileData missile, Vector3 clusterInitPos, Vector3 clusterInitVel,
            out Vector3 dir, out Vector3 up, out Vector3 velocity, out float pitchVel, out float yawVel)
        {
            var cluster = missile.cluster;
            dir = (missile.position - clusterInitPos).Normalized();
            //dir = Vector3.UnitZ;
            Vector3 dummy;
            OpenTKHelper.TwoPerpAxes(dir, out dummy, out up);

            velocity = Vector3.Zero; //dir * 2.0f;
            pitchVel = 0.1f;
            yawVel = 0f;
        }

        public void update(SSpaceMissileData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchAcc, ref float yawAcc)
        {
            //pitchAcc = 0.2f;
            thrustAcc += 2f * timeElapsed;
        }
    }

    public class SProportionalNavigationPursuitDriver : ISSpaceMissilePursuitDriver
    {
        public void update(SSpaceMissileData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel)
        {
            // TODO proportional navigation
            // TODO adjust things (thrust?) so that distanceToTarget = closing velocity * timeToHit
        }

        public float estimateTimeNeededToHit(SSpaceMissileData missile)
        {
            // TODO
            return 100f;
        }
    }
}

