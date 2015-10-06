using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public interface ISSpaceMissileEjectionDriver
    {
        // TODO who decides when the ejection phase is over??
        void init(SSpaceMissileVisualizerCluster cluster,
            ref Vector3 dir, ref Vector3 velocity, ref Vector3 angularVelocity);
        void update(SSpaceMissileVisualizion missile, float timeElapsed, 
            ref float thrust, ref Vector3 lateralAcc);
    }

    public interface ISSpaceMissilePursuitDriver
    {
        void update(SSpaceMissileVisualizion missile, float timeElapsed,
            ref float thrust, ref Vector3 lateralAcc);
        float estimateTimeNeededToHit(SSpaceMissileVisualizion missile); 
    }
}

