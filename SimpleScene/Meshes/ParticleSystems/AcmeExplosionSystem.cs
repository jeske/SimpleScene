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
			new RectangleF(0.5f,  0f,    0.25f, 0.25f),
			new RectangleF(0.75f, 0f,    0.25f, 0.25f),
			new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
			new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
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

		protected readonly float m_flashDuration;
		protected readonly SSFixedPositionEmitter m_flashEmitter;
		protected readonly SSColorKeyframesEffector m_flashColorEffector;
		protected readonly SSMasterScaleKeyframesEffector m_flashScaleEffector;

		public AcmeExplosionSystem (int capacity, float duration=1f, RectangleF[] flashSprites = null)
			: base(capacity)
		{
			{
				// flash
				m_flashDuration = duration * 0.2f;
				m_flashEmitter = new SSFixedPositionEmitter ();
				m_flashEmitter.SpriteRectangles = (flashSprites != null ? flashSprites : c_flashSpritesDefault);
				m_flashEmitter.ParticlesPerEmission = 1;
				m_flashEmitter.Velocity = Vector3.Zero;
				m_flashEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
				m_flashEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
				m_flashEmitter.Life = duration;
				//AddEmitter (m_flashEmitter);

				m_flashColorEffector = new SSColorKeyframesEffector ();
				// keyframes configured during Explode()
				AddEffector (m_flashColorEffector);

				m_flashScaleEffector = new SSMasterScaleKeyframesEffector ();
				m_flashScaleEffector.Keyframes.Add (0f, 1f);
				m_flashScaleEffector.Keyframes.Add (m_flashDuration, 1.5f);

				AddEffector (m_flashScaleEffector);

				m_flashEmitter.EffectorMask = m_flashColorEffector.EffectorMask = m_flashScaleEffector.EffectorMask 
					= (byte)ComponentMask.Flash;
			}
		}

		public void ShowExplosion(Vector3 position, float size)
		{
			m_flashScaleEffector.Reset ();
			m_flashScaleEffector.Amplification = size;

			m_flashColorEffector.Reset ();
			m_flashColorEffector.Keyframes.Clear ();
			m_flashColorEffector.Keyframes.Add (0f, new Color4(1f, 1f, 1f, 1f));
			m_flashColorEffector.Keyframes.Add (m_flashDuration, new Color4 (1f, 1f, 1f, 0f));

			m_flashEmitter.Position = position;
			m_flashEmitter.EmitParticles (storeNewParticle);
		}
	}
}

