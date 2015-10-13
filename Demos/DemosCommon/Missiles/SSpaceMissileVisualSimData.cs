using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileData
    {
        // TODO actual rendering constructs. Where do they go?

        public enum State { Ejection, Pursuit, AtTarget, Intercepted, Terminated };

        #region accessors to internally managed
        public SSpaceMissileClusterData cluster { get { return _cluster; } }
        public int clusterId { get { return _clusterId; } }
        public State state { get { return _state; } }
        public Vector3 position { get { return _position; } }
        #endregion

        #region mutable by drivers
        public Vector3 velocity = Vector3.Zero;
        public Vector3 direction = Vector3.UnitZ;
        #endregion

        #region for debugging only
        public Vector3 _lataxDebug = Vector3.Zero;
        #endregion

        #region helpers
        public Vector3 up { 
            get { 
                Quaternion quat = OpenTKHelper.neededRotation(Vector3.UnitZ, direction);
                return Vector3.Transform(Vector3.UnitY, quat);
            } 
        }
        public Vector3 pitchAxis { 
            get { 
                Quaternion quat = OpenTKHelper.neededRotation(Vector3.UnitZ, direction);
                return Vector3.Transform(Vector3.UnitX, quat);
            } 
        }
        public float jetStrength { get { return velocity.LengthFast; } }
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
                    System.Console.WriteLine("pursuit activated at t = " + cluster.timeSinceLaunch);
                    _state = State.Pursuit;
                    _driver = mParams.createPursuit(this);
                }
                break;
            case State.Pursuit:
                Vector3 latax;
                if (cluster.target.hitTest(this)) {
                    System.Console.WriteLine("missile at target at t = " + cluster.timeSinceLaunch);
                    _state = State.AtTarget;
                    _driver = null;
                }
                break;
            case State.AtTarget:
                if (mParams.terminateWhenAtTarget) {
                    System.Console.WriteLine("missile terminated at target at t = " + cluster.timeSinceLaunch);
                    _state = State.Terminated;
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
}

