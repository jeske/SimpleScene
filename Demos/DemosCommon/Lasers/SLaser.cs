using System;
using OpenTK.Graphics;
using SimpleScene.Util;
using OpenTK;

namespace SimpleScene.Demos
{
	// TODO have an easy way to update laser start and finish
	// TODO ability to "hold" laser active

	// TODO pulse, interference effects
	// TODO laser "drift"

	public class SLaserParameters
	{
		#region auxiliary function types
		/// <summary>
		/// Intensity as a function of period fraction t (from 0 to 1)
		/// </summary>
		public delegate float PeriodicFunction(float t);

		/// <summary>
		/// Beam placement functions positions beam origins for one or more laser beams. When implementing
		/// assume laser origin is at (0, 0, 0) and the target is in the +z direction
		/// </summary>
		public delegate Vector3 BeamPlacementFunction(int beamID, int numBeams, float t);
		#endregion

		#region middle sprites
		public Color4 backgroundColor = Color4.Magenta;
		public Color4 overlayColor = Color4.White;

		/// <summary>
		/// padding for the start+middle stretched sprite. Mid section vertices gets streched 
		/// beyond this padding.
		/// </summary>
		public float laserSpritePadding = 0.05f;

		/// <summary>
		/// width of the middle section sprite (in world units)
		/// </summary>
		public float backgroundWidth = 2f;
		#endregion

		#region interference sprite
		public Color4 interferenceColor = Color4.White;

		/// <summary>
		/// Interference sprite will be drawn X times thicker than the start+middle section width
		/// </summary>
		public float interferenceScale = 2.0f;

		/// <summary>
		/// Describes the value of the U-coordinate offset for the interference as a function of time
		/// </summary>
		public PeriodicFunction interferenceUFunc = (t) => -0.75f * t;
		#endregion

		#region start-only radial sprites
		/// <summary>
		/// start-only radial emission sprites will be drawn x times larger than the middle section width
		/// when at full intensity (looking straight into the laser)
		/// </summary>
		public float startPointScale = 1f;
		#endregion

		#region periodic intensity		
		/// <summary>
		/// Intensity as a function of period fraction t (from 0 to 1)
		/// </summary>
		public PeriodicFunction intensityPeriodicFunction = 
			t => 0.8f + 0.1f * (float)Math.Sin(2.0f * (float)Math.PI * 10f * t) 
							 * (float)Math.Sin(2.0f * (float)Math.PI * 2f * t) ;

		/// <summary>
		/// Further periodic modulation or scale, if needed
		/// </summary>
		public PeriodicFunction intensityModulation =
			t => 1f;
		#endregion

		#region intensity ADSR envelope
		/// <summary>
		/// Attack-decay-sustain-release envelope, with infinite sustain to simulate
		/// "engaged-until-released" lasers by default
		/// </summary>
		public ADSREnvelope intensityEnvelope 
			= new ADSREnvelope (0.15f, 0.15f, float.PositiveInfinity, 0.5f, 1f, 0.7f);
			//= new ADSREnvelope (0.20f, 0.20f, float.PositiveInfinity, 1f, 1f, 0.7f);
		#endregion

		#region periodic drift
		public PeriodicFunction driftXFunc = 
			t => (float)Math.Cos (2.0f * (float)Math.PI * 0.1f * t) 
		       * (float)Math.Cos (2.0f * (float)Math.PI * 0.53f * t);

		public PeriodicFunction driftYFunc =
			t => (float)Math.Sin (2.0f * (float)Math.PI * 0.1f * t) 
			   * (float)Math.Sin (2.0f * (float)Math.PI * 0.57f * t);

		public PeriodicFunction driftModulationFunc =
			t => 0.1f;
		#endregion

		#region multi-beam settings
		/// <summary>
		/// Each "laser" entry can produce multiple rendered beams to model synchronized laser cannon arrays
		/// </summary>
		public int numBeams = 1;

		/// <summary>
		/// Beam placement functions positions beam origins for one or more laser beams. When implementing
		/// assume laser origin is at (0, 0, 0) and the target is in the +z direction. Default function
		/// arranges beams in a circle around the origin.
		/// </summary>
		public BeamPlacementFunction beamStartPlacementFunc = (beamID, numBeams, t) => {
			if (numBeams <= 1) {
				return Vector3.Zero;
			} else {
				float a = 2f * (float)Math.PI / (float)numBeams * beamID;
				return new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 0f);
			}
		};

		/// <summary>
		/// Beam-placement function output will be scaled by this much to produce the final beam start
		/// positions in world coordinates
		/// </summary>
		public float beamStartPlacementScale = 1f;

