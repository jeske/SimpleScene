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
    }

    public class SSpaceMissileObjectTarget : ISSpaceMissileTarget
    {
        protected readonly SSObject _target;

        public SSpaceMissileObjectTarget(SSObject target)
        {
            _target = target;
        }

        public Vector3 position { get { return _target.Pos; } }
        public Vector3 velocity { get { return Vector3.Zero; } } // TODO
        public bool isAlive { get { return true; } }

        public bool hitTest(SSpaceMissileData missile, out Vector3 hitLocation)
        {
            Vector3 velNorm = missile.velocity.Normalized();
            SSRay ray = new SSRay(missile.position, velNorm);
            float rayDistance;
            if(_target.Intersect(ref ray, out rayDistance)) {
                float nextTickDistance = missile.velocity.LengthFast * missile.cluster.parameters.simulationStep;
                if (rayDistance - nextTickDistance < missile.cluster.parameters.atTargetDistance) {
                    hitLocation = missile.position + velNorm * rayDistance;
                    return true;
                }
            }
            hitLocation = new Vector3(float.PositiveInfinity);
            return false;
        }
    }
}

