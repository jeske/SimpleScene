using System;
using System.Drawing;

namespace SimpleScene
{
	public class AcmeExplosionSystem : SSParticleSystem
	{
		enum ExplosionComponent : byte { 
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
		public RectangleF[] FlamesSpriteSprites = {
			new RectangleF(0f,    0f,    0.25f, 0.25f),
			new RectangleF(0f,    0.25f, 0.25f, 0.25f),
			new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
			new RectangleF(0.25f, 0f,    0.25f, 0.25f),
		};

		/// <summary>
		/// Override to match your flash effect sprites (one RectangleF per available sprite)
		/// </summary>
		public RectangleF[] FlashSprites = {
			new RectangleF(0.5f,  0.5f,  0.25f, 0.25f),
			new RectangleF(0.5f,  0.75f, 0.25f, 0.25f),
			new RectangleF(0.75f, 0.75f, 0.25f, 0.25f),
			new RectangleF(0.75f, 0.5f,  0.25f, 0.25f),
		};

		/// <summary>
		/// Override to match your smoke trails sprites (one RectangleF per available sprite)
		/// </summary>
		public RectangleF[] SmokeTrailsSprites = {
			new RectangleF(0f, 0.5f,   0.5f, 0.125f),
			new RectangleF(0f, 0.625f, 0.5f, 0.125f),
			new RectangleF(0f, 0.75f,  0.5f, 0.125f),
		};

		// TODO debris sprites
		// TODO shockwave sprites
		// TODO round sparks sprites
		// TODO flying sparks sprites
		#endregion

		public AcmeExplosionSystem (int capacity)
			: base(capacity)
		{
		}

		public override void Reset()
		{
			base.Reset ();
		}
	}
}

