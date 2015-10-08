using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileData
    {
        // TODO actual rendering constructs. Where do they go?

        public enum State { Ejection, Pursuit, Intercepted };

        public SSpaceMissileClusterData cluster { get { return _cluster; } }
        public int clusterId { get { return _clusterId; } }

        public State state { get { return _state; } }
        public Vector3 position { get { return _position; } }
        public Vector3 direction { get { return _direction; } }
        public Vector3 up { get { return _up; } }
        public float thrustAcc { get { return _thrustAcc; } }

        #if false
        public Vector3 up { get { return _up; } }

        public Vector3 velocity { get { return _velocity; } }
        public Vector3 angularVelocity { get { return _angularVelocity; } }
        public Vector3 acceleration { get { return _acceleration; } }
        public Vector3 lateralAcceleration { get { return _lateralAcceleration; } }
        #endif

        public Vector3 pitchAxis {
            get {
                return Vector3.Cross(_direction, _up).Normalized();
            }
        }

        /// <summary>
        /// The cluster this missile belongs to. Variable gives access to params, target, other missiles.
        /// </summary>
        protected readonly SSpaceMissileClusterData _cluster; 

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
        protected float _thrustAcc = 0f;

        protected Vector3 _direction = Vector3.UnitZ;
        protected Vector3 _up = Vector3.UnitY;

        protected float _pitchVel = 0f;
        protected float _pitchAcc = 0f;
        protected float _yawVel = 0f;
        protected float _yawAcc = 0f;

        protected float _timeSinceLaunch = 0f;

        public SSpaceMissileData(SSpaceMissileClusterData cluster, int clusterId,
                                        Vector3 initClusterPos, Vector3 initClusterVel, 
                                        Vector3 missilePos, float timeToHitTarget)
        {
            _cluster = cluster;
            _clusterId = clusterId;
            _position = missilePos;
            _timeToHit = timeToHitTarget;

            var ejection = _cluster.parameters.ejectionDriver;
            ejection.init(this, initClusterPos, initClusterVel, out _direction, out _up, out _velocity, out _pitchVel, out _yawVel);

            ejection.update(this, 0f, ref _thrustAcc, ref _pitchAcc, ref _yawAcc);
        }

        public void updateExecution(float timeElapsed, Vector3 prevTargetPos, float targetTimeDelta)
        {
            //_timeElapsed = timeElapsed;
            _timeSinceLaunch += timeElapsed;

            _position += _velocity * timeElapsed;
            Matrix4 changeInOrientation 
                = Matrix4.CreateFromAxisAngle(_up, _yawVel * timeElapsed)
                * Matrix4.CreateFromAxisAngle(pitchAxis, _pitchVel * timeElapsed);
            _direction = Vector3.Transform(_direction, changeInOrientation).Normalized();
            _up = Vector3.Transform(_up, changeInOrientation).Normalized();

            var mParams = _cluster.parameters;
            switch (_state) {
            case State.Ejection:
                mParams.ejectionDriver.update(this, timeElapsed, 
                    ref _thrustAcc, ref _pitchAcc, ref _yawAcc);
                if (mParams.pursuitDriver.estimateTimeNeededToHit(this) >= _timeToHit
                && _timeSinceLaunch >= mParams.minActivationTime) {
                    _state = State.Pursuit;
                }
                break;
            case State.Pursuit:
                mParams.pursuitDriver.update(this, timeElapsed, 
                    ref _thrustAcc, ref _pitchAcc, ref _yawAcc);
                break;
            case State.Intercepted:
                // todo
                break;
            }

            _pitchAcc = Math.Min(_pitchAcc, mParams.maxRotationalAcc);
            _yawAcc = Math.Min(_yawAcc, mParams.maxRotationalAcc);

            _pitchVel += _pitchAcc * timeElapsed;
            _yawVel += _yawAcc * timeElapsed;
            _velocity += _thrustAcc * _direction * timeElapsed;
        }

        public void updateTimeToHit(float timeToHit)
        {
            _timeToHit = timeToHit;
        }

        public void updateRenderingObjects(float timeElapsed)
        {
            // TODO
        }
    }

   
}

