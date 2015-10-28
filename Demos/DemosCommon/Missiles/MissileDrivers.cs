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

        public SSimpleMissileEjectionDriver(SSpaceMissileData missile, Vector3 clusterInitPos, Vector3 clusterInitVel)
        {
            _missile = missile;
            var mParams = _missile.cluster.parameters;

            _missile.visualDirection = (_missile.position - clusterInitPos);
            if (_missile.visualDirection.LengthSquared < 0.0001f) {
                // means missile was spawned right at the launcher. pick a direction towards target
                _missile.visualDirection = (_missile.cluster.target.position - _missile.position).Normalized();
            } else {
                // spawned away from launcher. ok to orient away from launcher
                _missile.visualDirection.Normalize();
            }
            _missile.velocity = clusterInitVel + _missile.visualDirection * mParams.ejectionVelocity;

            var rand = SSpaceMissilesSimulation.rand;
            _yawVelocity = (float)rand.NextDouble() * mParams.ejectionMaxRotationVel;
            _pitchVelocity = (float)rand.NextDouble() * mParams.ejectionMaxRotationVel;

            _missile.visualSmokeSize = 1f;
            _missile.visualSmokeAmmount = 1f;
        }

        public void updateExecution(float timeElapsed) 
        { 
            float t = _missile.cluster.timeSinceLaunch;
            float dy = _yawVelocity * t;
            float dp = _pitchVelocity * t;

            Quaternion q = Quaternion.FromAxisAngle(_missile.up, dy)
                           * Quaternion.FromAxisAngle(_missile.pitchAxis, dp);
            _missile.visualDirection = Vector3.Transform(_missile.visualDirection, q);

            var mParams = _missile.cluster.parameters;
            _missile.velocity += _missile.visualDirection * mParams.ejectionAcc;
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

            _missile.visualSmokeSize = 0.1f;
            _missile.visualSmokeAmmount = 1f;

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

            if (mParams.pursuitAugmentedPN == true) {
                // this code is not tested as there are currently no targets with well defined accelerations
                Vector3 losDir = R.Normalized();
                float targetAccLos = Vector3.Dot(target.acceleration, losDir);
                Vector3 targetLatAx = target.acceleration - targetAccLos * losDir;
                latax += mParams.pursuitNavigationGain * targetLatAx / 2f;
            }

            _missile._lataxDebug = latax;

            // apply latax
            var oldVelMag = _missile.velocity.LengthFast;
            _missile.velocity += latax * timeElapsed;
            float tempVelMag = _missile.velocity.LengthFast;
            if (oldVelMag != 0f) {
                float r = tempVelMag / oldVelMag;
                if (r > 1f) {
                    _missile.velocity /= r;
                }
            }

            if (mParams.pursuitHitTimeCorrection) {
                // apply pursuit hit time correction
                float dist = R.LengthFast;
                if (dist != 0f) {
                    Vector3 targetDir = R / dist;
                    float v0 = -Vector3.Dot(Vr, targetDir);
                    float t = _missile.cluster.timeToHit;
                    float correctionAccMag = 2f * (dist - v0 * t) / t / t;
                    Vector3 corrAcc = correctionAccMag * targetDir;
                    _missile.velocity += corrAcc * timeElapsed;
                    _missile._hitTimeCorrAccDebug = corrAcc;
                }
            } else {
                // hit time correction inactive. allow accelerating to achieve optimal velocity or forever
                oldVelMag = _missile.velocity.LengthFast;
                float velDelta = mParams.pursuitMaxAcc * timeElapsed;
                float newVelMag = Math.Min(oldVelMag + velDelta, mParams.pursuitMaxVelocity);
                if (oldVelMag != 0f) {
                    _missile.velocity *= (newVelMag / oldVelMag);
                }
            }

            // make visual direction "lean into" velocity
            Vector3 axis;
            float angle;
            OpenTKHelper.neededRotation(_missile.visualDirection, _missile.velocity.Normalized(),
                out axis, out angle);
            float abs = Math.Abs(angle);
            if (abs > mParams.pursuitVisualRotationRate && abs > 0f) {
                angle = angle / abs * mParams.pursuitVisualRotationRate;
            }
            Quaternion quat = Quaternion.FromAxisAngle(axis, angle);

            _missile.visualDirection = Vector3.Transform(_missile.visualDirection, quat);
            _missile.visualSmokeSize = 1f;
            _missile.visualSmokeAmmount = 1f;
        }
    }
}

