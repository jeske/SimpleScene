using System;
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
        Vector3 acceleration { get; }
        Vector3 direction { get; }
        bool isAlive{ get; } // target is structurally intact
        bool hitTest(SSpaceMissileData missile, out Vector3 hitLocation);
        void update(float timeElapsed);
    }

    public class SSpaceMissileObjectTarget : ISSpaceMissileTarget
    {
        protected readonly SSObject _targetObj;
        protected Vector3 _prevPos;
        protected Vector3 _velocity;
        protected Vector3 _acc;

        public SSpaceMissileObjectTarget(SSObject targetObj)
        {
            _targetObj = targetObj;
            _prevPos = targetObj.Pos;
            _velocity = Vector3.Zero;
            _acc = Vector3.Zero;
        }

        public virtual Vector3 position { get { return _targetObj.Pos; } }

        public virtual Vector3 direction { get { return _targetObj.Dir; } }

        public virtual Vector3 velocity { get { return _velocity; } }

        public virtual Vector3 acceleration { get { return _acc; } }

        public virtual bool isAlive { 
            get {
                // this isn't currently used and is pretty arbitrary at this point
                return _targetObj.renderState.visible
                    && _targetObj.renderState.toBeDeleted == false;
            }
        }

        public virtual bool hitTest(SSpaceMissileData missile, out Vector3 hitLocation)
        {
            //Vector3 velNorm = (missile.velocity - this.velocity).Normalized();
            var mParams = missile.cluster.parameters;
            Vector3 missileVelNorm = missile.velocity.Normalized();
            SSRay ray = new SSRay(missile.position, missileVelNorm);
            float rayDistance;
            float simStep = missile.cluster.parameters.simulationStep;
            if(_targetObj.Intersect(ref ray, out rayDistance)) {
                float nextTickDistance = missile.velocity.LengthFast * simStep;
                if (rayDistance - nextTickDistance < mParams.atTargetDistance) {
                    hitLocation = missile.position + missileVelNorm * rayDistance + this.velocity * simStep;
                    return true;
                }
            } else if ((_targetObj.Pos - missile.position).LengthFast < mParams.atTargetDistance) {
                // failsafe for smaller bounding spheres etc.
                hitLocation = _targetObj.Pos;
                return true;
            }
            hitLocation = new Vector3(float.PositiveInfinity);
            return false;
        }

        /// <summary> carefull not to call this more than once per simulation step </summary>
        public virtual void update(float timeElapsed)
        {
            if (timeElapsed == 0f) return;

            var newVel = (_targetObj.Pos - _prevPos) / timeElapsed;
            _acc = (newVel - _velocity) / timeElapsed;

            _velocity = newVel;
            _prevPos = _targetObj.Pos;
        }
    }
}

