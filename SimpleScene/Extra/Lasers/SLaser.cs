using System;
using System.Drawing;
using System.Collections.Generic;
using SimpleScene.Util;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
	// TODO have an easy way to update laser start and finish
	// TODO ability to "hold" laser active

	// TODO pulse, interference effects
	// TODO laser "drift"
	/// <summary>
	/// Data model for a laser (which is to be used in conjunction with one or more laser beams)
	/// </summary>
	public class SLaser
	{
		/// <summary>
		/// Laser parameters. This is NOT to be tempered with by laser render and update code.
		/// </summary>
		public readonly SLaserParameters parameters = null;

		/// <summary>
		/// object that appears to emit the laser
		/// </summary>
		public SSObject sourceObject = null;

		/// <summary>
		/// Transform for the beam emission point in coordinates local to the source object
		/// </summary>
		public Matrix4 sourceTxfm = Matrix4.Identity;

		/// <summary>
		/// Object that the laser hits
		/// </summary>
		public SSObject targetObject = null;

		/// <summary> 
		/// Transform for the beam end point in coordinates local to the destination object
		/// </summary>
		public Matrix4 targetTxfm = Matrix4.Identity;

        /// <summary>
        /// Obstacle objects that laser may "hit" in addition to the destObject; 
        /// </summary>
        public List<SSObject> beamObstacles = null;

		#region local-copy intensity envelope
		/// <summary>
		/// Points to the intensity envelope actually used (so as to not temper with the original one
		/// that may be shared with other lasers)
		/// </summary>
		protected readonly LinearADSREnvelope _localIntensityEnvelope = null;

		/// <summary>
		/// Hack for the ADSR envelope to skip the infinite sustain part and begin releasing the laser
		/// </summary>
		protected bool _releaseDirty = false;
		#endregion

		public float time { get { return _localT; } }
		public float envelopeIntensity { get { return _envelopeIntensity; } }
		public bool hasExpired { get { return _localT > _localIntensityEnvelope.totalDuration; } }

		#region run-time updated variables
		protected float _localT = 0f;
		protected float _envelopeIntensity = 1f;
		protected SLaserBeam[] _beams = null;
		#endregion

		public SLaser(SLaserParameters laserParams)
		{
			this.parameters = laserParams;
			this._localIntensityEnvelope = laserParams.intensityEnvelope.Clone();

            _beams = new SLaserBeam[parameters.numBeams];
            for (int i = 0; i < parameters.numBeams; ++i) {
                _beams[i] = new SLaserBeam(this, i);
            }
		}

		public Vector3 sourcePos()
		{
            var mat = sourceTxfm;
            if (sourceObject != null) {
                mat = mat * sourceObject.worldMat;
            }
            return Vector3.Transform (Vector3.Zero, mat);
		}

		public Quaternion sourceOrient()
		{
            var ret = sourceTxfm.ExtractRotation();
            if (sourceObject != null) {
                ret = sourceObject.worldMat.ExtractRotation() * ret;
            }
			return ret;
		}

		public Vector3 destPos()
		{
            var mat = targetTxfm;
            if (targetObject != null) {
                mat = mat * targetObject.worldMat;
            }
			return Vector3.Transform (Vector3.Zero, mat);
		}

		public Vector3 direction()
		{
			return (destPos () - sourcePos ()).Normalized ();
		}

		public SLaserBeam beam(int id)
		{
			if (_beams == null || _beams.Length <= id) {
				return null;
			}
			return _beams [id];
		}

		public void release()
		{
			this._releaseDirty = true;
		}

		public void update(float elapsedS)
		{
			// compute local time
			_localT += elapsedS;

			// envelope intensity
			if (parameters.intensityEnvelope != null) {
				var env = _localIntensityEnvelope;
				if(_releaseDirty == true 
					&& _localT < (env.attackDuration + env.decayDuration + env.sustainDuration))
				{
					// force the existing envelope into release. this is a bit hacky.
					env.attackDuration = 0f;
					env.decayDuration = 0f;
					env.sustainDuration = _localT;
					_releaseDirty = false;
				}
				_envelopeIntensity = env.computeLevel (_localT);
			} else {
				_envelopeIntensity = 1f;
			}

			// update beam models
			foreach (var beam in _beams) {
				beam.update (_localT);
			}
		}
	}

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
		protected Vector3 _beamStart = Vector3.Zero;
		protected Vector3 _beamEnd = Vector3.Zero;
        protected bool _hitsAnObstacle = false;
		protected float _interferenceOffset = 0f;

		public Vector3 startPos { get { return _beamStart; } }
		public Vector3 endPos { get { return _beamEnd; } }
        public bool hitsAnObstacle { get { return _hitsAnObstacle; } }
		public float periodicIntensity { get { return _periodicIntensity; } }
		public float interferenceOffset { get { return _interferenceOffset; } }

		public Vector3 direction()
		{
			return (_beamEnd - _beamStart).Normalized ();
		}

        public float lengthFast()
        {
            return (_beamEnd - _beamStart).LengthFast;
        }

        public float lengthSq()
        {
            return (_beamEnd - _beamStart).LengthSquared;
        }

        public SSRay ray()
        {
            return new SSRay (_beamStart, direction());
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
			_interferenceOffset = laserParams.middleInterferenceUFunc (periodicT);
            _periodicIntensity = laserParams.intensityPeriodicFunction (periodicT);
            _periodicIntensity *= laserParams.intensityModulation (periodicT);

			// beam emission point placement
            _beamStart = _laser.sourcePos();
            _beamEnd = _laser.destPos();
            Vector3 localPlacement = laserParams.beamStartPlacementFunc(
                    _beamId, laserParams.numBeams, absoluteTimeS);
            if (localPlacement != Vector3.Zero) {
                var zAxis = (_beamEnd - _beamStart).Normalized ();
                Vector3 xAxis, yAxis;
                OpenTKHelper.TwoPerpAxes (zAxis, out xAxis, out yAxis);
                var placement = localPlacement.X * xAxis + localPlacement.Y * yAxis + localPlacement.Z * zAxis;
                _beamStart += laserParams.beamStartPlacementScale * placement;
                _beamEnd += laserParams.beamDestSpread * placement;
            }

			// periodic world-coordinate drift
            float driftX = laserParams.driftXFunc (periodicT);
            float driftY = laserParams.driftYFunc (periodicT);
            var driftMod = laserParams.driftModulationFunc (periodicT);
            driftX *= driftMod;
            driftY *= driftMod;

			if (driftX != 0f && driftY != 0f) {
				Vector3 driftXAxis, driftYAxis;
				OpenTKHelper.TwoPerpAxes (_laser.direction(), out driftXAxis, out driftYAxis);
				_beamEnd += (driftX * driftXAxis + driftY * driftYAxis);
			}

            // intersects with any of the intersecting objects
            _hitsAnObstacle = false;
            if (_laser.beamObstacles != null) {
            //if (false) {
                // TODO note the code below is slow. Wen you start having many lasers
                // this will cause problems. Consider using BVH for ray tests or analyzing
                // intersection math.
                var ray = this.ray();
                float closestDistance = this.lengthFast(); 
                foreach (var obj in _laser.beamObstacles) {
                    float distanceToInterect;
                    if (obj.Intersect(ref ray, out distanceToInterect)) {

                        if (distanceToInterect < closestDistance) {
                            closestDistance = distanceToInterect;
                            _hitsAnObstacle = true;
                        }
                    }
                }
                _beamEnd = _beamStart + ray.dir * closestDistance;
            }
		}
	}
}

