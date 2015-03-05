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

		protected readonly float m_flashDuration;
		protected readonly ParticlesSphereGenerator m_flashSphereGen;
		protected readonly SSParticlesFieldEmitter m_flashEmitter;

		protected int m_explosionIndexMask = 0x1;

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
					m_flameSmokeDuration = m_duration;
					m_flameSmokeEmitter = new SSRadialEmitter ();
					m_flameSmokeEmitter.SpriteRectangles = (flameSmokeSprites != null ? flameSmokeSprites : c_flameSmokeSpritesDefault);
					m_flameSmokeEmitter.ParticlesPerEmission = 6;
					m_flameSmokeEmitter.Life = m_flameSmokeDuration;
					m_flameSmokeEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
					m_flameSmokeEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
					m_flameSmokeEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.05f);
					m_flameSmokeEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.05f);
					m_flameSmokeEmitter.RMin = 0f;
					m_flameSmokeEmitter.RMax = 1f;
				}

				// flash
				{
					m_flashDuration = 0.1f * m_duration;
					m_flashSphereGen = new ParticlesSphereGenerator ();
					m_flashEmitter = new SSParticlesFieldEmitter (m_flashSphereGen);
					m_flashEmitter.SpriteRectangles = (flashSprites != null ? flashSprites : c_flashSpritesDefault);
					m_flashEmitter.ParticlesPerEmission = 5;
					m_flashEmitter.Velocity = Vector3.Zero;
					m_flashEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
					m_flashEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
					m_flashEmitter.Life = m_flashDuration;
				}
			}
		}

		public virtual void ShowExplosion(Vector3 position, float intensity)
		{
			int shiftedIndexMask = m_explosionIndexMask << 8;

			// flame/smoke
			#if true
			var flamesSmokeColorEffector = new SSColorKeyframesEffector ();
			flamesSmokeColorEffector.MaskMathFunction = SSParticleEffector.MatchFunction.Equals;
			flamesSmokeColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
			flamesSmokeColorEffector.Keyframes.Add (0.75f*m_flameSmokeDuration, new Color4 (0f, 0f, 0f, 0.5f));
			flamesSmokeColorEffector.Keyframes.Add (m_flameSmokeDuration, new Color4 (0f, 0f, 0f, 0f));
			flamesSmokeColorEffector.ColorMask = FlameColor;
			flamesSmokeColorEffector.ParticleLifetime = m_flameSmokeDuration;
			AddEffector (flamesSmokeColorEffector);

			var flameSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
			flameSmokeScaleEffector.MaskMathFunction = SSParticleEffector.MatchFunction.Equals;
			flameSmokeScaleEffector.Keyframes.Add (0f, 0.1f);
			flameSmokeScaleEffector.Keyframes.Add (0.75f * m_flameSmokeDuration, 1f);
			flameSmokeScaleEffector.Keyframes.Add (m_flameSmokeDuration, 1.2f);
			flameSmokeScaleEffector.Amplification = intensity;
			flameSmokeScaleEffector.ParticleLifetime = m_flameSmokeDuration;
			AddEffector (flameSmokeScaleEffector);

			m_flameSmokeEmitter.VelocityMagnitudeMin = 0.20f * intensity;
			m_flameSmokeEmitter.VelocityMagnitudeMax = 0.30f * intensity;
			//m_flameSmokeEmitter.VelocityMagnitude = 0f;
			m_flameSmokeEmitter.Center = position;
			m_flameSmokeEmitter.EffectorMask = flameSmokeScaleEffector.EffectorMask = flamesSmokeColorEffector.EffectorMask
				= (ushort)(shiftedIndexMask | (int)ComponentMask.FlameSmoke);
			m_flameSmokeEmitter.EmitParticles (storeNewParticle);
			#endif

			// flash
			#if true
			var flashColorEffector = new SSColorKeyframesEffector ();
			flashColorEffector.MaskMathFunction = SSParticleEffector.MatchFunction.Equals;
			flashColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
			flashColorEffector.Keyframes.Add (m_flashDuration, new Color4 (1f, 1f, 1f, 0f));
			flashColorEffector.ColorMask = FlashColor;
			flashColorEffector.ParticleLifetime = m_flashDuration;
			AddEffector (flashColorEffector);

			var flashScaleEffector = new SSMasterScaleKeyframesEffector ();
			flashScaleEffector.MaskMathFunction = SSParticleEffector.MatchFunction.Equals;
			flashScaleEffector.Keyframes.Add (0f, 1f);
			flashScaleEffector.Keyframes.Add (m_flashDuration, 1.5f);
			flashScaleEffector.Amplification = intensity;
			flashScaleEffector.ParticleLifetime = m_flashDuration;
			AddEffector (flashScaleEffector);

			m_flashEmitter.EffectorMask = flashColorEffector.EffectorMask = flashScaleEffector.EffectorMask 
				= (ushort)(shiftedIndexMask | (int)ComponentMask.Flash);
			m_flashSphereGen.Center = position;
			m_flashSphereGen.Radius = 0.5f * intensity;
			m_flashEmitter.EmitParticles (storeNewParticle);
			#endif

			m_explosionIndexMask <<= 1;
			if (m_explosionIndexMask == 0x100) {
				m_explosionIndexMask = 0x1;
			}

		}

		public override void Simulate (float timeDelta)
		{
			base.Simulate (timeDelta);

			// remove old effectors
			for (int i = 0; i < m_effectors.Count; ++i) {
				if (m_effectors [i].TimeSinceReset >= m_duration) {
					m_effectors.RemoveAt (i);
					i--;
				}
			}
		}
	}
}

