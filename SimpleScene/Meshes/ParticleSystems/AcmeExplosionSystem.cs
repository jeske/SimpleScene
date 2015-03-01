using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{
	public class AcmeExplosionSystem : SSParticleSystem
	{
		enum ComponentMask : byte { 
			FlameSmoke = 0x1, 
			Flash = 0x2,
			FlyingSparks = 0x4, 
			SmokeTrails = 0x8,
			RoundSparks = 0x10,
			Debris = 0x20,
			Shockwave = 0x40,
		};

		#region define sprite and size locations variables
		// default to fig7.png asset by Mike McClelland

		/// <summary>
		/// Override to match your flame and smoke effect sprites (one RectangleF per available sprite)
		/// </summary>
		public static readonly RectangleF[] c_flamesSpriteSpritesDefault = {
			new RectangleF(0f,    0f,    0.25f, 0.25f),
			new RectangleF(0f,    0.25f, 0.25f, 0.25f),
			new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
			new RectangleF(0.25f, 0f,    0.25f, 0.25f),
		};

		/// <summary>
		/// Override to match your flash effect sprites (one RectangleF per available sprite)
		/// </summary>
		public static readonly RectangleF[] c_flashSpritesDefault = {
			new RectangleF(0.5f,  0.5f,  0.25f, 0.25f),
			new RectangleF(0.5f,  0.75f, 0.25f, 0.25f),
			new RectangleF(0.75f, 0.75f, 0.25f, 0.25f),
			new RectangleF(0.75f, 0.5f,  0.25f, 0.25f),
		};

		/// <summary>
		/// Override to match your smoke trails sprites (one RectangleF per available sprite)
		/// </summary>
		public static readonly RectangleF[] c_smokeTrailsSpritesDefault = {
			new RectangleF(0f, 0.5f,   0.5f, 0.125f),
			new RectangleF(0f, 0.625f, 0.5f, 0.125f),
			new RectangleF(0f, 0.75f,  0.5f, 0.125f),
		};

		// TODO debris sprites
		// TODO shockwave sprites
		// TODO round sparks sprites
		// TODO flying sparks sprites
		#endregion

		protected readonly SSFixedPositionEmitter m_flashEmitter;

		public AcmeExplosionSystem (int capacity, float duration=1f, RectangleF[] flashSprites = null)
			: base(capacity)
		{
			{
				// flash
				m_flashEmitter = new SSFixedPositionEmitter ();
				var flashColorEffector = new SSColorKeyframesEffector ();
				var flashScaleEffector = new SSMasterScaleKeyframesEffector ();
				m_flashEmitter.EffectorMask = flashColorEffector.EffectorMask = flashScaleEffector.EffectorMask 
					= (byte)ComponentMask.Flash;

				m_flashEmitter.SpriteRectangles = (flashSprites != null ? flashSprites : c_flashSpritesDefault);
				m_flashEmitter.ParticlesPerEmission = 1;
				m_flashEmitter.Velocity = Vector3.Zero;
				//AddEmitter (m_flashEmitter);

				flashColorEffector.Keyframes.Add (0f, new Color4(1f, 1f, 1f, 1f));
				flashColorEffector.Keyframes.Add (duration, new Color4 (1f, 1f, 1f, 0f));
				AddEffector (flashColorEffector);

				flashScaleEffector.Keyframes.Add (0f, 1f);
				flashScaleEffector.Keyframes.Add (duration, 1.5f);
				AddEffector (flashScaleEffector);
			}
		}

		public void Explode(Vector3 position)
		{
			m_flashEmitter.Position = position;
			m_flashEmitter.EmitParticles (storeNewParticle);
		}
	}
}

