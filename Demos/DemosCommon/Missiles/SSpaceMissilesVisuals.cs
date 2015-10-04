using System;
using System.Collections.Generic;
using OpenTK;

namespace SimpleScene.Demos
{
    /// <summary>
    /// Missile target provides target position, velocity, and acceleration
    /// </summary>
    public interface ISSpaceMissileTarget
    {
        Vector3 position { get; }
        Vector3 velocity { get; }
        Vector3 acceleration { get; } // make optional?
        bool isAlive{ get; } // target is structurally intact
    };

    public class SSpaceMissilesVisuals
    {
        protected List<SSpaceMissileVisualizerCluster> _clusters
            = new List<SSpaceMissileVisualizerCluster>();

        public void launchCluster(
            Vector3 launchPos, Vector3 launchVel, 
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters clusterParams)
        {

        }

        public void updateVisualization(float timeElapsed)
        {
            foreach (var cluster in _clusters) {
                cluster.updateVisualization(timeElapsed);
            }
        }

        /// <summary>
        /// Terminate a missile in a cluster before it reaches its target
        /// </summary>
        public void interceptMissile(SSpaceMissileVisualizion missile, float delay)
        {
            // TODO
        }


    }

    public class SSpaceMissileVisualizerCluster
    {
        protected readonly SSpaceMissileVisualizion[] _missiles;
        protected readonly ISSpaceMissileTarget _target;
        protected readonly SSpaceMissileVisualizerParameters _parameters;

        public SSpaceMissileVisualizerParameters parameters { get { return _parameters; } }
        public ISSpaceMissileTarget target { get { return _target; } }

        SSpaceMissileVisualizerCluster(
            Vector3 initPos, Vector3 initVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters mParams)
        {
            _target = target;
            _parameters = mParams;
            _missiles = new SSpaceMissileVisualizion[numMissiles];
            for (int i = 0; i < numMissiles; ++i) {
                _missiles [i] = new SSpaceMissileVisualizion (this, i, initPos, initVel, timeToHit);
            }
        }

        public void updateVisualization(float timeElapsed)
        {
            foreach (var missile in _missiles) {
                missile.updateExecution(timeElapsed);
            }
        }

        public void updateTimeToHit(float timeToHit)
        {
            foreach (var missile in _missiles) {
                missile.updateTimeToHit(timeToHit);
            }
        }
    }

    public class SSpaceMissileVisualizerParameters
    {
        public float minActivationTime = 1f;

        public ISSpaceMissileEjectionDriver ejectionDriver; // TODO = ?
        public ISSpaceMissileEjectionDriver pursuitDriver; // TODO =

        // TODO: max lateral acceleration?
        // TODO throw/programmed maneuver pattern
        // TODO: different homing behavior types?
        // TODO: fuel strategy?
    }
}