		/// <summary>
		/// Multiple beams ends will be this far apart from the laser endpoint. (in world coordinates)
		/// </summary>
		public float beamDestSpread = 0f;
		#endregion

        #region emission flare 
        public float emissionFlareSizeMaxPx = 500f;
        public float occDiskDirOffset = 0.5f;
        public float occDisk1RadiusPx = 15f;
        public float occDisk2RadiusWU = 0.5f;
        public float occDisksAlpha = 0.0001f;
        //public float occDisksAlpha = 0.3f;
        #endregion

        #region screen hit flare
        public float hitFlareSizeMaxPx = 2000f;
        public float coronaBackgroundScale = 1f;
        public float coronaOverlayScale = 0.5f;
        public float ring1Scale = 0.5f;
        public float ring2Scale = 0.25f;
        #endregion
	}

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
		/// object emitting the laser
		/// </summary>
		public SSObject sourceObject = null;

		/// <summary>
		/// Transform for the beam emission point in coordinates local to the source object
		/// </summary>
		public Matrix4 sourceTxfm = Matrix4.Identity;

		/// <summary>
		/// Object that the laser hits
		/// </summary>
		public SSObject destObject = null;

		/// <summary>
		/// Transform for the beam end point in coordinates local to the destination object
		/// </summary>
		public Matrix4 destTxfm = Matrix4.Identity;

		#region local-copy intensity envelope
		/// <summary>
		/// Points to the intensity envelope actually used (so as to not temper with the original one
		/// that may be shared with other lasers)
		/// </summary>
		protected readonly ADSREnvelope _localIntensityEnvelope = null;

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
			return Vector3.Transform (Vector3.Zero, sourceTxfm * sourceObject.worldMat);
		}

		public Quaternion sourceOrient()
		{
			return sourceObject.worldMat.ExtractRotation () * sourceTxfm.ExtractRotation();
		}

		public Vector3 destPos()
		{
			return Vector3.Transform (Vector3.Zero, destTxfm * destObject.worldMat);
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
		protected static readonly Random _random = new Random();

		protected readonly int _beamId;
		protected readonly SLaser _laser;

		protected float _periodicTOffset = 0f;

		protected float _periodicT = 0f;
		protected float _periodicIntensity = 1f;
		protected Vector3 _beamStart = Vector3.Zero;
		protected Vector3 _beamEnd = Vector3.Zero;
		protected float _interferenceOffset = 0f;

		public Vector3 startPos { get { return _beamStart; } }
		public Vector3 endPos { get { return _beamEnd; } }
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

			// interference sprite U coordinates' offset
			if (laserParams.interferenceUFunc != null) {
				_interferenceOffset = laserParams.interferenceUFunc (periodicT);
			} else {
				_interferenceOffset = 0f;
			}

			// periodic intensity
			if (laserParams.intensityPeriodicFunction != null) {
				_periodicIntensity = laserParams.intensityPeriodicFunction (periodicT);
			} else {
				_periodicIntensity = 1f;
			}

			if (laserParams.intensityModulation != null) {
				_periodicIntensity *= laserParams.intensityModulation (periodicT);
			}

			// beam emission point placement
			var src = _laser.sourcePos();
			var dst = _laser.destPos();
			if (laserParams.beamStartPlacementFunc != null) {
				var zAxis = (dst - src).Normalized ();
				Vector3 xAxis, yAxis;
				OpenTKHelper.TwoPerpAxes (zAxis, out xAxis, out yAxis);
				var localPlacement = laserParams.beamStartPlacementFunc (_beamId, laserParams.numBeams, absoluteTimeS);
				var placement = localPlacement.X * xAxis + localPlacement.Y * yAxis + localPlacement.Z * zAxis;
				_beamStart = src + laserParams.beamStartPlacementScale * placement;
				_beamEnd = dst + laserParams.beamDestSpread * placement;
			} else {
				_beamStart = src;
				_beamEnd = dst;
			}

			// periodic world-coordinate drift
			float driftX, driftY;
			if (laserParams.driftXFunc != null) {
				driftX = laserParams.driftXFunc (periodicT);
			} else {
				driftX = 0f;
			}

			if (laserParams.driftYFunc != null) {
				driftY = laserParams.driftYFunc (periodicT);
			} else {
				driftY = 0f;
			}

			if (laserParams.driftModulationFunc != null) {
				var driftMod = laserParams.driftModulationFunc (periodicT);
				driftX *= driftMod;
				driftY *= driftMod;
			}

			if (driftX != 0f && driftY != 0f) {
				Vector3 driftXAxis, driftYAxis;
				OpenTKHelper.TwoPerpAxes (_laser.direction(), out driftXAxis, out driftYAxis);
				_beamEnd += (driftX * driftXAxis + driftY * driftYAxis);
			}
		}
	}
}

