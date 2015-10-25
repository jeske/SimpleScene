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
        public readonly SSObject targetObj;

        protected Vector3 _prevPos;
        protected Vector3 _velocity;
        protected Vector3 _acc;

        public SSpaceMissileObjectTarget(SSObject targetObj)
        {
            this.targetObj = targetObj;
            _prevPos = targetObj.Pos;
            _velocity = Vector3.Zero;
            _acc = Vector3.Zero;
        }

        public virtual Vector3 position { get { return targetObj.Pos; } }

        public virtual Vector3 direction { get { return targetObj.Dir; } }

        public virtual Vector3 velocity { get { return _velocity; } }

        public virtual Vector3 acceleration { get { return _acc; } }

        public virtual bool isAlive { 
            get {
                // this isn't currently used and is pretty arbitrary at this point
                return targetObj.renderState.visible
                    && targetObj.renderState.toBeDeleted == false;
            }
        }

        public virtual bool hitTest(SSpaceMissileData missile, out Vector3 hitLocation)
        {
            //hitLocation = missile.cluster.target.position;
            //return false;

            var mParams = missile.cluster.parameters;
            float simStep = missile.cluster.parameters.simulationStep;
            float nextTickDist = missile.velocity.LengthFast * simStep;
            float testDistSq = (nextTickDist + targetObj.worldBoundingSphereRadius);
            testDistSq *= testDistSq;
            float distSq = (targetObj.Pos - missile.position).LengthSquared;

            if (testDistSq > distSq) {

                Vector3 velNorm = (missile.velocity - this.velocity);
                velNorm.NormalizeFast();
                SSRay ray = new SSRay(missile.position, velNorm);
                float rayDistance = 0f;
                if(targetObj.PreciseIntersect(ref ray, ref rayDistance)) {
                    if (rayDistance - nextTickDist < mParams.atTargetDistance) {
                        hitLocation = missile.position + this.velocity * simStep
                            + velNorm * rayDistance;
                        return true;
                    }
                }
            }
            hitLocation = new Vector3(float.PositiveInfinity);
            return false;
        }

        /// <summary> carefull not to call this more than once per simulation step </summary>
        public virtual void update(float timeElapsed)
        {
            if (timeElapsed == 0f) return;

            var newVel = (targetObj.Pos - _prevPos) / timeElapsed;
            //_acc = (newVel - _velocity) / timeElapsed;
            _acc = Vector3.Zero;

            _velocity = newVel;
            _prevPos = targetObj.Pos;
            //_velocity = Vector3.Zero;
        }
    }
}

