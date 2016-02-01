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
        // TODO switch targets while firing

        public delegate Vector3 TargetVelFunc(SSObject target);

        /// <summary>
        /// Customized to spawn laser burn particles at target velocity
        /// </summary>
        public readonly TargetVelFunc targetVelFunc;

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

        /// <summary> get added to beam placement function results </summary>
        public List<Vector3> beamOriginPresets = null;

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

        public SLaser(SLaserParameters laserParams, TargetVelFunc targetVelFunc=null)
		{
			this.parameters = laserParams;
			this._localIntensityEnvelope = laserParams.intensityEnvelope.Clone();
            this.targetVelFunc = targetVelFunc;

            _beams = new SLaserBeam[parameters.numBeams];
            for (int i = 0; i < parameters.numBeams; ++i) {
                _beams[i] = new SLaserBeam(this, i);
            }
		}

		public Quaternion sourceOrient()
		{
            var ret = sourceTxfm.ExtractRotation();
            if (sourceObject != null) {
                ret = sourceObject.worldMat.ExtractRotation() * ret;
            }
			return ret;
		}

        public Vector3 targetVelocity()
        {
            if (targetVelFunc != null) {
                return targetVelFunc(targetObject);
            }
            return Vector3.Zero;
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

        internal Vector3 txfmSourceToWorld(Vector3 localPos)
        {
            // TODO cache matrix?
            var mat = sourceTxfm;
            if (sourceObject != null) {
                mat = mat * sourceObject.worldMat;
            }
            return Vector3.Transform(localPos, mat);
        }

        internal Vector3 txfmTargetToWorld(Vector3 localPos)
        {
            var mat = targetTxfm;
            if (targetObject != null) {
                mat = mat * targetObject.worldMat;
            }
            return Vector3.Transform(localPos, mat);
        }

	}
}

