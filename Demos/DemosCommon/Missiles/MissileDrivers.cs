using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public interface ISSpaceMissileDriver
    {
        void updateExecution(float timeElapsed);
    }

    public class SSimpleMissileEjectionDriver : ISSpaceMissileDriver
    {
        protected readonly SSpaceMissileData _missile;

        protected readonly float _yawVelocity; // purely visual
        protected readonly float _pitchVelocity; // purely visual
        protected readonly Vector3 _initDir;

        public SSimpleMissileEjectionDriver(SSpaceMissileData missile, 
            Vector3 clusterInitPos, Vector3 clusterInitVel)
        {
            _missile = missile;

            var cluster = _missile.cluster;
            var mParams = cluster.parameters;

            missile.direction = (missile.position - clusterInitPos).Normalized();
            missile.velocity = missile.direction * mParams.ejectionVelocity;

            var rand = SSpaceMissilesVisualSimulation.rand;
            _yawVelocity = (float)rand.NextDouble() * mParams.ejectionMaxRotationVel;
            _pitchVelocity = (float)rand.NextDouble() * mParams.ejectionMaxRotationVel;
        }

        public void updateExecution(float timeElapsed) 
        { 
            float t = _missile.cluster.timeSinceLaunch;
            float dy = _yawVelocity * t;
            float dp = _pitchVelocity * t;

            Quaternion q = Quaternion.FromAxisAngle(_missile.up, dy)
                           * Quaternion.FromAxisAngle(_missile.pitchAxis, dp);
            _missile.direction = Vector3.Transform(_missile.direction, q);

            var mParams = _missile.cluster.parameters;
            _missile.velocity += _missile.direction * mParams.ejectionAcc;
        }
    }

    /// <summary>
    /// http://www.moddb.com/members/blahdy/blogs/gamedev-introduction-to-proportional-navigation-part-i
    /// </summary>
    public class SProportionalNavigationPursuitDriver : ISSpaceMissileDriver
    {
        protected SSpaceMissileData _missile;
        public Vector3 _rtmOld;

        public SProportionalNavigationPursuitDriver(SSpaceMissileData missile)
        {
            _missile = missile;
            _rtmOld = _computeRtm(missile.cluster.target.position, missile.position);
        }

        public void updateExecution(float timeElapsed)
        {
            // TODO adjust things (thrust?) so that distanceToTarget = closing velocity * timeToHit

            var mParams = _missile.cluster.parameters;

            // compute latax
            Vector3 rtmNew = _computeRtm(_missile.cluster.target.position, _missile.position);
            Vector3 losDelta = rtmNew - _rtmOld;
            //float losRate = losDelta.LengthFast / timeElapsed;
            float losRate = losDelta.LengthFast;
            Vector3 latax = mParams.navigationGain * losRate * losRate * rtmNew; // A = rtmNew * N * Vc * losRate

            // apply latax
            var oldMagnitude = _missile.velocity.LengthFast;
            latax.Normalize();
            latax *= (oldMagnitude * timeElapsed);
            _missile.velocity += latax;
            float r = _missile.velocity.Length / oldMagnitude;
            if (r > 1f) {
                _missile.velocity /= r;
            }

            // make visual direction "lean into" velocity
            Vector3 axis;
            float angle;
            OpenTKHelper.neededRotation(_missile.direction, _missile.velocity.Normalized(),
                out axis, out angle);
            float abs = Math.Abs(angle);
            if (abs > mParams.maxVisualRotation) {
                angle = angle / abs * mParams.maxVisualRotation;
            }
            Quaternion quat = Quaternion.FromAxisAngle(axis, angle);

            _missile.direction = Vector3.Transform(_missile.direction, quat);

            //_missile.direction = _missile.velocity.Normalized();

            // housekeeping
            _rtmOld = rtmNew;
        }

        public float estimateTimeNeededToHit(SSpaceMissileData missile)
        {
            // TODO
            return 100f;
        }

        protected static Vector3 _computeRtm(Vector3 targetPos, Vector3 missilePos)
        {
            return (targetPos - missilePos).Normalized();
        }
    }
}

