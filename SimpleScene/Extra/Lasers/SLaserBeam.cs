using System;
using System.Drawing;
using System.Collections.Generic;
using SimpleScene.Util;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{

    /// <summary>
    /// Data model for a laser beam
    /// </summary>
    public class SLaserBeam
    {
        // TODO receive laser hit locations

        protected static readonly Random _random = new Random();

        protected readonly int _beamId;
        protected readonly SLaser _laser;

        protected float _periodicTOffset = 0f;

        protected float _periodicT = 0f;
        protected float _periodicIntensity = 1f;
        protected Vector3 _beamStartWorld = Vector3.Zero;
        protected Vector3 _beamEndWorld = Vector3.Zero;
        protected bool _hitsAnObstacle = false;
        protected float _interferenceOffset = 0f;

        public Vector3 startPosWorld { get { return _beamStartWorld; } }
        public Vector3 endPosWorld { get { return _beamEndWorld; } }
        public bool hitsAnObstacle { get { return _hitsAnObstacle; } }
        public float periodicIntensity { get { return _periodicIntensity; } }
        public float interferenceOffset { get { return _interferenceOffset; } }

        public Vector3 directionWorld()
        {
            return (_beamEndWorld - _beamStartWorld).Normalized ();
        }

        public float lengthFastWorld()
        {
            return (_beamEndWorld - _beamStartWorld).LengthFast;
        }

        public float lengthSqWorld()
        {
            return (_beamEndWorld - _beamStartWorld).LengthSquared;
        }

        public SSRay rayWorld()
        {
            return new SSRay (_beamStartWorld, directionWorld());
        }

        public SLaserBeam(SLaser laser, int beamId)
        {
            _laser = laser;
            _beamId = beamId;

            // make periodic beam effects slightly out of sync with each other
            _periodicTOffset = (float)_random.NextDouble() * 10f;
        }

        public void update(float absoluteTimeS)
        {
            float periodicT = absoluteTimeS + _periodicTOffset;
            var laserParams = _laser.parameters;

            // update variables
            _interferenceOffset = laserParams.middleInterferenceUFunc(periodicT);
            _periodicIntensity = laserParams.intensityPeriodicFunction(periodicT);
            _periodicIntensity *= laserParams.intensityModulation(periodicT);

            // evaluate start position for this update
            Vector3 beamOriginLocal = Vector3.Zero;
            if (_laser.beamOriginPresets != null && _laser.beamOriginPresets.Count > 1) {
                var presetIdx = _beamId;
                if (presetIdx >= _laser.beamOriginPresets.Count) {
                    presetIdx %= _laser.beamOriginPresets.Count;
                }
                beamOriginLocal = _laser.beamOriginPresets [presetIdx];
            }
            _beamStartWorld = _laser.txfmSourceToWorld(beamOriginLocal);
            _beamEndWorld = _laser.txfmTargetToWorld(Vector3.Zero);
            Vector3 beamDirWorld = (_beamEndWorld - _beamStartWorld).Normalized();
            Vector3 laserXaxisWorld, laserYaxisWorld;
            OpenTKHelper.TwoPerpAxes(beamDirWorld, out laserXaxisWorld, out laserYaxisWorld);
            // local placement to start start placement in world coordinates
            Vector3 localPlacement = laserParams.getBeamPlacementVector(_beamId, laserParams.numBeams, absoluteTimeS);
            Vector3 startPlacement = localPlacement * laserParams.beamStartPlacementScale;
            Vector3 startPlacementWorldOffset = 
                laserXaxisWorld * startPlacement.X + laserYaxisWorld * startPlacement.Y + beamDirWorld * startPlacement.Z;
            _beamStartWorld += startPlacementWorldOffset;

            // end position in world coordinates including drift; before intersection test
            float driftX = laserParams.driftXFunc (periodicT);
            float driftY = laserParams.driftYFunc (periodicT);
            var driftMod = laserParams.driftModulationFunc (periodicT);
            Vector3 driftedEndPlacement = laserParams.beamDestSpread * localPlacement 
                + new Vector3(driftX, driftY, 0f) * driftMod;
            Vector3 endPlacementWorldOffset = 
                laserXaxisWorld * driftedEndPlacement.X + laserYaxisWorld * driftedEndPlacement.Y + beamDirWorld * driftedEndPlacement.Z;
            _beamEndWorld += endPlacementWorldOffset;

            _hitsAnObstacle = false;
            // intersects with any of the intersecting objects
            if (_laser.beamObstacles != null) {
                //if (false) {
                // TODO note the code below is slow. Wen you start having many lasers
                // this will cause problems. Consider using BVH for ray tests or analyzing
                // intersection math.
                float closestDistance = float.PositiveInfinity; 
                foreach (var obj in _laser.beamObstacles) {
                    float distanceToInterect;
                    var ray = new SSRay(_beamStartWorld, (_beamEndWorld - _beamStartWorld).Normalized());
                    if (obj.Intersect(ref ray, out distanceToInterect)) {
                        if (distanceToInterect < closestDistance) {
                            closestDistance = distanceToInterect;
                            _hitsAnObstacle = true;
                            _beamEndWorld = _beamStartWorld + ray.dir * closestDistance;
                        }
                    }
                }
            }
        }
    }
}

