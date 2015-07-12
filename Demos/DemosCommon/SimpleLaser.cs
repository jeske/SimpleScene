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
		#region types
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
		/// How fast (in world-units/sec) the interference texture coordinates are moving
		/// </summary>
		public float interferenceVelocity = -0.5f;

		/// <summary>
		/// Interference sprite will be drawn X times thicker than the start+middle section width
		/// </summary>
		public float interferenceScale = 2.0f;
		#endregion

		#region start-only sprite
		/// <summary>
		/// start-only (emission) sprites will be drawn x times larger than the start+middle section width
		/// </summary>
		public float startPointScale = 1.0f;
		#endregion

		#region periodic intensity		
		public float intensityFrequency = 10f; // in Hz

		/// <summary>
		/// Intensity as a function of period fraction t (from 0 to 1)
		/// </summary>
		public PeriodicFunction intensityPeriodicFunction = 
			t => 0.8f + 0.3f * (float)Math.Sin(2.0f * (float)Math.PI * t) 
							 * (float)Math.Sin(2.0f * (float)Math.PI * t * 0.2f) ;
		/// <summary>
		/// Further periodic modulation, if needed
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
			= new ADSREnvelope (0.10f, 0.10f, float.PositiveInfinity, 0.15f, 1f, 0.8f);
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
		public int numBeams = 1;

		public BeamPlacementFunction beamPlacementFunc = (beamID, numBeams, t) => {
			if (numBeams <= 1) {
				return Vector3.Zero;
			} else {
				float a = 2f * (float)Math.PI / (float)numBeams * beamID;
				return new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 0f);
			}
		};

		public float beamPlacementScale = 1f;
		#endregion
	}

	public class SimpleLaser
	{
		public delegate void ReleaseCallbackDelegate(SimpleLaser invoker);

		public readonly SimpleLaserParameters parameters = null;

		public SSObject sourceObject = null;
		public SSObject destObject = null;
		// TODO nextDestObject??
		public Matrix4 sourceTxfm = Matrix4.Identity;
		public Matrix4 destTxfm = Matrix4.Identity;

		#region envelope release and self-destroy callback
		/// <summary>
		/// Hack to bend the ADSR envelope to skip the infinite sustain part
		/// </summary>
		internal bool releaseDirty = false;

		/// <summary>
		/// Points to the intensity envelope actually used
		/// </summary>
		public readonly ADSREnvelope localIntensityEnvelope = null;

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

