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
            Vector3 launchPos, Vector3 launchVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters clusterParams)
        {
            var cluster = new SSpaceMissileVisualizerCluster (
              launchPos, launchVel, numMissiles, target, timeToHit, clusterParams);
            _clusters.Add(cluster);

            // TODO remove cluster: auto -OR- manual
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
        public Vector3 prevTargetPos { get { return _prevTargetPos; } }

        protected Vector3 _prevTargetPos;
        protected float _timeDeltaAccumulator = 0f;

        public SSpaceMissileVisualizerCluster(
            Vector3 initClusterPos, Vector3 initClusterVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters mParams)
        {
            _target = target;
            _prevTargetPos = target.position;
            _parameters = mParams;
            _missiles = new SSpaceMissileVisualizion[numMissiles];

            _parameters.spawnGen.Generate(numMissiles,
                (i, scale, pos, orient) => {
                    Vector3 missilePos = initClusterPos + pos * _parameters.spawnMaxDistance;
                    _missiles [i] = new SSpaceMissileVisualizion (
                        this, i, missilePos, initClusterVel, timeToHit);
                    return true; // accept new missile from the generator
                }
            );
        }

        public void updateVisualization(float timeElapsed)
        {
            timeElapsed += _timeDeltaAccumulator;
            while (timeElapsed >= _parameters.simulationStep) {
                foreach (var missile in _missiles) {
                    missile.updateExecution(timeElapsed, _prevTargetPos);
                }
                timeElapsed -= _parameters.simulationStep;
            }
            _timeDeltaAccumulator = timeElapsed;
            _prevTargetPos = _target.position;
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
        public float simulationStep = 0.05f;

        public float minActivationTime = 1f;
        public float maxRotationalAcc = 0.2f;

        public BodiesFieldGenerator spawnGen = new BodiesFieldGenerator(
            new ParticlesSphereGenerator(Vector3.Zero, 1f));
        public float spawnMaxDistance = 10f;

        public ISSpaceMissileEjectionDriver ejectionDriver; // TODO = ?
        public ISSpaceMissilePursuitDriver pursuitDriver; // TODO =

        // TODO: max lateral acceleration?
        // TODO throw/programmed maneuver pattern
        // TODO: different homing behavior types?
        // TODO: fuel strategy?
    }
}

