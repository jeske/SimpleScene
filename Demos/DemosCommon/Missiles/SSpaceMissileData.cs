using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileData
    {
        // TODO actual rendering constructs. Where do they go?

        public enum State { Ejection, Pursuit, AtTarget, Intercepted, Terminated };

        #region accessors to the internally managed
        public SSpaceMissileClusterData cluster { get { return _cluster; } }
        public int clusterId { get { return _clusterId; } }
        public State state { get { return _state; } }
        public Vector3 position { get { return _position; } }
        #endregion

        #region mutable by drivers
        public Vector3 velocity = Vector3.Zero;

        /// <summary> direction where the missile is visually facing </summary>>
        public Vector3 visualDirection = Vector3.UnitZ;

        /// <summary> How much smoke is there? [0-1] </summary>
        public float visualSmokeAmmount = 0.5f;

        /// <summary> Size of smoke sprites [0-1] </summary>
        public float visualSmokeSize = 0.5f;
        #endregion

        #region render helpers
        public Vector3 up { 
            get { 
                Quaternion quat = OpenTKHelper.neededRotation(Vector3.UnitZ, visualDirection);
                return Vector3.Transform(Vector3.UnitY, quat);
            } 
        }
        public Vector3 pitchAxis { 
            get { 
                Quaternion quat = OpenTKHelper.neededRotation(Vector3.UnitZ, visualDirection);
                return Vector3.Transform(Vector3.UnitX, quat);
            } 
        }
        #endregion

        #region for debugging only
        public Vector3 _lataxDebug = Vector3.Zero;
        public Vector3 _hitTimeCorrAccDebug = Vector3.Zero;
        #endregion

        #region internally managed
        /// <summary> The cluster this missile belongs to </summary>
        protected readonly SSpaceMissileClusterData _cluster; 

        /// <summary> ID within a cluster. Can be referenced by ejection/pursuit behaviors. </summary>
        protected readonly int _clusterId;

        /// <summary> High level state of what the missile is doing </summary>
        protected State _state = State.Ejection;

        /// <summary> Currently active missile driver </summary>
        protected ISSpaceMissileDriver _driver = null;

        protected Vector3 _position = Vector3.Zero;
        #endregion

        public SSpaceMissileData(SSpaceMissileClusterData cluster, int clusterId,
                                        Vector3 initClusterPos, Vector3 initClusterVel, 
                                        Vector3 missilePos, float timeToHitTarget)
        {
            _cluster = cluster;
            _clusterId = clusterId;
            _state = State.Ejection;
            _position = missilePos;

            _driver = _cluster.parameters.createEjection(this, initClusterPos, initClusterVel);
            _driver.updateExecution(0f);
        }

        public Vector3 jetPosition()
        {
            var mParams = cluster.parameters;
            return this.position - this.visualDirection * mParams.missileScale * mParams.jetPosition;
        }

        public void terminate()
        {
            _state = State.Terminated;
            _driver = null;
            System.Console.WriteLine("missile terminated on request at t = " + _cluster.timeSinceLaunch);
        }

        public void updateExecution(float timeElapsed)
        {
            _position += velocity * timeElapsed;

            var mParams = _cluster.parameters;
            switch (_state) {
            case State.Ejection:
                if (cluster.timeSinceLaunch >= mParams.minActivationTime) {
                    _state = State.Pursuit;
                    _driver = mParams.createPursuit(this);
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("missile pursuit activated at t = " + cluster.timeSinceLaunch);
                    }
                }
                break;
            case State.Pursuit:
                bool forceHit = (mParams.pursuitHitTimeCorrection && _cluster.timeToHit <= 0f);
                if (forceHit) {
                    // fake a velocity large enough to make hit test succeeed
                    velocity = cluster.target.velocity
                        + (_cluster.target.position - _position) / timeElapsed;
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("forcing missile at target (hit time correction)");
                    }
                } 
                Vector3 hitPos;
                bool hitTestSucceeded = cluster.target.hitTest(this, out hitPos);
                if (hitTestSucceeded || forceHit) {
                    _position = hitTestSucceeded ? hitPos : _cluster.target.position;
                    velocity = Vector3.Zero;
                    _state = State.AtTarget;
                    _driver = null;
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("missile at target at t = " + cluster.timeSinceLaunch);
                    }
                }
                break;
            case State.AtTarget:
                if (mParams.terminateWhenAtTarget) {
                    _state = State.Terminated;
                    _driver = null;
                    if (mParams.targetHitHandlers != null) {
                        mParams.targetHitHandlers(_position, mParams);
                    }
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("missile terminated at target at t = " + cluster.timeSinceLaunch);
                    }
                }
                break;
            case State.Intercepted:
                // todo
                break;
            case State.Terminated:
                throw new Exception ("missile state machine still running while in the terminated state");
                break;
            }

            if (_driver != null) {
                _driver.updateExecution(timeElapsed);
            }
        }

    }  

    // compute los rate
    #if false
    Vector3 rtmNew = _computeRtm(cluster.target.position, _position);
    Vector3 losDelta = rtmNew - _rtmOld;
    _losRate = losDelta.LengthFast / timeElapsed;
    _losRateRate = (_losRate - _losRateOld) / timeElapsed;
    _rtmOld = rtmNew;
    _losRateOld = _losRate;

    protected static Vector3 _computeRtm(Vector3 targetPos, Vector3 missilePos)
    {
    return (targetPos - missilePos).Normalized();
    }
    #endif

}

