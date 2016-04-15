using System;
using OpenTK;

namespace SimpleScene.Demos
{
    public interface ISSpaceMissileDriver
    {
        void updateExecution(float timeElapsed);
    }

    public class SMissileEjectionDriver : ISSpaceMissileDriver
    {
        public virtual SSpaceMissileData missile { get; } 

        public SMissileEjectionDriver(SSpaceMissileData missile)
        {
            this.missile = missile;
        }

        public virtual void updateExecution(float timeElapsed)
        {
            // for a non-visual missile model, we can just idle for a bit
        }
    }

    public class SMissileEjectionVisualDriver : SMissileEjectionDriver
    {
        public float yawVelocity; // purely visual
        public float pitchVelocity; // purely visual

        protected new SSpaceMissileVisualData missile { 
            get { return base.missile as SSpaceMissileVisualData; }
        }
            
        public SMissileEjectionVisualDriver(SSpaceMissileVisualData missile)
            : base(missile)
        {
            var mParams = missile.parameters as SSpaceMissileVisualParameters;
            var rand = SSpaceMissilesVisualSimulation.rand;
            yawVelocity = (float)rand.NextDouble() * mParams.ejectionMaxRotationVel;
            pitchVelocity = (float)rand.NextDouble() * mParams.ejectionMaxRotationVel;
            if (missile.velocity.Length > 0f) {
                missile.visualDirection = missile.velocity.Normalized();
            }
        }

        public override void updateExecution(float timeElapsed) 
        { 
            base.updateExecution(timeElapsed);

            float t = missile.timeSinceLaunch;
            float dy = yawVelocity * t;
            float dp = pitchVelocity * t;

            Quaternion q = Quaternion.FromAxisAngle(missile.up, dy)
                         * Quaternion.FromAxisAngle(missile.pitchAxis, dp);
            missile.visualDirection = Vector3.Transform(missile.visualDirection, q);

            var mParams = missile.parameters;
            missile.velocity += missile.visualDirection * mParams.ejectionAcc;
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
        protected virtual SSpaceMissileData missile { get; }

        public SProportionalNavigationPursuitDriver(SSpaceMissileData missile)
        {
            this.missile = missile;
        }

        public virtual void updateExecution(float timeElapsed) 
        {
            if (timeElapsed <= 0f) return;

            // basic proportional navigation. see wikipedia
            var mParams = missile.parameters;
            var target = missile.target;

            Vector3 Vr = target.velocity - missile.velocity;
            Vector3 R = target.position - missile.position;
            Vector3 omega = Vector3.Cross(R, Vr) / R.LengthSquared;
            Vector3 cross = Vector3.Cross(Vr, omega);
            Vector3 latax;
            if (cross.LengthSquared == 0) {
                latax = R.Normalized() * mParams.pursuitMaxAcc;
            } else {
                latax = mParams.pursuitNavigationGain * cross;
            }

            if (mParams.pursuitAugmentedPN == true) {
                // this code is not tested as there are currently no targets with well defined accelerations
                Vector3 losDir = R.Normalized();
                float targetAccLos = Vector3.Dot(target.acceleration, losDir);
                Vector3 targetLatAx = target.acceleration - targetAccLos * losDir;
                latax += mParams.pursuitNavigationGain * targetLatAx / 2f;
            }

            missile._lataxDebug = latax;

            // apply latax
            var oldVelMag = missile.velocity.LengthFast;
            missile.velocity += latax * timeElapsed;
            float tempVelMag = missile.velocity.LengthFast;
            if (oldVelMag != 0f) {
                float r = tempVelMag / oldVelMag;
                if (r > 1f) {
                    missile.velocity /= r;
                }
            }

            if (mParams.pursuitHitTimeCorrection 
             && !float.IsNaN(missile.timeToHit)) {
                // apply pursuit hit time correction
                float dist = R.LengthFast;
                if (dist != 0f) {
                    Vector3 targetDir = R / dist;
                    float v0 = -Vector3.Dot(Vr, targetDir);
                    float t = missile.timeToHit;
                    float correctionAccMag = 2f * (dist - v0 * t) / t / t;
                    Vector3 corrAcc = correctionAccMag * targetDir;
                    missile.velocity += corrAcc * timeElapsed;
                    missile._hitTimeCorrAccDebug = corrAcc;
                }
            } else {
                // hit time correction inactive. allow accelerating to achieve optimal velocity or forever
                oldVelMag = missile.velocity.LengthFast;
                float velDelta = mParams.pursuitMaxAcc * timeElapsed;
                float newVelMag = Math.Min(oldVelMag + velDelta, mParams.pursuitMaxVelocity);
                if (oldVelMag != 0f) {
                    missile.velocity *= (newVelMag / oldVelMag);
                }
            }

        }
    }

    public class SProportionalNavigationPursuitVisualDriver : SProportionalNavigationPursuitDriver
    {
        public new SSpaceMissileVisualData missile {
            get { return base.missile as SSpaceMissileVisualData; }
        }

        public SProportionalNavigationPursuitVisualDriver(SSpaceMissileVisualData missile)
            : base(missile)
        {
        }

        public override void updateExecution(float timeElapsed)
        {
            base.updateExecution(timeElapsed);

            if (timeElapsed <= 0f) return;

            var mParams = missile.parameters as SSpaceMissileVisualParameters;
            var target = missile.target;

            // make visual direction "lean into" velocity
            Vector3 axis;
            float angle;
            OpenTKHelper.neededRotation(missile.visualDirection, missile.velocity.Normalized(),
                out axis, out angle);
            float abs = Math.Abs(angle);
            if (abs > mParams.pursuitVisualRotationRate && abs > 0f) {
                angle = angle / abs * mParams.pursuitVisualRotationRate;
            }
            Quaternion quat = Quaternion.FromAxisAngle(axis, angle);

            missile.visualDirection = Vector3.Transform(missile.visualDirection, quat);
        }
    }
}

