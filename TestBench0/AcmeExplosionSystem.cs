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

		protected readonly float m_duration;

		protected readonly float m_flameSmokeDuration;
		protected readonly SSRadialEmitter m_flameSmokeEmitter;
		protected readonly SSColorKeyframesEffector m_flamesSmokeColorEffector;
		protected readonly SSMasterScaleKeyframesEffector m_flameSmokeScaleEffector;

		protected readonly float m_flashDuration;
		protected readonly ParticlesSphereGenerator m_flashSphereGen;
		protected readonly SSParticlesFieldEmitter m_flashEmitter;
		protected readonly SSColorKeyframesEffector m_flashColorEffector;
		protected readonly SSMasterScaleKeyframesEffector m_flashScaleEffector;

		public AcmeExplosionSystem (
			int capacity, 
			float duration=5f, 
			RectangleF[] flameSmokeSprites = null,
			RectangleF[] flashSprites = null
		)
			: base(capacity)
		{
			{
				m_duration = duration;

				// flame/smoke
				{
					m_flameSmokeDuration = m_duration * 0.5f;
					m_flameSmokeEmitter = new SSRadialEmitter ();
					m_flameSmokeEmitter.SpriteRectangles = (flameSmokeSprites != null ? flameSmokeSprites : c_flameSmokeSpritesDefault);
					m_flameSmokeEmitter.ParticlesPerEmissionMin = 1;
					m_flameSmokeEmitter.ParticlesPerEmissionMax = 3;
					m_flameSmokeEmitter.EmissionIntervalMin = 0f;
					m_flameSmokeEmitter.EmissionIntervalMax = 0.1f * m_flameSmokeDuration;
					m_flameSmokeEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
					m_flameSmokeEmitter.Life = m_flameSmokeDuration;
					m_flameSmokeEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
					m_flameSmokeEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
					m_flameSmokeEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.5f);
					m_flameSmokeEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.5f);
					m_flameSmokeEmitter.RMin = 0f;
					m_flameSmokeEmitter.RMax = 1f;
					AddEmitter (m_flameSmokeEmitter);

					m_flamesSmokeColorEffector = new SSColorKeyframesEffector ();
					m_flamesSmokeColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
					m_flamesSmokeColorEffector.Keyframes.Add (0.5f*m_flameSmokeDuration, new Color4 (0f, 0f, 0f, 0.5f));
					m_flamesSmokeColorEffector.Keyframes.Add (m_flameSmokeDuration, new Color4 (0f, 0f, 0f, 0f));
					m_flamesSmokeColorEffector.ColorMask = FlameColor;
					m_flamesSmokeColorEffector.ParticleLifetime = m_flameSmokeDuration;
					AddEffector (m_flamesSmokeColorEffector);

					m_flameSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
					m_flameSmokeScaleEffector.Keyframes.Add (0f, 0.1f);
					m_flameSmokeScaleEffector.Keyframes.Add (0.5f * m_flameSmokeDuration, 1f);
					m_flameSmokeScaleEffector.Keyframes.Add (m_flameSmokeDuration, 1.2f);
					m_flameSmokeScaleEffector.ParticleLifetime = m_flameSmokeDuration;
					AddEffector (m_flameSmokeScaleEffector);

					m_flameSmokeEmitter.EffectorMask = m_flameSmokeScaleEffector.EffectorMask = m_flamesSmokeColorEffector.EffectorMask
						= (byte)ComponentMask.FlameSmoke;
				}

				// flash
				{
					m_flashDuration = 0.15f * m_duration;
					m_flashSphereGen = new ParticlesSphereGenerator ();
					m_flashEmitter = new SSParticlesFieldEmitter (m_flashSphereGen);
					m_flashEmitter.SpriteRectangles = (flashSprites != null ? flashSprites : c_flashSpritesDefault);
					m_flashEmitter.ParticlesPerEmissionMin = 1;
					m_flashEmitter.ParticlesPerEmissionMax = 2;
					m_flashEmitter.EmissionIntervalMin = 0f;
					m_flashEmitter.EmissionIntervalMax = 0.2f * m_flashDuration;
					m_flashEmitter.Life = m_flashDuration;
					m_flashEmitter.Velocity = Vector3.Zero;
					m_flashEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
					m_flashEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
					m_flashEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
					AddEmitter (m_flashEmitter);

					m_flashColorEffector = new SSColorKeyframesEffector ();
					m_flashColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
					m_flashColorEffector.Keyframes.Add (m_flashDuration, new Color4 (1f, 1f, 1f, 0f));
					m_flashColorEffector.ColorMask = FlashColor;
					m_flashColorEffector.ParticleLifetime = m_flashDuration;
					AddEffector (m_flashColorEffector);

					m_flashScaleEffector = new SSMasterScaleKeyframesEffector ();
					m_flashScaleEffector.ParticleLifetime = m_flashDuration;
					m_flashScaleEffector.Keyframes.Add (0f, 1f);
					m_flashScaleEffector.Keyframes.Add (m_flashDuration, 1.5f);
					AddEffector (m_flashScaleEffector);

					m_flashEmitter.EffectorMask = m_flashColorEffector.EffectorMask = m_flashScaleEffector.EffectorMask 
						= (byte)ComponentMask.Flash;
				}
			}
		}

		// TODO Reset/reconfigure

		public virtual void ShowExplosion(Vector3 position, float intensity)
		{
			// flame/smoke
			#if true
			m_flameSmokeEmitter.ComponentScale = new Vector3(intensity, intensity, 1f);
			m_flameSmokeEmitter.VelocityMagnitudeMin = 0.20f * intensity;
			m_flameSmokeEmitter.VelocityMagnitudeMax = 0.30f * intensity;
			//m_flameSmokeEmitter.VelocityMagnitude = 0f;
			m_flameSmokeEmitter.Center = position;
			m_flameSmokeEmitter.TotalEmissionsLeft = 3;
			#endif

			// flash
			#if true
			m_flashEmitter.ComponentScale = new Vector3(intensity);
			m_flashSphereGen.Center = position;
			m_flashSphereGen.Radius = 0.3f * intensity;
			m_flashEmitter.TotalEmissionsLeft = 2;
			#endif
		}
	}
}

