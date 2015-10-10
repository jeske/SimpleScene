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

        bool hitTest(SSpaceMissileData missile);
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

        public bool hitTest(SSpaceMissileData missile)
        {
            SSRay ray = new SSRay(missile.position, missile.direction);
            float distance;
            return _target.Intersect(ref ray, out distance) 
                && distance < missile.cluster.parameters.atTargetDistance;
        }
    }
}

