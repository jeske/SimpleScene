using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public interface ISSpaceMissileEjectionDriver
    {
        // Note the position is already assigned by field generator
        void init(SSpaceMissileVisualizion missile,
            out Vector3 dir, out Vector3 up, out Vector3 velocity, out float pitchVel, out float yawVel);
        void update(SSpaceMissileVisualizion missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel);
    }

    public interface ISSpaceMissilePursuitDriver
    {
        void update(SSpaceMissileVisualizion missile, float timeElapsed, 
            ref float thrustAcc, ref float pitchVel, ref float yawVel);
        float estimateTimeNeededToHit(SSpaceMissileVisualizion missile); 
    }


}

