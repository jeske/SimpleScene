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
        bool isAlive{ get; } // target is structurally intact
        bool hitTest(SSpaceMissileData missile, out Vector3 hitLocation);
        void update(float timeElapsed);
    }

    public class SSpaceMissileObjectTarget : ISSpaceMissileTarget
    {
        protected readonly SSObject _targetObj;
        protected Vector3 _prevPos;
        protected Vector3 _velocity;

        public SSpaceMissileObjectTarget(SSObject targetObj)
        {
            _targetObj = targetObj;
        }

        public virtual Vector3 position { get { return _targetObj.Pos; } }

        public virtual Vector3 velocity { get { return _velocity; } } // TODO

        public virtual bool isAlive { 
            get {
                // this is pretty arbitrary at this point
                return _targetObj.renderState.visible
                    && _targetObj.renderState.toBeDeleted == false;
            }
        }

        public virtual bool hitTest(SSpaceMissileData missile, out Vector3 hitLocation)
        {
            //Vector3 velNorm = (missile.velocity - this.velocity).Normalized();
            Vector3 velNorm = missile.velocity.Normalized();
            SSRay ray = new SSRay(missile.position, velNorm);
            float rayDistance;
            if(_targetObj.Intersect(ref ray, out rayDistance)) {
                float nextTickDistance = missile.velocity.LengthFast * missile.cluster.parameters.simulationStep;
                if (rayDistance - nextTickDistance < missile.cluster.parameters.atTargetDistance) {
                    hitLocation = missile.position + velNorm * rayDistance;
                    return true;
                }
            }
            hitLocation = new Vector3(float.PositiveInfinity);
            return false;
        }

        /// <summary> carefull not to call this more than once per simulation step </summary>
        public virtual void update(float timeElapsed)
        {
            _velocity = (_targetObj.Pos - _prevPos) / timeElapsed;
            _prevPos = _targetObj.Pos;
        }
    }
}

