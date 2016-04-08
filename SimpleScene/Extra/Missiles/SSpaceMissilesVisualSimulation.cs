using System;
using System.Collections.Generic;
using System.Drawing; // for RectangleF
using System.Diagnostics; // stopwatch
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissilesVisualSimulation
    {
        /// <summary> shared randomizer object </summary>
        public static Random rand = new Random();

        /// <summary> delta time multiplier </summary>
        public float timeScale = 1f;

        /// <summary> interval at which target velocity/acc/etc can be updated </summary>
        public float targetUpdateInterval = 0.2f;

        /// <summary> number of missile clusters currently active </value>
        public int numClusters { get { return _clusters.Count; } }

        protected readonly List<SSpaceMissileClusterVisualData> _clusters
            = new List<SSpaceMissileClusterVisualData>();

        #region targets
        protected readonly HashSet<ISSpaceMissileTarget> _targets = new HashSet<ISSpaceMissileTarget>();
        protected float _timeDeltaAccumulator = 0f;
        #endregion

        protected Stopwatch _stopwatch = new Stopwatch ();

        public SSpaceMissileClusterVisualData launchCluster(
            Matrix4 launcherWorldMat, Vector3 launchVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualParameters clusterParams,
            Vector3[] localPositioningOffsets = null,
            Vector3[] localDirectionPresets = null,
            BodiesFieldGenerator meshPositioningGenerator = null,
            SSpaceMissileVisualData.AtTargetFunc atTargetFunc = null
        )
        {
            var cluster = new SSpaceMissileClusterVisualData (
                launcherWorldMat, launchVel, numMissiles, target, timeToHit, clusterParams, 
                localPositioningOffsets, localDirectionPresets, meshPositioningGenerator,
                atTargetFunc
            );
            _clusters.Add(cluster);
            _targets.Add(target);
            return cluster;
        }

        public void removeMissile(SSpaceMissileVisualData missile)
        {
            missile.terminate();
        }

        public void removeCluster(SSpaceMissileClusterVisualData cluster)
        {
            cluster.terminateAll();
        }

        public void removeAll()
        {
            foreach (var cluster in _clusters) {
                cluster.terminateAll();
            }
            _clusters.Clear();
        }

        public void updateSimulation(float unused)
        {
            float timeElapsed = (float)_stopwatch.ElapsedMilliseconds / 1000f;
            _stopwatch.Restart();

            timeElapsed *= timeScale;

            // update targets
            float accTime = timeElapsed + _timeDeltaAccumulator;
            while (accTime >= targetUpdateInterval) {
                foreach (var target in _targets) {
                    target.update(targetUpdateInterval);
                }
                accTime -= targetUpdateInterval;
            }
            _timeDeltaAccumulator = accTime;

            // update clusters/missiles
            bool skipRemove = true;
            foreach (var cluster in _clusters) {
                cluster.updateSimulation(timeElapsed);
                if (skipRemove && cluster.isTerminated) {
                    skipRemove = false;
                }
            }

            // remove missile clusters that are fully terminated
            if (!skipRemove) {
                _clusters.RemoveAll((cluster) => cluster.isTerminated);
            }
        }
    }

    /// <summary> Missile cluster contains missiles and their shared data </summary>
    public class SSpaceMissileClusterVisualData
    {
        public SSpaceMissileVisualData.AtTargetFunc atTargetFunc = null;

        public SSpaceMissileVisualData[] missiles { get { return _missiles; } }
        public SSpaceMissileVisualParameters parameters { get { return _parameters; } }
        public ISSpaceMissileTarget target { get { return _target; } }
        public float timeToHit { 
            get { return _timeToHit; } 
            set { _timeToHit = value; }
        }
        public float timeSinceLaunch { 
            get { return _timeSinceLaunch; } 
            set { timeSinceLaunch = value; }
        }
        public bool isTerminated { get { return _isTerminated; } }

        protected readonly SSpaceMissileVisualData[] _missiles;
        protected readonly ISSpaceMissileTarget _target;
        protected readonly SSpaceMissileVisualParameters _parameters;

        protected float _timeDeltaAccumulator = 0f;
        protected float _timeSinceLaunch = 0f;
        protected float _timeToHit = 0f;
        protected bool _isTerminated = false;

        public SSpaceMissileClusterVisualData(
            Matrix4 launcherWorldMat, Vector3 launcherVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualParameters mParams,
            Vector3[] meshPositioningOffsets = null,
            Vector3[] meshPositioningDirections = null,
            BodiesFieldGenerator meshPositioningGenerator = null,
            SSpaceMissileVisualData.AtTargetFunc atTargetFunc = null)
        {
            _target = target;
            _timeToHit = timeToHit;
            _parameters = mParams;
            _missiles = new SSpaceMissileVisualData[numMissiles];
            this.atTargetFunc = atTargetFunc;

            Vector3[] localSpawnPts = new Vector3[numMissiles];
            Quaternion[] localSpawnOrients = new Quaternion[numMissiles];
            if (meshPositioningGenerator != null) {
                meshPositioningGenerator.Generate(numMissiles,
                    (id, scale, pos, orient) => {
                        localSpawnPts [id] = pos;
                        localSpawnOrients [id] = orient;
                        return true;
                    }
                );
            }

            Quaternion launcherOrientation = launcherWorldMat.ExtractRotation();
            for (int i = 0; i < numMissiles; ++i) {
                if (meshPositioningOffsets != null && meshPositioningOffsets.Length > 0) {
                    localSpawnPts [i] += meshPositioningOffsets [i % meshPositioningOffsets.Length];
                }
                if (meshPositioningDirections != null && meshPositioningDirections.Length > 0) {
                    int idx = i % meshPositioningDirections.Length;
                    localSpawnOrients [i] *= OpenTKHelper.getRotationTo(
                        Vector3.UnitZ, meshPositioningDirections [idx], Vector3.UnitZ);
                }
                Vector3 missileWorldPos = Vector3.Transform(localSpawnPts [i], launcherWorldMat);
                Vector3 missileLocalDir = Vector3.Transform(Vector3.UnitZ, localSpawnOrients [i]);
                Vector3 missileWorldDir = Vector3.Transform(missileLocalDir, launcherOrientation);
                Vector3 missileWorldVel = launcherVel + missileWorldDir * mParams.ejectionVelocity;

                _missiles [i] = mParams.createMissile(
                    missileWorldPos, missileWorldDir, missileWorldVel, this, i);

                #if false
                _missiles [i] = new SSpaceMissileVisualData (
                    missileWorldPos, missileWorldDir, missileWorldVel,
                    this, clusterId: i);
                #endif
            }
        }

        public void updateTimeToHit(float timeToHit)
        {
            _timeToHit = timeToHit;
        }

        public void terminateAll()
        {
            foreach (var missile in _missiles) {
                missile.terminate();
            }
            _isTerminated = true;
        }

        public void updateSimulation(float timeElapsed)
        {
            float accTime = timeElapsed + _timeDeltaAccumulator;
            float step = parameters.simulationStep;
            while (accTime >= step) {
                _simulateStep(step);
                accTime -= step;
            }
            _timeDeltaAccumulator = accTime;
        }

        protected void _simulateStep(float timeElapsed)
        {
            bool isTerminated = true;
            foreach (var missile in _missiles) {
                if (missile.state != SSpaceMissileVisualData.State.Terminated) {
                    isTerminated = false;
                    missile.updateExecution(timeElapsed);
                }
            }
            _isTerminated = isTerminated;
            _timeToHit -= timeElapsed;
            _timeSinceLaunch += timeElapsed;
        }
    }
}

