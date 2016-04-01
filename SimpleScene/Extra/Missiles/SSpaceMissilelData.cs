using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileData
    {
        public delegate void MissileAtTargetFunc (SSpaceMissileData missileData);

        public enum State { Ejection, Pursuit, AtTarget, Intercepted, Terminated };

        #region missile specific
        public Vector3 position = Vector3.Zero;
        public Vector3 velocity = Vector3.Zero;
        public State state = State.Ejection;

        /// <summary> high level state of what the missile is doing </summary>
        /// <summary> currently active missile driver, controls missile velocity and visual orientation </summary>
        protected ISSpaceMissileDriver _driver = null;
        #endregion

        #region may be shared by a missile cluster
        public virtual ISSpaceMissileTarget target { get; }
        public virtual SSpaceMissileParameters parameters { get; }
        public virtual float timeSinceLaunch { get { return _timeSinceLaunchPriv; } }
        public virtual float timeToHit { get; set; }
        public virtual MissileAtTargetFunc atTargetFunc { get; set; }
        #endregion

        private float _timeSinceLaunchPriv = 0f;

        public SSpaceMissileData(SSpaceMissileParameters parameters, ISSpaceMissileTarget target,
            Vector3 missileWorldPos, Vector3 missileWorldVel,
            float timeToHitTarget)
        {
            this.position = missileWorldPos;
            this.velocity = missileWorldVel;
            this.parameters = parameters;
            this.target = target;
        }

        public void terminate()
        {
            state = State.Terminated;
            _driver = null;
            if (parameters.debuggingAid) {
                System.Console.WriteLine("missile terminated on request at t = " + timeSinceLaunch);
            }
        }

        public void updateExecution(float timeElapsed)
        {
            if (state == State.Ejection && _driver == null) {
                _driver = parameters.createEjection(this);
                _driver.updateExecution(0f);
            }

            position += velocity * timeElapsed;

            var mParams = parameters;
            switch (state) {
            case State.Ejection:
                if (this.timeSinceLaunch >= mParams.pursuitActivationTime) {
                    state = State.Pursuit;
                    _driver = mParams.createPursuit(this);
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("missile pursuit activated at t = " + timeSinceLaunch);
                    }
                }
                break;
            case State.Pursuit:
                bool forceHit = (mParams.pursuitHitTimeCorrection && timeToHit <= 0f);
                if (forceHit) {
                    // fake a velocity large enough to make hit test succeeed
                    velocity = target.velocity
                        + (target.position - this.position) / timeElapsed;
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("forcing missile at target (hit time correction)");
                    }
                } 
                Vector3 hitPos;
                bool hitTestSucceeded = target.hitTest(this, out hitPos);
                if (hitTestSucceeded || forceHit) {
                    position = hitTestSucceeded ? hitPos : target.position;
                    velocity = Vector3.Zero;
                    state = State.AtTarget;
                    _driver = null;
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("missile at target at t = " + timeSinceLaunch);
                        if (float.IsNaN(position.X)) {
                            System.Console.WriteLine("bad position");
                        }
                    }
                    if (this.atTargetFunc != null) {
                        this.atTargetFunc(this);
                    }
                }
                break;
            case State.AtTarget:
                if (mParams.terminateWhenAtTarget) {
                    state = State.Terminated;
                    _driver = null;
                    if (mParams.debuggingAid) {
                        System.Console.WriteLine("missile terminated at target at t = " + timeSinceLaunch);
                    }
                }
                break;
            case State.Intercepted:
                // todo
                break;
            case State.Terminated:
                //throw new Exception ("missile state machine still running while in the terminated state");
                break;
            }

            if (_driver != null) {
                _driver.updateExecution(timeElapsed);
            }

            _timeSinceLaunchPriv += timeElapsed;
        }

        #region for debugging only
        public Vector3 _lataxDebug = Vector3.Zero;
        public Vector3 _hitTimeCorrAccDebug = Vector3.Zero;
        #endregion
    }  

    public class SSpaceMissileVisualData : SSpaceMissileData
    {
        #region accessors to the internally managed
        public SSpaceMissileClusterVisualData cluster { get { return _cluster; } }
        public int clusterId { get { return _clusterId; } }
        #endregion

        #region mutable by drivers

        /// <summary> direction where the missile is visually facing </summary>>
        public Vector3 visualDirection = Vector3.UnitZ;

        /// <summary> How much smoke is there? [0-1] </summary>
        public float visualSmokeAmmount = 1f;

        /// <summary> Size of smoke sprites [0-1] </summary>
        public float visualSmokeSize = 1f;
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

        #region self managed
        /// <summary> The cluster this missile belongs to </summary>
        protected readonly SSpaceMissileClusterVisualData _cluster; 

        /// <summary> id within a cluster. Can be referenced by ejection/pursuit behaviors. </summary>
        protected readonly int _clusterId;
        #endregion

        public SSpaceMissileVisualData(SSpaceMissileClusterVisualData cluster, int clusterId,
                                 //Vector3 initClusterPos, Vector3 initClusterVel, 
                                 Vector3 missileWorldPos, Vector3 missileWorldDir, Vector3 missileWorldVel,
                                 float timeToHitTarget)
            : base(cluster.parameters, cluster.target, missileWorldPos, missileWorldVel, timeToHitTarget)
        {
            _cluster = cluster;
            _clusterId = clusterId;
            visualDirection = missileWorldDir;

        }

        /// <summary> position in world space where the jet starts </summary>
        public Vector3 jetPosition()
        {
            var mParams = cluster.parameters;
            return this.position - this.visualDirection * mParams.missileBodyScale * mParams.jetPosition;
        }

        #region shared by all missiles in a cluster
        public override ISSpaceMissileTarget target { get { return _cluster.target; } }
        public override SSpaceMissileParameters parameters { get { return _cluster.parameters; } }
        public override float timeSinceLaunch { get { return _cluster.timeSinceLaunch; } }
        public override float timeToHit { get { return _cluster.timeToHit; } }
        public override MissileAtTargetFunc atTargetFunc { get { return _cluster.atTargetFunc; } }
        #endregion


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

}

