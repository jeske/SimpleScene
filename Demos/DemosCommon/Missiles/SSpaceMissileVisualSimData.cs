using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileData
    {
        // TODO actual rendering constructs. Where do they go?

        public enum State { Ejection, Pursuit, AtTarget, Intercepted, Terminated };

        public SSpaceMissileClusterData cluster { get { return _cluster; } }
        public int clusterId { get { return _clusterId; } }

        public State state { get { return _state; } }
        public Vector3 position { get { return _position; } }
        public Vector3 velocity { get { return _velocity; } }
        //public Vector3 direction { get { return _direction; } }
        public Vector3 up { 
            get { 
                Vector3 ret, dummy;
                OpenTKHelper.TwoPerpAxes(_direction, out ret, out dummy);
                return ret;
            } 
        }

        // hacky/messy
        public Vector3 direction { get { return _velocity.Normalized(); } }
        public float thrustAcc { get { return _velocity.LengthFast; } }

        #region proportional navigation
        public Vector3 posOlder { get { return _posOlder; } }
        public Vector3 posNewer { get { return _posNewer; } }
        #endregion

        #if false
        public Vector3 up { get { return _up; } }

        public Vector3 velocity { get { return _velocity; } }
        public Vector3 angularVelocity { get { return _angularVelocity; } }
        public Vector3 acceleration { get { return _acceleration; } }
        public Vector3 lateralAcceleration { get { return _lateralAcceleration; } }
        #endif

        /// <summary>
        /// The cluster this missile belongs to. Variable gives access to params, target, other missiles.
        /// </summary>
        protected readonly SSpaceMissileClusterData _cluster; 

        /// <summary>
        /// ID within a cluster. Can be referenced by ejection/pursuit behaviors.
        /// </summary>
        protected readonly int _clusterId;

        /// <summary>
        /// Time before before the target must be hit. Can get updated from an external simulation.
        /// </summary>
        protected float _timeToHit = 0f;

        #region simulation basics
        protected Vector3 _position = Vector3.Zero;
        protected Vector3 _velocity = Vector3.Zero;
        //protected float _pitchVel = 0f;
        //protected float _yawVel = 0f;
        protected Vector3 _direction = Vector3.UnitZ;
        //protected Vector3 _up = Vector3.UnitY;
        protected float _timeSinceLaunch = 0f;
        #endregion

        #region missile controls
        /// <summary>
        /// High level state of what the missile is doing
        /// </summary>
        protected State _state = State.Ejection;

        protected float _thrustAcc = 0f;
        protected float _pitchAcc = 0f;
        protected float _yawAcc = 0f;
        #endregion

        #region navigation aid
        protected Vector3 _posOlder;
        protected Vector3 _posNewer;
        #endregion

        public SSpaceMissileData(SSpaceMissileClusterData cluster, int clusterId,
                                        Vector3 initClusterPos, Vector3 initClusterVel, 
                                        Vector3 missilePos, float timeToHitTarget)
        {
            _cluster = cluster;
            _clusterId = clusterId;
            _timeToHit = timeToHitTarget;
            _position = missilePos;

            var ejection = _cluster.parameters.ejectionDriver;
            float dummy;
            ejection.init(this, initClusterPos, initClusterVel, out _direction, out _velocity, out dummy, out dummy);

            ejection.updateExecution(this, 0f, ref _thrustAcc, ref dummy, ref dummy);
        }

        public void terminate()
        {
            _state = State.Terminated;
            System.Console.WriteLine("missile terminated on demand");
        }

        public void updateExecution(float timeElapsed)
        {
            updateNavigationData(timeElapsed);

            _timeSinceLaunch += timeElapsed;

            _position += _velocity * timeElapsed;
            //_direction = Vector3.Transform(_direction, changeInOrientation).Normalized();
            //_up = Vector3.Transform(_up, changeInOrientation).Normalized();

            var mParams = _cluster.parameters;
            switch (_state) {
            case State.Ejection:
                float dummy = 1f;
                mParams.ejectionDriver.updateExecution(this, timeElapsed, 
                    ref _thrustAcc, ref dummy, ref dummy);
                if (mParams.pursuitDriver.estimateTimeNeededToHit(this) >= _timeToHit
                && _timeSinceLaunch >= mParams.minActivationTime) {
                    System.Console.WriteLine("pursuit activated at t = " + _timeSinceLaunch);
                    _state = State.Pursuit;
                    _posOlder = _posNewer = _position;
                }
                break;
            case State.Pursuit:
                Vector3 latax;
                mParams.pursuitDriver.updateExecution(this, timeElapsed, out latax);
                var oldMagnitude = _velocity.LengthFast;
                latax.Normalize();
                latax *= (oldMagnitude * timeElapsed);
                _velocity += latax;
                float r = _velocity.Length / oldMagnitude;
                if (r > 1f) {
                    _velocity /= r;
                }

                if ((position - cluster.target.position).LengthFast <= mParams.terminateWhenAtTargetDist) {
                    System.Console.WriteLine("missile at target at t = " + _timeSinceLaunch);
                    _state = State.AtTarget;
                }
                //_velocity += _velocity.Normalized() * 0.2f;

                break;
            case State.AtTarget:
                if (mParams.terminateWhenAtTarget) {
                    System.Console.WriteLine("missile terminated at target at t = " + _timeSinceLaunch);
                    _state = State.Terminated;
                }
                break;
            case State.Intercepted:
                // todo
                break;
            case State.Terminated:
                throw new Exception ("missile state machine still running in a terminated state");
                break;

            }

            //_pitchAcc = Math.Min(_pitchAcc, mParams.maxRotationalAcc);
            //_yawAcc = Math.Min(_yawAcc, mParams.maxRotationalAcc);

            //_pitchVel += _pitchAcc * timeElapsed;
            //_yawVel += _yawAcc * timeElapsed;
            //_velocity += _thrustAcc * _direction * timeElapsed;
        }

        public void updateNavigationData(float timeElapsed)
        {
            _posOlder = _posNewer;
            _posNewer = _position;
        }

        public void updateTimeToHit(float timeToHit)
        {
            _timeToHit = timeToHit;
        }
    }  
}

