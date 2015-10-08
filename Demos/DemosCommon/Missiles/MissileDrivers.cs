using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public interface ISSpaceMissileEjectionDriver
    {
        // Note the position is already assigned by field generator
        void init(SSpaceMissileVisualizationData missile, Vector3 clusterInitPos, Vector3 clusterInitVel,
            out Vector3 dir, out Vector3 up, out Vector3 velocity, out float pitchVel, out float yawVel);
        void update(SSpaceMissileVisualizationData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel);
    }

    public interface ISSpaceMissilePursuitDriver
    {
        void update(SSpaceMissileVisualizationData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel);
        float estimateTimeNeededToHit(SSpaceMissileVisualizationData missile); 
    }

    public class SSimpleMissileEjectionDriver : ISSpaceMissileEjectionDriver
    {
        public void init(SSpaceMissileVisualizationData missile, Vector3 clusterInitPos, Vector3 clusterInitVel,
            out Vector3 dir, out Vector3 up, out Vector3 velocity, out float pitchVel, out float yawVel)
        {
            var cluster = missile.cluster;
            dir = (missile.position - clusterInitPos).Normalized();
            //dir = Vector3.UnitZ;
            Vector3 dummy;
            OpenTKHelper.TwoPerpAxes(dir, out dummy, out up);

            velocity = dir * 5.0f;
            pitchVel = 0f;
            yawVel = 0f;
        }

        public void update(SSpaceMissileVisualizationData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel)
        {
            // do nothing?
        }
    }

    public class SProportionalNavigationPursuitDriver : ISSpaceMissilePursuitDriver
    {
        public void update(SSpaceMissileVisualizationData missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel)
        {
            // TODO proportional navigation
            // TODO adjust things (thrust?) so that distanceToTarget = closing velocity * timeToHit
        }

        public float estimateTimeNeededToHit(SSpaceMissileVisualizationData missile)
        {
            // TODO
            return 100f;
        }
    }
}

