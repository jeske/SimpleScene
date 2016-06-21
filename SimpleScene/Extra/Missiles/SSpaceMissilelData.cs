using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileData
    {
        public delegate void AtTargetFunc (SSpaceMissileData missileData);

        #region missile specific
        /// <summary> high level state of what the missile is doing </summary>
        public enum State : byte { Uninitialized, Ejection, Pursuit, AtTarget, Intercepted, FadingOut, Terminated };

        public virtual Vector3 displayPosition { get { return position; } }

        public Vector3 position = Vector3.Zero;
        public Vector3 velocity = Vector3.Zero;

        public State state {
            get { return _state; }
            set {
                // it may be desirable to assign state externally, f.e. when synchronizing over a network.
                // the state has to be closely synced with the missile driver
                if (state != value) {
                    _state = value;
                    switch (state) {
                    case State.Ejection:
                        if (_driver == null || !(_driver is SMissileEjectionDriver)) { // is this slow?
                            _driver = parameters.createEjection(this);
                        }
                        break;
                    case State.Pursuit:
                        if (_driver == null || !(_driver is SProportionalNavigationPursuitDriver)) { // is this slow?
                            _driver = parameters.createPursuit(this);
                        }
                        break;
                    case State.FadingOut:
                        _driver = null;
                        _sharableData.fadeTime = _sharableData.timeSinceLaunch;
                        break;
                    case State.Terminated:
                        _driver = null;
                        break;
                    default:
                        if (_driver != null) {
                            // we could just write null unconditionally. but doing it this way may help caching?
                            _driver = null;
                        }
                        break;
                    }
                }
            }
        }

        protected State _state = State.Uninitialized;

        public ISSpaceMissileDriver driver { get { return _driver; } }

        /// <summary> currently active missile driver, controls missile velocity and visual orientation </summary>
        protected ISSpaceMissileDriver _driver = null;
        #endregion

        #region may be shared by a missile cluster
        public ISSpaceMissileTarget target { get { return _sharableData.target; } }
        public SSpaceMissileParameters parameters { get { return _sharableData.parameters; } }
        public float timeSinceLaunch { 
            get { return _sharableData.timeSinceLaunch; } 
            set { _sharableData.timeSinceLaunch = value; }
        }
        public float timeToHit { 
            get { return _sharableData.timeToHit; } 
            set { _sharableData.timeToHit = value; }
        }
        public float fadeTime {
            get { return _sharableData.fadeTime; }
            set { _sharableData.fadeTime = value; }
        }
        public AtTargetFunc atTargetFunc { get { return _sharableData.atTargetFunc; } }

        protected ISharableData _sharableData;
        #endregion

        public SSpaceMissileData(
            Vector3 missileWorldPos, Vector3 missileWorldVel,
            SSpaceMissileParameters parameters = null, ISSpaceMissileTarget target = null,
            float timeToHitTarget = 0f, ISharableData sharableData = null, AtTargetFunc atf = null)
        {
            _sharableData = sharableData ?? new SingleInstanceData () {
                target = target,
                parameters = parameters,
                timeSinceLaunch = 0f,
                timeToHit = timeToHitTarget,
                atTargetFunc = atf,
            };

            this.position = missileWorldPos;
            this.velocity = missileWorldVel;

            state = State.Ejection;
            updateExecution(0f);
        }

        public void terminate()
        {
            state = State.Terminated;
            if (parameters.debuggingAid) {
                Console.WriteLine("missile terminated on request at t = " + timeSinceLaunch);
            }
        }

        public void fadeOut()
        {
            this.state = State.FadingOut;
            if (parameters.debuggingAid) {
                Console.WriteLine("missile begins fading at t = " + timeSinceLaunch);
            }
        }

        public virtual void updateExecution(float timeElapsed)
        {
            position += velocity * timeElapsed;

            var mParams = parameters;
            switch (_state) {
            case State.Ejection:
                if (this.timeSinceLaunch >= mParams.pursuitActivationTime) {
                    state = State.Pursuit;
                    if (mParams.debuggingAid) {
                        Console.WriteLine("missile pursuit activated at t = " + timeSinceLaunch);
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
                        Console.WriteLine("forcing missile at target (hit time correction)");
                    }
                } 
                Vector3 hitPos;
                bool hitTestSucceeded = target.hitTest(this, out hitPos);
                if (hitTestSucceeded || forceHit) {
                    position = hitTestSucceeded ? hitPos : target.position;
                    velocity = Vector3.Zero;
                    state = State.AtTarget;
                    if (mParams.debuggingAid) {
                        Console.WriteLine("missile at target at t = " + timeSinceLaunch);
                        if (float.IsNaN(position.X)) {
                            Console.WriteLine("bad position");
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
                    if (mParams.debuggingAid) {
                        Console.WriteLine("missile terminated at target at t = " + timeSinceLaunch);
                    }
                }
                break;
            case State.FadingOut:
                if ((_sharableData.timeSinceLaunch - _sharableData.fadeTime) >= mParams.fadeDuration) {
                    state = State.Terminated;
                    if (mParams.debuggingAid) {
                        Console.WriteLine("missile terminated after fading at t = " + timeSinceLaunch);
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

            _sharableData.update(timeElapsed);
        }

        #region for debugging only
        public Vector3 _lataxDebug = Vector3.Zero;
        public Vector3 _hitTimeCorrAccDebug = Vector3.Zero;
        #endregion

        public interface ISharableData
        {
            ISSpaceMissileTarget target { get; }
            SSpaceMissileParameters parameters { get; }
            float timeSinceLaunch { get; set; }
            float timeToHit { get; set; }
            float fadeTime { get; set; }
            SSpaceMissileData.AtTargetFunc atTargetFunc { get; }
            void update(float elapsed);
        }

        protected class SingleInstanceData : ISharableData
        {
            public ISSpaceMissileTarget target { get; set; }
            public SSpaceMissileParameters parameters { get; set; }
            public float timeSinceLaunch { get; set; }
            public float timeToHit { get; set; }
            public float fadeTime { get; set; }
            public SSpaceMissileData.AtTargetFunc atTargetFunc { get; set; }
            public void update(float elapsed) { 
                timeSinceLaunch += elapsed; 
                timeToHit -= elapsed;
            }
        }
    }  

    public class SSpaceMissileVisualData : SSpaceMissileData
    {
        #region accessors to the internally managed
        public SSpaceMissileClusterVisualData cluster { 
            get { return (_sharableData as ClusterData).cluster; } }
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

        public float distToTarget {
            get {
                return (target.position - this.position).LengthFast;
            }
        }
        #endregion

        /// <summary> id within a cluster. Can be referenced by ejection/pursuit behaviors. </summary>
        protected readonly int _clusterId;

        public SSpaceMissileVisualData (
            Vector3 missileWorldPos, Vector3 missileWorldDir, Vector3 missileWorldVel,
            SSpaceMissileClusterVisualData cluster, int clusterId)
            : base(missileWorldPos, missileWorldVel, sharableData: new ClusterData (cluster))
        {
            _sharableData = new ClusterData (cluster);
            _clusterId = clusterId;
            visualDirection = missileWorldDir;
        }

        public SSpaceMissileVisualData (
            Vector3 missileWorldPos, Vector3 missileWorldVel,
            SSpaceMissileParameters parameters = null, ISSpaceMissileTarget target = null, AtTargetFunc atf = null,
            float ejectionYawVelocity = float.NaN, float ejectionPitchVelocity = float.NaN)
            : base(missileWorldPos, missileWorldVel, parameters, target)
        {
            var ejectionAsVisual = _driver as SMissileEjectionVisualDriver;
            if (!float.IsNaN(ejectionYawVelocity)) {
                ejectionAsVisual.yawVelocity = ejectionYawVelocity;
            }
            if (!float.IsNaN(ejectionPitchVelocity)) {
                ejectionAsVisual.pitchVelocity = ejectionPitchVelocity;
            }
        }


        /// <summary> position in world space where the jet starts </summary>
        public Vector3 jetPosition()
        {
            var mParams = parameters as SSpaceMissileVisualParameters;
            return this.displayPosition - this.visualDirection * mParams.missileBodyScale * mParams.jetPosition;
        }

        #region shared by all missiles in a cluster
        protected class ClusterData : ISharableData
        {
            public readonly SSpaceMissileClusterVisualData cluster;

            public ClusterData(SSpaceMissileClusterVisualData cluster) { this.cluster = cluster; }

            public ISSpaceMissileTarget target { get { return cluster.target; } }
            public SSpaceMissileParameters parameters { get { return cluster.parameters; } }
            public float timeSinceLaunch { 
                get { return cluster.timeSinceLaunch; } 
                set { cluster.timeSinceLaunch = value; }
            }
            public float timeToHit { 
                get { return cluster.timeToHit; } 
                set { cluster.timeToHit = value; }
            }
            public float fadeTime {
                get { return cluster.fadeTime; }
                set { cluster.fadeTime = value; }
            }
            public AtTargetFunc atTargetFunc { get { return cluster.atTargetFunc; } }
            public void update(float elapsed) 
            { 
                // do nothing; cluster updates already take care of time variables
            }
        }

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

