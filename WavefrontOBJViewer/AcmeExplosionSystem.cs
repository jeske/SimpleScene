using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{
	/// <summary>
	/// An explosion system based on a a gamedev.net article
	/// http://www.gamedev.net/page/resources/_/creative/visual-arts/make-a-particle-explosion-effect-r2701
	/// </summary>
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
		/// Default locations of flame sprites in fig7.png
		/// </summary>
		public static readonly RectangleF[] c_flameSmokeSpritesDefault = {
			new RectangleF(0f,    0f,    0.25f, 0.25f),
			new RectangleF(0f,    0.25f, 0.25f, 0.25f),
			new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
			new RectangleF(0.25f, 0f,    0.25f, 0.25f),
		};

		/// <summary>
		/// Default locations of flash sprites in fig7.png
		/// </summary>
		public static readonly RectangleF[] c_flashSpritesDefault = {
			new RectangleF(0.5f,  0f,    0.25f, 0.25f),
			new RectangleF(0.75f, 0f,    0.25f, 0.25f),
			new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
			new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
		};

		/// <summary>
		/// Default locations of smoke trails sprites in fig7.png
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

		#region effects' colors
		public Color4 FlameColor = Color4.Orange;
		public Color4 FlashColor = Color4.Yellow;

		#endregion

		protected readonly SSRadialEmitter m_flameSmokeEmitter;
		protected readonly SSMasterScaleKeyframesEffector m_flameSmokeScaleEffector;
		protected readonly SSColorKeyframesEffector m_flamesSmokeColorEffector;

		protected readonly SSFixedPositionEmitter m_flashEmitter;
		protected readonly SSColorKeyframesEffector m_flashColorEffector;
		protected readonly SSMasterScaleKeyframesEffector m_flashScaleEffector;

		public AcmeExplosionSystem (
			int capacity, 
			float duration=10f, 
			RectangleF[] flameSmokeSprites = null,
			RectangleF[] flashSprites = null
		)
			: base(capacity)
		{
			{
				// flame/smoke
				{
					float flameSmokeDuration = duration;
					m_flameSmokeEmitter = new SSRadialEmitter ();
					m_flameSmokeEmitter.SpriteRectangles = (flameSmokeSprites != null ? flameSmokeSprites : c_flameSmokeSpritesDefault);
					m_flameSmokeEmitter.ParticlesPerEmission = 6;
					m_flameSmokeEmitter.Life = flameSmokeDuration;
					m_flameSmokeEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
					m_flameSmokeEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
					m_flameSmokeEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.05f);
					m_flameSmokeEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.05f);
					m_flameSmokeEmitter.RMin = 0f;
					m_flameSmokeEmitter.RMax = 1f;

					m_flamesSmokeColorEffector = new SSColorKeyframesEffector ();
					m_flamesSmokeColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
					m_flamesSmokeColorEffector.Keyframes.Add (0.75f*flameSmokeDuration, new Color4 (0f, 0f, 0f, 0.5f));
					m_flamesSmokeColorEffector.Keyframes.Add (flameSmokeDuration, new Color4 (0f, 0f, 0f, 0f));
					AddEffector (m_flamesSmokeColorEffector);

					m_flameSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
					m_flameSmokeScaleEffector.Keyframes.Add (0f, 0.1f);
					m_flameSmokeScaleEffector.Keyframes.Add (0.75f * flameSmokeDuration, 1f);
					m_flameSmokeScaleEffector.Keyframes.Add (flameSmokeDuration, 1.2f);
					AddEffector (m_flameSmokeScaleEffector);

					// TODO flame/smoke color effector

					m_flameSmokeEmitter.EffectorMask = m_flameSmokeScaleEffector.EffectorMask = m_flamesSmokeColorEffector.EffectorMask
						= (byte)ComponentMask.FlameSmoke;
				}

				// flash
				{
					float flashDuration = 0.1f * duration;
					m_flashEmitter = new SSFixedPositionEmitter ();
					m_flashEmitter.SpriteRectangles = (flashSprites != null ? flashSprites : c_flashSpritesDefault);
					m_flashEmitter.ParticlesPerEmission = 1;
					m_flashEmitter.Velocity = Vector3.Zero;
					m_flashEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
					m_flashEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
					m_flashEmitter.Life = flashDuration;
					//AddEmitter (m_flashEmitter);

					m_flashColorEffector = new SSColorKeyframesEffector ();
					m_flashColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
					m_flashColorEffector.Keyframes.Add (flashDuration, new Color4 (1f, 1f, 1f, 0f));
					AddEffector (m_flashColorEffector);

					m_flashScaleEffector = new SSMasterScaleKeyframesEffector ();
					m_flashScaleEffector.Keyframes.Add (0f, 1f);
					m_flashScaleEffector.Keyframes.Add (flashDuration, 1.5f);
					AddEffector (m_flashScaleEffector);

					m_flashEmitter.EffectorMask = m_flashColorEffector.EffectorMask = m_flashScaleEffector.EffectorMask 
						= (byte)ComponentMask.Flash;
				}
			}
		}

		public void ShowExplosion(Vector3 position, float intensity)
		{
			// flame/smoke
			#if true
			m_flameSmokeScaleEffector.Reset ();
			m_flameSmokeScaleEffector.Amplification = intensity;

			m_flamesSmokeColorEffector.Reset();
			m_flamesSmokeColorEffector.ColorMask = FlameColor;

			m_flameSmokeEmitter.VelocityMagnitudeMin = 0.20f * intensity;
			m_flameSmokeEmitter.VelocityMagnitudeMax = 0.30f * intensity;
			//m_flameSmokeEmitter.VelocityMagnitude = 0f;
			m_flameSmokeEmitter.Center = position;
			m_flameSmokeEmitter.EmitParticles (storeNewParticle);
			#endif

			// flash
			#if true
			m_flashScaleEffector.Reset ();
			m_flashScaleEffector.Amplification = intensity;

			m_flashColorEffector.Reset ();
			m_flashColorEffector.ColorMask = FlashColor;

			m_flashEmitter.Position = position;
			m_flashEmitter.EmitParticles (storeNewParticle);
			#endif
		}
	}
}

