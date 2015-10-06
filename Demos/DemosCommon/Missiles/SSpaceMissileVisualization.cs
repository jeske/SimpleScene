using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileVisualizion
    {
        // TODO actual rendering constructs. Where do they go?

        public enum State { Ejection, Pursuit, Intercepted };

        public SSpaceMissileVisualizerCluster cluster { get { return _cluster; } }
        public int clusterId { get { return _clusterId; } }

        public Vector3 position { get { return _position; } }
        public Vector3 velocity { get { return _velocity; } }
        public Vector3 angularVelocity { get { return _angularVelocity; } }
        public Vector3 acceleration { get { return _acceleration; } }
        public Vector3 direction { get { return _direction; } }
        public Vector3 lateralAcceleration { get { return _lateralAcceleration; } }
        public float thrust { get { return _thrust; } }

        /// <summary>
        /// The cluster this missile belongs to. Variable gives access to params, target, other missiles.
        /// </summary>
        protected readonly SSpaceMissileVisualizerCluster _cluster; 

        /// <summary>
        /// ID within a cluster. Can be referenced by ejection/pursuit behaviors.
        /// </summary>
        protected readonly int _clusterId;

        /// <summary>
        /// High level state of what the missile is doing
        /// </summary>
        protected State _state = State.Ejection;

        /// <summary>
        /// Time before before the target must be hit. Can get updated from an external simulation.
        /// </summary>
        protected float _timeToHit = 0f;

        protected Vector3 _position = Vector3.Zero;
        protected Vector3 _velocity = Vector3.Zero;
        protected Vector3 _angularVelocity = Vector3.Zero;
        protected Vector3 _acceleration = Vector3.Zero;
        protected Vector3 _lateralAcceleration = Vector3.Zero;
        protected Vector3 _direction = Vector3.Zero;
        protected float _thrust = 0f;

        protected float _timeElapsed = 0f;
        protected float _localTime = 0f;
        protected Vector3 _prevTargetDir;

        public SSpaceMissileVisualizion(SSpaceMissileVisualizerCluster cluster, int clusterId,
                                        Vector3 initClusterPos, Vector3 initClusterVel, float timeToHit)
        {
            _cluster = cluster;
            _clusterId = clusterId;
            _timeToHit = timeToHit;

            var ejection = _cluster.parameters.ejectionDriver;
            ejection.init(_cluster, ref _direction, ref _velocity, ref _angularVelocity);

            _prevTargetDir = (cluster.target.position - _position).Normalized();
            ejection.update(this, 0f, ref _thrust, ref _lateralAcceleration);
        }

        public void updateExecution(float timeElapsed)
        {
            _timeElapsed = timeElapsed;
            _localTime += timeElapsed;

            var mParams = _cluster.parameters;
            switch (_state) {
            case State.Ejection:
                mParams.ejectionDriver.update(this, timeElapsed, ref _thrust, ref _lateralAcceleration);
                if (mParams.pursuitDriver.estimateTimeNeededToHit(this) >= _timeToHit
                && _localTime >= mParams.minActivationTime) {
                    _state = State.Pursuit;
                }
                break;
            case State.Pursuit:
                mParams.pursuitDriver.update(this, timeElapsed, ref _thrust, ref _lateralAcceleration);
                break;
            case State.Intercepted:
                // todo
                break;
            }
        }

        public void updateTimeToHit(float timeToHit)
        {
            _timeToHit = timeToHit;
            // TODO adjust things (thrust?) so that distanceToTarget = closing velocity * timeToHit
        }

        public void updateRenderingObjects(float timeElapsed)
        {
            // TODO
        }
    }

   
}

