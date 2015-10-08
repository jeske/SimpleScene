using System;
using System.Collections.Generic;
using System.Drawing; // for RectangleF
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissilesVisualSimulation
    {
        public float timeScale = 1f;

        protected List<SSpaceMissileClusterData> _clusters
            = new List<SSpaceMissileClusterData>();

        public SSpaceMissileClusterData launchCluster(
            Vector3 launchPos, Vector3 launchVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualParameters clusterParams)
        {
            var cluster = new SSpaceMissileClusterData (
              launchPos, launchVel, numMissiles, target, timeToHit, clusterParams);
            _clusters.Add(cluster);
            return cluster;

            // TODO remove cluster: auto -OR- manual
 
        }

        public void updateVisualization(float timeElapsed)
        {
            timeElapsed *= timeScale;
            foreach (var cluster in _clusters) {
                cluster.updateVisualization(timeElapsed);
            }
        }

        /// <summary>
        /// Terminate a missile in a cluster before it reaches its target
        /// </summary>
        public void interceptMissile(SSpaceMissileData missile, float delay)
        {
            // TODO
        }


    }

    public class SSpaceMissileClusterData
    {
        public SSpaceMissileData[] missiles { get { return _missiles; } }
        public SSpaceMissileVisualParameters parameters { get { return _parameters; } }
        //public ISSpaceMissileTarget target { get { return _target; } }
        //public Vector3 prevTargetPos { get { return _prevTargetPos; } }

        protected readonly SSpaceMissileData[] _missiles;
        protected readonly ISSpaceMissileTarget _target;
        protected readonly SSpaceMissileVisualParameters _parameters;

        protected Vector3 _prevTargetPos;
        protected float _timeDeltaAccumulator = 0f;

        public SSpaceMissileClusterData(
            Vector3 launcherPos, Vector3 launcherVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualParameters mParams)
        {
            _target = target;
            _prevTargetPos = target.position;
            _parameters = mParams;
            _missiles = new SSpaceMissileData[numMissiles];

            Matrix4 spawnTxfm = _parameters.spawnTxfm(_target, launcherPos, launcherVel);
            _parameters.spawnGenerator.Generate(numMissiles,
                (i, scale, pos, orient) => {
                    Vector3 missilePos = pos * _parameters.spawnDistanceScale;
                    missilePos = Vector3.Transform(missilePos, spawnTxfm);
                    _missiles [i] = new SSpaceMissileData (
                        this, i, launcherPos, launcherVel, missilePos, timeToHit);
                    return true; // accept new missile from the generator
                }
            );
        }

        public void updateVisualization(float timeElapsed)
        {
            float accTime = timeElapsed + _timeDeltaAccumulator;
            while (accTime >= _parameters.simulationStep) {
                foreach (var missile in _missiles) {
                    missile.updateExecution(_parameters.simulationStep, _prevTargetPos, timeElapsed);
                }
                accTime -= _parameters.simulationStep;
            }
            _timeDeltaAccumulator = accTime;
            _prevTargetPos = _target.position;
        }

        public void updateTimeToHit(float timeToHit)
        {
            foreach (var missile in _missiles) {
                missile.updateTimeToHit(timeToHit);
            }
        }
    }
}

