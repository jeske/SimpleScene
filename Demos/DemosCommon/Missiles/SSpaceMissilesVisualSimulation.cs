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

    public class SSpaceMissilesVisualSimulation
    {
        public float timeScale = 1f;

        protected List<SSpaceMissileVisualSimCluster> _clusters
            = new List<SSpaceMissileVisualSimCluster>();

        public SSpaceMissileVisualSimCluster launchCluster(
            Vector3 launchPos, Vector3 launchVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters clusterParams)
        {
            var cluster = new SSpaceMissileVisualSimCluster (
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
        public void interceptMissile(SSpaceMissileVisualizationData missile, float delay)
        {
            // TODO
        }


    }

    public class SSpaceMissileVisualSimCluster
    {
        public SSpaceMissileVisualizationData[] missiles { get { return _missiles; } }
        public SSpaceMissileVisualizerParameters parameters { get { return _parameters; } }
        //public ISSpaceMissileTarget target { get { return _target; } }
        //public Vector3 prevTargetPos { get { return _prevTargetPos; } }

        protected readonly SSpaceMissileVisualizationData[] _missiles;
        protected readonly ISSpaceMissileTarget _target;
        protected readonly SSpaceMissileVisualizerParameters _parameters;

        protected Vector3 _prevTargetPos;
        protected float _timeDeltaAccumulator = 0f;

        public SSpaceMissileVisualSimCluster(
            Vector3 launcherPos, Vector3 launcherVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualizerParameters mParams)
        {
            _target = target;
            _prevTargetPos = target.position;
            _parameters = mParams;
            _missiles = new SSpaceMissileVisualizationData[numMissiles];

            Matrix4 spawnTxfm = _parameters.spawnTxfm(_target, launcherPos, launcherVel);
            _parameters.spawnGenerator.Generate(numMissiles,
                (i, scale, pos, orient) => {
                    Vector3 missilePos = pos * _parameters.spawnDistanceScale;
                    missilePos = Vector3.Transform(missilePos, spawnTxfm);
                    _missiles [i] = new SSpaceMissileVisualizationData (
                        this, i, missilePos, launcherVel, timeToHit);
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

    public class SSpaceMissileVisualizerParameters
    {
        public delegate Matrix4 SpawnTxfmDelegate(ISSpaceMissileTarget target, 
                                                  Vector3 clusterPos, Vector3 clusterVel);

        #region visual simulation parameters
        public float simulationStep = 0.05f;

        public float minActivationTime = 1f;
        public float maxRotationalAcc = 0.2f;

        public BodiesFieldGenerator spawnGenerator 
            = new BodiesFieldGenerator(new ParticlesSphereGenerator(Vector3.Zero, 1f));
        public SpawnTxfmDelegate spawnTxfm 
            = (target, clusterPos, clusterVel) => { return Matrix4.CreateTranslation(clusterPos); };
        public float spawnDistanceScale = 10f;

        public ISSpaceMissileEjectionDriver ejectionDriver
            = new SSimpleMissileEjectionDriver();
        public ISSpaceMissilePursuitDriver pursuitDriver
            = new SProportionalNavigationPursuitDriver();

        // TODO: fuel strategy???
        #endregion

        #region render parameters
        public SSAbstractMesh missileMesh
            = SSAssetManager.GetInstance<SSMesh_wfOBJ>("missiles", "missile.obj");
        /// <summary>
        /// orientation needed to make the missile mesh face into positive X axis
        /// </summary>
        public Quaternion missileMeshOrient = Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI);
        public float missileScale = 1f;
        // TODO flame tex, smoke tex
        #endregion
    }
}

