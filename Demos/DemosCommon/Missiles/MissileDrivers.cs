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
    /// http://en.wikipedia.org/wiki/Proportional_navigation
    /// 
    /// take this one with a grain of salt:
    /// http://www.moddb.com/members/blahdy/blogs/gamedev-introduction-to-proportional-navigation-part-i
    /// </summary>
    public class SProportionalNavigationPursuitDriver : ISSpaceMissileDriver
    {
        protected SSpaceMissileData _missile;

        public SProportionalNavigationPursuitDriver(SSpaceMissileData missile)
        {
            _missile = missile;
        }

        public void updateExecution(float timeElapsed)
        {
            // TODO adjust things (thrust?) so that distanceToTarget = closing velocity * timeToHit

            var mParams = _missile.cluster.parameters;

            var target = _missile.cluster.target;
            Vector3 Vr = target.velocity - _missile.velocity;
            Vector3 R = target.position - _missile.position;
            Vector3 omega = Vector3.Cross(R, Vr) / R.LengthSquared;
            Vector3 latax = mParams.pursuitNavigationGain * Vector3.Cross(Vr, omega);
            _missile._lataxDebug = latax;

            // apply latax
            var oldVelMag = _missile.velocity.LengthFast;
            _missile.velocity += latax * timeElapsed;
            float tempVelMag = _missile.velocity.LengthFast;
            float r = tempVelMag / oldVelMag;
            if (r > 1f) {
                _missile.velocity /= r;
            }

            // apply pursuit hit time correction
            if (mParams.pursuitHitTimeCorrection)
            {
                float timeToHit = _missile.cluster.timeToHit;
                float estimatedTimeToHit;
                float dot = Vector3.Dot(R.Normalized(), _missile.velocity.Normalized());
                //float acos = ));
                float correctionAcc;
                //if (Math.Abs(acos) < 0.5f) {
                //if (true) {
                if (dot > 0f) {
                    // flying towards target in almost a straight line...
                    float dist = R.LengthFast;
                    float v0 = Vr.LengthFast;
                    estimatedTimeToHit = dist/v0 ;
                    correctionAcc = 2 * (dist - v0 * timeToHit) / timeToHit / timeToHit;
                    correctionAcc = Math.Abs(correctionAcc);
                    float tDiscrepancy = estimatedTimeToHit - timeToHit;
                    //float esimatedRtmDiscrepancy = (target.position - _missile.position).LengthFast - 
                    if (tDiscrepancy > mParams.hitTimeTolerance) {
                        _missile.velocity *= (1f + Math.Min(correctionAcc, mParams.maxPursuitHitTimeCorrectionThrust) * timeElapsed / oldVelMag);
                    } else if (tDiscrepancy < -mParams.hitTimeTolerance) {
                        _missile.velocity *= (1f - Math.Min(correctionAcc, mParams.maxPursuitHitTimeCorrectionBrake) * timeElapsed / oldVelMag);
                    }
                } else if (_missile.losRateRate < 0f) {
                    estimatedTimeToHit = _missile.losRate / -_missile.losRateRate;
                    correctionAcc = mParams.maxPursuitHitTimeCorrectionThrust;
                } else {
                    estimatedTimeToHit = timeToHit;
                    correctionAcc = 0f;
                }

            }

            // make visual direction "lean into" velocity
            Vector3 axis;
            float angle;
            OpenTKHelper.neededRotation(_missile.direction, _missile.velocity.Normalized(),
                out axis, out angle);
            float abs = Math.Abs(angle);
            if (abs > mParams.maxPursuitRotationRate) {
                angle = angle / abs * mParams.maxPursuitRotationRate;
            }
            Quaternion quat = Quaternion.FromAxisAngle(axis, angle);

            _missile.direction = Vector3.Transform(_missile.direction, quat);

            //_missile.direction = _missile.velocity.Normalized();
        }

        public float estimateTimeNeededToHit(SSpaceMissileData missile)
        {
            // TODO
            return 100f;
        }
    }
}

