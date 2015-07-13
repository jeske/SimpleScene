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

	public class SimpleLaserParameters
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

		#region start-only radial sprite
		/// <summary>
		/// start-only radial emission sprites will be drawn x times larger than the middle section width
		/// </summary>
		public float startPointScale = 1.0f;
		#endregion

		#region periodic intensity		
		/// <summary>
		/// Intensity as a function of period fraction t (from 0 to 1)
		/// </summary>
		public PeriodicFunction intensityPeriodicFunction = 
			t => 0.8f + 0.3f * (float)Math.Sin(2.0f * (float)Math.PI * 10f * t) 
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
	}

	public class SimpleLaser
	{
		// TODO nextDestObject??

		/// <summary>
		/// Called when the laser has fully faded and is safe to delete by laser management systems
		/// </summary>
		public delegate void ReleaseCallbackDelegate(SimpleLaser invoker);

		/// <summary>
		/// Laser parameters. This is NOT to be tempered with by laser render and update code.
		/// </summary>
		public readonly SimpleLaserParameters parameters = null;

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
		public readonly ADSREnvelope localIntensityEnvelope = null;

		/// <summary>
		/// Hack for the ADSR envelope to skip the infinite sustain part and begin releasing the laser
		/// </summary>
		internal bool releaseDirty = false;

		/// <summary>
		/// Called when the laser has fully faded and is safe to delete by laser management systems
		/// </summary>
		public ReleaseCallbackDelegate postReleaseFunc = null;
		#endregion

		public SimpleLaser(SimpleLaserParameters laserParams)
		{
			this.parameters = laserParams;
			this.localIntensityEnvelope = laserParams.intensityEnvelope.Clone();
		}

		public Vector3 sourcePos()
		{
			return Vector3.Transform (Vector3.Zero, sourceTxfm * sourceObject.worldMat);
		}

		public Vector3 destPos()
		{
			return Vector3.Transform (Vector3.Zero, destTxfm * destObject.worldMat);
		}

		public void release()
		{
			this.releaseDirty = true;
		}
	}
}

