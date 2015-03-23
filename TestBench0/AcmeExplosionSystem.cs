// (C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{

	public class AcmeExplosionRenderer : SSInstancedMeshRenderer
	{
		public AcmeExplosionRenderer(int particleCapacity=100)
			: base(new AcmeExplosionSystem(particleCapacity),
				   AcmeExplosionSystem.GetDefaultTexture(),
				   SSTexturedQuad.DoubleFaceInstance)
		{
			Billboarding = SSInstancedMeshRenderer.BillboardingType.None;
			AlphaBlendingEnabled = true;
			DepthRead = true;
			DepthWrite = false;
			SimulateOnUpdate = true;
			Name = "acme expolsion renderer";
		}

		public void ShowExplosion(Vector3 position, float intensity)
		{
			AcmeExplosionSystem aes = ParticleSystem as AcmeExplosionSystem;
			aes.ShowExplosion (position, intensity);
		}

		/// <summary>
		/// An explosion system based on a a gamedev.net article
		/// http://www.gamedev.net/page/resources/_/creative/visual-arts/make-a-particle-explosion-effect-r2701
		/// </summary>
		public class AcmeExplosionSystem : SSParticleSystem
		{
			public static SSTexture GetDefaultTexture()
			{
				return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7.png");
				//return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7_debug.png");
			}

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

			public static readonly RectangleF[] c_flyingSparksSpritesDefault = {
				new RectangleF(0.75f, 0.85f, 0.25f, 0.05f)
			};

			public static readonly RectangleF[] c_roundSparksSpritesDefault = {
				new RectangleF(0.5f, 0.75f, 0.25f, 0.25f)
			};

			public static readonly RectangleF[] c_debrisSpritesDefault = {
				new RectangleF(0.5f, 0.5f, 0.083333f, 0.083333f),
				new RectangleF(0.583333f, 0.5f, 0.083333f, 0.083333f),
				new RectangleF(0.66667f, 0.5f, 0.083333f, 0.083333f),

				new RectangleF(0.5f, 0.583333f, 0.083333f, 0.083333f),
				new RectangleF(0.583333f, 0.583333f, 0.083333f, 0.083333f),
				new RectangleF(0.66667f, 0.583333f, 0.083333f, 0.083333f),

				new RectangleF(0.5f, 0.66667f, 0.083333f, 0.083333f),
				new RectangleF(0.583333f, 0.66667f, 0.083333f, 0.083333f),
				new RectangleF(0.66667f, 0.66667f, 0.083333f, 0.083333f),
			};

			public static readonly RectangleF[] c_shockwaveSpritesDefault = {
				new RectangleF (0.75f, 0.5f, 0.25f, 0.25f)
			};

			// TODO debris sprites
			// TODO shockwave sprites
			// TODO round sparks sprites
			// TODO flying sparks sprites
			#endregion

			#region effects' colors
			//public Color4 FlameColor = Color4.Orange;
			public Color4 FlameColor = Color4.DarkOrange;
			public Color4 FlashColor = Color4.Yellow;
			public Color4 FlyingSparksColor = Color4.DarkGoldenrod;
			public Color4 SmokeTrailsColor = Color4.Orange;
			public Color4 RoundSparksColor = Color4.OrangeRed;
			public Color4 DebrisColorStart = Color4.Orange;
			public Color4 DebrisColorEnd = Color4.Silver;
			public Color4 ShockWaveColor = Color4.Orange;
			#endregion

			#region timing settings
			//public float TimeScale = 0.3f;
			public float TimeScale = 1f;
			public float FlameSmokeDuration = 2.5f;
			public float FlashDuration = 0.5f;
			public float FlyingSparksDuration = 2.5f;
			public float SmokeTrailsDuration = 1.5f;
			public float RoundSparksDuration = 2.5f;
			public float DebrisDuration = 4f;
			public float ShockwaveDuration = 2f;
			#endregion

			protected enum ComponentMask : ushort { 
				FlameSmoke = 0x1, 
				Flash = 0x2,
				FlyingSparks = 0x4, 
				SmokeTrails = 0x8,
				RoundSparks = 0x10,
				Debris = 0x20,
				Shockwave = 0x40,
			};

			protected readonly SSRadialEmitter m_flameSmokeEmitter;

			protected readonly ParticlesSphereGenerator m_flashSphereGen;
			protected readonly SSParticlesFieldEmitter m_flashEmitter;

			protected readonly SSRadialEmitter m_flyingSparksEmitter;

			protected readonly SSRadialEmitter m_smokeTrailsEmitter;
			protected readonly SSComponentScaleKeyframeEffector m_smokeTrailsScaleEffector;

			protected readonly SSRadialEmitter m_roundSparksEmitter;

			protected readonly SSRadialEmitter m_debrisEmitter;

			protected readonly ShockwaveEmitter m_shockwaveEmitter;

			protected readonly RadialBillboardOrientator m_radialOrientator;

			public AcmeExplosionSystem (
				int particleCapacity, 
				RectangleF[] flameSmokeSprites = null,
				RectangleF[] flashSprites = null,
				RectangleF[] flyingSparksSprites = null,
				RectangleF[] smokeTrailSprites = null,
				RectangleF[] roundSparksSprites = null,
				RectangleF[] debrisSprites = null,
				RectangleF[] shockwaveSprites = null
			)
				: base(particleCapacity)
			{
				{
					// flame/smoke
					{
						m_flameSmokeEmitter = new SSRadialEmitter ();
						m_flameSmokeEmitter.SpriteRectangles = (flameSmokeSprites != null ? flameSmokeSprites : c_flameSmokeSpritesDefault);
						m_flameSmokeEmitter.ParticlesPerEmission = 2;
						//m_flameSmokeEmitter.EmissionIntervalMin = 0f;
						//m_flameSmokeEmitter.EmissionIntervalMax = 0.1f * FlameSmokeDuration;
						m_flameSmokeEmitter.EmissionInterval = 0.03f * FlameSmokeDuration;
						m_flameSmokeEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						m_flameSmokeEmitter.Life = FlameSmokeDuration;
						m_flameSmokeEmitter.OrientationMin = new Vector3(0f, 0f, 0f);
						m_flameSmokeEmitter.OrientationMax = new Vector3(0f, 0f, 2f*(float)Math.PI);
						m_flameSmokeEmitter.BillboardXY = true;
						m_flameSmokeEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.5f);
						m_flameSmokeEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.5f);
						m_flameSmokeEmitter.RadiusOffsetMin = 0f;
						m_flameSmokeEmitter.RadiusOffsetMax = 0.5f;
						AddEmitter (m_flameSmokeEmitter);

						var flamesSmokeColorEffector = new SSColorKeyframesEffector ();
						flamesSmokeColorEffector.ColorMask = FlameColor;
						flamesSmokeColorEffector.ParticleLifetime = FlameSmokeDuration;
						flamesSmokeColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
						flamesSmokeColorEffector.Keyframes.Add (0.4f*FlameSmokeDuration, new Color4 (0f, 0f, 0f, 0.5f));
						flamesSmokeColorEffector.Keyframes.Add (FlameSmokeDuration, new Color4 (0f, 0f, 0f, 0f));
						AddEffector (flamesSmokeColorEffector);

						var flameSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
						flameSmokeScaleEffector.ParticleLifetime = FlameSmokeDuration;
						flameSmokeScaleEffector.Keyframes.Add (0f, 0.1f);
						flameSmokeScaleEffector.Keyframes.Add (0.25f * FlameSmokeDuration, 1f);
						flameSmokeScaleEffector.Keyframes.Add (FlameSmokeDuration, 1.2f);
						AddEffector (flameSmokeScaleEffector);

						m_flameSmokeEmitter.EffectorMask 
							= flameSmokeScaleEffector.EffectorMask 
							= flamesSmokeColorEffector.EffectorMask
							= (ushort)ComponentMask.FlameSmoke;
					}

					// flash
					{
						m_flashSphereGen = new ParticlesSphereGenerator ();
						m_flashEmitter = new SSParticlesFieldEmitter (m_flashSphereGen);
						m_flashEmitter.SpriteRectangles = (flashSprites != null ? flashSprites : c_flashSpritesDefault);
						m_flashEmitter.ParticlesPerEmissionMin = 1;
						m_flashEmitter.ParticlesPerEmissionMax = 2;
						m_flashEmitter.EmissionIntervalMin = 0f;
						m_flashEmitter.EmissionIntervalMax = 0.2f * FlashDuration;
						m_flashEmitter.Life = FlashDuration;
						m_flashEmitter.Velocity = Vector3.Zero;
						m_flashEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
						m_flashEmitter.OrientationMax = new Vector3 (0f, 0f, 2f*(float)Math.PI);
						m_flashEmitter.BillboardXY = true;
						m_flashEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						AddEmitter (m_flashEmitter);

						var flashColorEffector = new SSColorKeyframesEffector ();
						flashColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
						flashColorEffector.Keyframes.Add (FlashDuration, new Color4 (1f, 1f, 1f, 0f));
						flashColorEffector.ColorMask = FlashColor;
						flashColorEffector.ParticleLifetime = FlashDuration;
						AddEffector (flashColorEffector);

						var flashScaleEffector = new SSMasterScaleKeyframesEffector ();
						flashScaleEffector.ParticleLifetime = FlashDuration;
						flashScaleEffector.Keyframes.Add (0f, 1f);
						flashScaleEffector.Keyframes.Add (FlashDuration, 1.5f);
						AddEffector (flashScaleEffector);

						m_flashEmitter.EffectorMask = flashColorEffector.EffectorMask 
							= flashScaleEffector.EffectorMask 
							= (ushort)ComponentMask.Flash;
					}

					// flying sparks
					{
						m_flyingSparksEmitter = new SSRadialEmitter ();
						m_flyingSparksEmitter.SpriteRectangles = (flyingSparksSprites != null ? flyingSparksSprites 
																							: c_flyingSparksSpritesDefault);
						//m_flyingSparksEmitter.SpriteRectangles = c_flameSmokeSpritesDefault;
						m_flyingSparksEmitter.EmissionIntervalMin = 0f;
						m_flyingSparksEmitter.EmissionIntervalMax = 0.1f * FlyingSparksDuration;
						m_flyingSparksEmitter.Life = FlyingSparksDuration;
						m_flyingSparksEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						m_flyingSparksEmitter.ComponentScale = new Vector3 (5f, 1f, 1f);
						m_flyingSparksEmitter.Color = FlyingSparksColor;
						//m_flyingSparksEmitter.Phi = (float)Math.PI/4f;
						//m_flyingSparksEmitter.Phi = 0;
						//m_flyingSparksEmitter.Theta = 0;
						AddEmitter (m_flyingSparksEmitter);

						var flyingSparksColorEffector = new SSColorKeyframesEffector ();
						flyingSparksColorEffector.ColorMask = FlashColor;
						flyingSparksColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
						flyingSparksColorEffector.Keyframes.Add (FlyingSparksDuration, new Color4 (1f, 1f, 1f, 0f));
						flyingSparksColorEffector.ParticleLifetime = FlyingSparksDuration;
						AddEffector (flyingSparksColorEffector);

						m_flyingSparksEmitter.EffectorMask 
							= flyingSparksColorEffector.EffectorMask
							= (ushort)ComponentMask.FlyingSparks;
					}

					// smoke trails
					{
						m_smokeTrailsEmitter = new SSRadialEmitter();
						m_smokeTrailsEmitter.RadiusOffset = 3f;
						m_smokeTrailsEmitter.SpriteRectangles = (smokeTrailSprites == null) ? c_smokeTrailsSpritesDefault 
																							: smokeTrailSprites;
						m_smokeTrailsEmitter.ParticlesPerEmission = 16;
						m_smokeTrailsEmitter.EmissionIntervalMin = 0f;
						m_smokeTrailsEmitter.EmissionIntervalMax = 0.1f * SmokeTrailsDuration;
						m_smokeTrailsEmitter.Life = SmokeTrailsDuration;
						m_smokeTrailsEmitter.TotalEmissionsLeft = 0; // control this in ShowExplosion()
						m_smokeTrailsEmitter.Color = SmokeTrailsColor;
						AddEmitter(m_smokeTrailsEmitter);

						var smokeTrailsColorEffector = new SSColorKeyframesEffector();
						smokeTrailsColorEffector.ParticleLifetime = SmokeTrailsDuration;
						smokeTrailsColorEffector.ColorMask = SmokeTrailsColor;
						smokeTrailsColorEffector.Keyframes.Add(0f, new Color4(1f, 1f, 1f, 1f));
						smokeTrailsColorEffector.Keyframes.Add(SmokeTrailsDuration, new Color4(0.3f, 0.3f, 0.3f, 0f));
						AddEffector(smokeTrailsColorEffector);

						m_smokeTrailsScaleEffector = new SSComponentScaleKeyframeEffector();
						m_smokeTrailsScaleEffector.ParticleLifetime = SmokeTrailsDuration;
						m_smokeTrailsScaleEffector.BaseOffset = new Vector3(1f, 1f, 1f);
						m_smokeTrailsScaleEffector.Keyframes.Add(0f, new Vector3(0f));
						m_smokeTrailsScaleEffector.Keyframes.Add(0.5f*SmokeTrailsDuration, new Vector3(12f, 1.5f, 0f));
						m_smokeTrailsScaleEffector.Keyframes.Add(SmokeTrailsDuration, new Vector3(7f, 2f, 0f));
						AddEffector(m_smokeTrailsScaleEffector);

						m_smokeTrailsEmitter.EffectorMask
							= smokeTrailsColorEffector.EffectorMask
							= m_smokeTrailsScaleEffector.EffectorMask
							= (ushort)ComponentMask.SmokeTrails;
					}

					// round sparks
					{
						m_roundSparksEmitter = new SSRadialEmitter ();
						m_roundSparksEmitter.SpriteRectangles = (roundSparksSprites != null ? roundSparksSprites : c_roundSparksSpritesDefault);
						m_roundSparksEmitter.ParticlesPerEmission = 6;
						m_roundSparksEmitter.EmissionIntervalMin = 0f;
						m_roundSparksEmitter.EmissionIntervalMax = 0.05f * RoundSparksDuration;
						m_roundSparksEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						m_roundSparksEmitter.Life = RoundSparksDuration;
						m_roundSparksEmitter.BillboardXY = true;
						m_roundSparksEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
						m_roundSparksEmitter.OrientationMax = new Vector3 (0f, 0f, 2f*(float)Math.PI);
						m_roundSparksEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.25f);
						m_roundSparksEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.25f);
						m_roundSparksEmitter.RadiusOffsetMin = 0f;
						m_roundSparksEmitter.RadiusOffsetMax = 1f;
						AddEmitter (m_roundSparksEmitter);

						var roundSparksColorEffector = new SSColorKeyframesEffector ();
						roundSparksColorEffector.ParticleLifetime = RoundSparksDuration;
						roundSparksColorEffector.ColorMask = RoundSparksColor;
						//roundSparksColorEffector.Keyframes.Add (0f, new Color4 (1f, 1, 1f, 1f));
						roundSparksColorEffector.Keyframes.Add (0.1f*RoundSparksDuration, new Color4 (1f, 1f, 1f, 1f));
						roundSparksColorEffector.Keyframes.Add (RoundSparksDuration, new Color4 (1f, 1f, 1f, 0f));
						AddEffector (roundSparksColorEffector);

						var roundSparksScaleEffector = new SSMasterScaleKeyframesEffector ();
						roundSparksScaleEffector.ParticleLifetime = RoundSparksDuration;
						roundSparksScaleEffector.Keyframes.Add (0f, 1f);
						roundSparksScaleEffector.Keyframes.Add (0.25f * RoundSparksDuration, 3f);
						roundSparksScaleEffector.Keyframes.Add (RoundSparksDuration, 6f);
						AddEffector (roundSparksScaleEffector);

						m_roundSparksEmitter.EffectorMask 
							= roundSparksScaleEffector.EffectorMask 
							= roundSparksColorEffector.EffectorMask
							= (ushort)ComponentMask.RoundSparks;
					}

					// debris
					{
						m_debrisEmitter = new SSRadialEmitter ();
						m_debrisEmitter.SpriteRectangles = (debrisSprites != null ? debrisSprites : c_debrisSpritesDefault);
						m_debrisEmitter.ParticlesPerEmissionMin = 7;
						m_debrisEmitter.ParticlesPerEmissionMax = 10;
						m_debrisEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						m_debrisEmitter.Life = DebrisDuration;
						m_debrisEmitter.OrientationMin = new Vector3(0f, 0f, 0f);
						m_debrisEmitter.OrientationMax = new Vector3(0f, 0f, 2f*(float)Math.PI);
						m_debrisEmitter.BillboardXY = true;
						m_debrisEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.5f);
						m_debrisEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.5f);
						m_debrisEmitter.RadiusOffsetMin = 0f;
						m_debrisEmitter.RadiusOffsetMax = 1f;
						AddEmitter (m_debrisEmitter);

						var debrisColorFinal = new Color4(DebrisColorEnd.R, DebrisColorEnd.G, DebrisColorEnd.B, 0f);
						var debrisColorEffector = new SSColorKeyframesEffector ();
						debrisColorEffector.ParticleLifetime = DebrisDuration;
						debrisColorEffector.Keyframes.Add (0f, DebrisColorStart);
						debrisColorEffector.Keyframes.Add (0.3f*DebrisDuration, DebrisColorEnd);
						debrisColorEffector.Keyframes.Add (DebrisDuration, debrisColorFinal);
						AddEffector (debrisColorEffector);

						m_debrisEmitter.EffectorMask 
							= debrisColorEffector.EffectorMask
							= (ushort)ComponentMask.Debris;
					}

					// shockwave
					{
						m_shockwaveEmitter = new ShockwaveEmitter();
						m_shockwaveEmitter.SpriteRectangles = (shockwaveSprites != null ? shockwaveSprites : c_shockwaveSpritesDefault);
						m_shockwaveEmitter.ParticlesPerEmission = 1;
						m_shockwaveEmitter.TotalEmissionsLeft = 0;   // Control this in ShowExplosion()
						m_shockwaveEmitter.Life = ShockwaveDuration;
						m_shockwaveEmitter.Velocity = Vector3.Zero;
						AddEmitter(m_shockwaveEmitter);

						var shockwaveScaleEffector = new SSMasterScaleKeyframesEffector();
						shockwaveScaleEffector.ParticleLifetime = ShockwaveDuration;
						shockwaveScaleEffector.Keyframes.Add(0f, 0f);
						shockwaveScaleEffector.Keyframes.Add(ShockwaveDuration, 7f);
						AddEffector(shockwaveScaleEffector);

						var shockwaveColorEffector = new SSColorKeyframesEffector();
						shockwaveColorEffector.ParticleLifetime = ShockwaveDuration;
						shockwaveColorEffector.ColorMask = ShockWaveColor;
						shockwaveColorEffector.Keyframes.Add(0f, new Color4(1f, 1f, 1f, 1f));
						shockwaveColorEffector.Keyframes.Add(ShockwaveDuration, new Color4(1f, 1f, 1f, 0f));
						AddEffector(shockwaveColorEffector);

						m_shockwaveEmitter.EffectorMask
							= shockwaveScaleEffector.EffectorMask
							= shockwaveColorEffector.EffectorMask
							= (ushort)ComponentMask.Shockwave;
					}

					// shared
					{
						m_radialOrientator = new RadialBillboardOrientator();
						m_radialOrientator.EffectorMask = (ushort)ComponentMask.FlyingSparks 
													    | (ushort)ComponentMask.SmokeTrails;
						AddEffector (m_radialOrientator);
					}
				}
			}

			// TODO Reset/reconfigure

			public virtual void ShowExplosion(Vector3 position, float intensity)
			{
				// flame/smoke
				#if true
				m_flameSmokeEmitter.ComponentScale = new Vector3(intensity*3f, intensity*3f, 1f);
				m_flameSmokeEmitter.VelocityMagnitudeMin = 0.60f * intensity;
				m_flameSmokeEmitter.VelocityMagnitudeMax = 0.80f * intensity;
				m_flameSmokeEmitter.Center = position;
				m_flameSmokeEmitter.TotalEmissionsLeft = 5;
				#endif

				// flash
				#if true
				m_flashEmitter.ComponentScale = new Vector3(intensity*3f, intensity*3f, 1f);
				m_flashSphereGen.Center = position;
				m_flashSphereGen.Radius = 0.3f * intensity;
				m_flashEmitter.TotalEmissionsLeft = 2;
				#endif

				// flying sparks
				#if true
				m_flyingSparksEmitter.Center = position;
				m_flyingSparksEmitter.VelocityMagnitudeMin = intensity * 2f;
				m_flyingSparksEmitter.VelocityMagnitudeMax = intensity * 3f;
				m_flyingSparksEmitter.ParticlesPerEmission = (int)(5.0*Math.Log(intensity));
				m_flyingSparksEmitter.TotalEmissionsLeft = 1;
				#endif

				// smoke trails
				#if true
				m_smokeTrailsEmitter.Center = position;
				m_smokeTrailsEmitter.VelocityMagnitudeMin = intensity * 0.8f;
				m_smokeTrailsEmitter.VelocityMagnitudeMax = intensity * 1f;
				m_smokeTrailsEmitter.ParticlesPerEmission = (int)(5.0*Math.Log(intensity));
				m_smokeTrailsEmitter.TotalEmissionsLeft = 1;
				m_smokeTrailsEmitter.RadiusOffset = intensity;

				m_smokeTrailsScaleEffector.Amplification = new Vector3(0.3f*intensity, 0.15f*intensity, 0f);
				#endif

				// round sparks
				#if true
				m_roundSparksEmitter.ComponentScale = new Vector3(intensity, intensity, 1f);
				m_roundSparksEmitter.VelocityMagnitudeMin = 0.7f * intensity;
				m_roundSparksEmitter.VelocityMagnitudeMax = 1.2f * intensity;
				m_roundSparksEmitter.Center = position;
				m_roundSparksEmitter.TotalEmissionsLeft = 3;
				#endif

				// debris
				#if true
				//m_debrisEmitter.MasterScale = intensity / 2f;
				m_debrisEmitter.MasterScaleMin = 3f;
				m_debrisEmitter.MasterScaleMax = 0.4f*intensity;
				m_debrisEmitter.VelocityMagnitudeMin = 1f * intensity;
				m_debrisEmitter.VelocityMagnitudeMax = 3f * intensity;
				m_debrisEmitter.Center = position;
				m_debrisEmitter.ParticlesPerEmission = (int)(2.5*Math.Log(intensity));
				m_debrisEmitter.TotalEmissionsLeft = 1;
				#endif

				// shockwave
				#if true
				m_shockwaveEmitter.ComponentScale = new Vector3(intensity, intensity, 1f);
				m_shockwaveEmitter.Position = position;
				m_shockwaveEmitter.TotalEmissionsLeft = 1;
				#endif
			}

			public override void Simulate(float timeDelta)
			{
				timeDelta *= TimeScale;
				base.Simulate(timeDelta);
			}

			public override void UpdateCamera (ref Matrix4 modelView, ref Matrix4 projection)
			{
				m_radialOrientator.UpdateModelView (ref modelView);
				m_shockwaveEmitter.UpdateModelView (ref modelView);
			}
		}

		public class RadialBillboardOrientator : SSParticleEffector
		{
			protected float m_orientationX = 0f;

			/// <summary>
			/// Compute orientation around X once per frame to orient the sprites towards the viewer
			/// </summary>
			public void UpdateModelView(ref Matrix4 modelViewMatrix)
			{
				Quaternion quat = modelViewMatrix.ExtractRotation();
				// x-orient
				Vector3 test1 = new Vector3(0f, 1f, 0f);
				Vector3 test2 = Vector3.Transform(test1, quat);
				float dot = Vector3.Dot(test1, test2);
				float angle = (float)Math.Acos(dot);
				if (test2.Z < 0f) {
					angle = -angle;
				} 
				m_orientationX = angle;
			}

			protected override void effectParticle (SSParticle particle, float deltaT)
			{
				Vector3 dir = particle.Vel;

				// orient to look right
				float x = dir.X;
				float y = dir.Y;
				float z = dir.Z;
				float xy = dir.Xy.Length;
				float phi = (float)Math.Atan (z / xy);
				float theta = (float)Math.Atan2 (y, x);

				particle.Orientation.Y = phi;
				particle.Orientation.Z = -theta;
				particle.Orientation.X = m_orientationX;
			}
		}

		public class ShockwaveEmitter : SSFixedPositionEmitter
		{
			public void UpdateModelView(ref Matrix4 modelViewMatrix)
			{
				Quaternion quat = modelViewMatrix.ExtractRotation ().Inverted();
				Vector3 euler = OpenTKHelper.QuaternionToEuler (ref quat);
				Vector3 baseVec = new Vector3 (euler.X + 0.5f*(float)Math.PI, euler.Y, euler.Z); 
				OrientationMin = baseVec + new Vector3((float)Math.PI/8f, 0f, 0f);
				OrientationMax = baseVec + new Vector3((float)Math.PI*3f/8f, 2f*(float)Math.PI, 0f);
			}
		}
	}
}

