using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SSpaceMissileData
    {
        public delegate void AtTargetFunc (SSpaceMissileData missileData);

        #region missile specific
        /// <summary> high level state of what the missile is doing </summary>
        public enum State { Ejection, Pursuit, AtTarget, Intercepted, Terminated };

        public Vector3 position = Vector3.Zero;
        public Vector3 velocity = Vector3.Zero;
        public State state = State.Ejection;

        /// <summary> currently active missile driver, controls missile velocity and visual orientation </summary>
        protected ISSpaceMissileDriver _driver = null;
        #endregion

        #region may be shared by a missile cluster
        public ISSpaceMissileTarget target { get { return _sharableData.target; } }
        public SSpaceMissileParameters parameters { get { return _sharableData.parameters; } }
        public float timeSinceLaunch { get { return _sharableData.timeSinceLaunch; } }
        public float timeToHit { get { return _sharableData.timeToHit; } }
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
            float timeSinceLaunch { get; }
            float timeToHit { get; }
            SSpaceMissileData.AtTargetFunc atTargetFunc { get; }
            void update(float elapsed);
        }

        protected class SingleInstanceData : ISharableData
        {
            public ISSpaceMissileTarget target { get; set; }
            public SSpaceMissileParameters parameters { get; set; }
            public float timeSinceLaunch { get; set; }
            public float timeToHit { get; set; }
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

        /// <summary> position in world space where the jet starts </summary>
        public Vector3 jetPosition()
        {
            var mParams = parameters as SSpaceMissileVisualParameters;
            return this.position - this.visualDirection * mParams.missileBodyScale * mParams.jetPosition;
        }

        #region shared by all missiles in a cluster
        protected class ClusterData : ISharableData
        {
            public readonly SSpaceMissileClusterVisualData cluster;

            public ClusterData(SSpaceMissileClusterVisualData cluster) { this.cluster = cluster; }

            public ISSpaceMissileTarget target { get { return cluster.target; } }
            public SSpaceMissileParameters parameters { get { return cluster.parameters; } }
            public float timeSinceLaunch { get { return cluster.timeSinceLaunch; } }
            public float timeToHit { get { return cluster.timeToHit; } }
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

