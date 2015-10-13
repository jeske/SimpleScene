using System;
using System.Collections.Generic;
using System.Drawing; // for RectangleF
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissilesVisualSimulation
    {
        public static Random rand = new Random();

        //public float timeScale = 0.3f;
        public float timeScale = 0.3f;

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

        public void updateClusterTimeToHit(SSpaceMissileClusterData cluster, float timeToHit)
        {
            // TODO
        }

        public void removeMissile(SSpaceMissileData missile)
        {
            for (int i = 0; i < _clusters.Count; ++i) {
                var cluster = _clusters [i];
                if (cluster.removeMissile(missile)) {
                    if (cluster.allTerminated()) {
                        _clusters.RemoveAt(i);
                    }
                    return;
                }
            }
        }

        public void removeCluster(SSpaceMissileClusterData cluster)
        {
            for (int i = 0; i < _clusters.Count; ++i) {
                var cl = _clusters [i];
                if (cl == cluster) {
                    cl.terminateAll();
                    _clusters.RemoveAt(i);
                }
            }
        }

        public void removeAll()
        {
            foreach (var cluster in _clusters) {
                cluster.terminateAll();
            }
            _clusters.Clear();
        }

        public void updateSimulation(float timeElapsed)
        {
            timeElapsed *= timeScale;
            foreach (var cluster in _clusters) {
                cluster.updateSimulation(timeElapsed);
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
        public ISSpaceMissileTarget target { get { return _target; } }
        public float timeToHit { get { return _timeToHit; } }
        public float timeSinceLaunch { get { return _timeSinceLaunch; } }

        protected readonly SSpaceMissileData[] _missiles;
        protected readonly ISSpaceMissileTarget _target;
        protected readonly SSpaceMissileVisualParameters _parameters;

        protected float _timeDeltaAccumulator = 0f;
        protected float _timeSinceLaunch = 0f;
        protected float _timeToHit = 0f;

        public SSpaceMissileClusterData(
            Vector3 launcherPos, Vector3 launcherVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualParameters mParams)
        {
            _target = target;
            _timeToHit = timeToHit;
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

        public void updateTimeToHit(float timeToHit)
        {
            _timeToHit = timeToHit;
            // TODO what happens to missiles?
        }

        /// <summary> Remove true if missile was removed </summary>
        public bool removeMissile(SSpaceMissileData missile)
        {
            foreach (var m  in _missiles) {
                if (m == missile) {
                    m.terminate();
                    return true;
                }
            }
            return false;
        }

        public void terminateAll()
        {
            foreach (var missile in _missiles) {
                missile.terminate();
            }
        }

        public bool allTerminated()
        {
            foreach (var m  in _missiles) {
                if (m.state != SSpaceMissileData.State.Terminated) {
                    return false;
                }
            }
            return true;
        }

        public void updateSimulation(float timeElapsed)
        {
            float accTime = timeElapsed + _timeDeltaAccumulator;
            while (accTime >= _parameters.simulationStep) {
                _simulateStep(parameters.simulationStep);
                accTime -= _parameters.simulationStep;
            }
            _timeDeltaAccumulator = accTime;
        }

        protected void _simulateStep(float timeElapsed)
        {
            foreach (var missile in _missiles) {
                if (missile.state != SSpaceMissileData.State.Terminated) {
                    missile.updateExecution(timeElapsed);
                }
            }
            _timeToHit -= timeElapsed;
            _timeSinceLaunch += timeElapsed;
        }
    }
}

