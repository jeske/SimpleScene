// Copyright(C) David W. Jeske, 2013
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
		public AcmeExplosionRenderer(int capacity)
			: base(new AcmeExplosionSystem(capacity),
				   AcmeExplosionSystem.GetDefaultTexture(),
				   SSTexturedQuad.Instance)
		{
			Billboarding = SSInstancedMeshRenderer.BillboardingType.None;
			//Billboarding = SSInstancedMeshRenderer.BillboardingType.Instanced;
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
				//return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7.png");
				return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7_debug.png");
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
			#endregion

			#region timing settings
			public float TimeScale = 0.3f;
			public float FlameSmokeDuration = 2.5f;
			public float FlashDuration = 0.5f;
			public float FlyingSparksDuration = 2.5f;
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
			protected readonly RadialOrientator m_flyingSparksOrientator;

			//protected readonly SparksOrientator m_sharedBillboarder;

			public AcmeExplosionSystem (
				int capacity, 
				RectangleF[] flameSmokeSprites = null,
				RectangleF[] flashSprites = null,
				RectangleF[] flyingSparksSprites = null
			)
				: base(capacity)
			{
				{
					// flame/smoke
					{
						float flameSmokeDuration = FlameSmokeDuration;

						m_flameSmokeEmitter = new SSRadialEmitter ();
						m_flameSmokeEmitter.SpriteRectangles = (flameSmokeSprites != null ? flameSmokeSprites : c_flameSmokeSpritesDefault);
						m_flameSmokeEmitter.ParticlesPerEmissionMin = 1;
						m_flameSmokeEmitter.ParticlesPerEmissionMax = 3;
						m_flameSmokeEmitter.EmissionIntervalMin = 0f;
						m_flameSmokeEmitter.EmissionIntervalMax = 0.1f * flameSmokeDuration;
						m_flameSmokeEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						m_flameSmokeEmitter.Life = flameSmokeDuration;
						//m_flameSmokeEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
						//m_flameSmokeEmitter.OrientationMax = new Vector3 (0f, 0f, (float)Math.PI);
						//m_flameSmokeEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.5f);
						//m_flameSmokeEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.5f);
						m_flameSmokeEmitter.RMin = 0f;
						m_flameSmokeEmitter.RMax = 1f;
						AddEmitter (m_flameSmokeEmitter);

						var flamesSmokeColorEffector = new SSColorKeyframesEffector ();
						flamesSmokeColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
						flamesSmokeColorEffector.Keyframes.Add (0.5f*flameSmokeDuration, new Color4 (0f, 0f, 0f, 0.5f));
						flamesSmokeColorEffector.Keyframes.Add (flameSmokeDuration, new Color4 (0f, 0f, 0f, 0f));
						flamesSmokeColorEffector.ColorMask = FlameColor;
						flamesSmokeColorEffector.ParticleLifetime = flameSmokeDuration;
						AddEffector (flamesSmokeColorEffector);

						var flameSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
						flameSmokeScaleEffector.Keyframes.Add (0f, 0.1f);
						flameSmokeScaleEffector.Keyframes.Add (0.5f * flameSmokeDuration, 1f);
						flameSmokeScaleEffector.Keyframes.Add (flameSmokeDuration, 1.2f);
						flameSmokeScaleEffector.ParticleLifetime = flameSmokeDuration;
						AddEffector (flameSmokeScaleEffector);

						m_flameSmokeEmitter.EffectorMask = flameSmokeScaleEffector.EffectorMask = flamesSmokeColorEffector.EffectorMask
							= (ushort)ComponentMask.FlameSmoke;
					}

					// flash
					{
						float flashDuration = FlashDuration;

						m_flashSphereGen = new ParticlesSphereGenerator ();
						m_flashEmitter = new SSParticlesFieldEmitter (m_flashSphereGen);
						m_flashEmitter.SpriteRectangles = (flashSprites != null ? flashSprites : c_flashSpritesDefault);
						m_flashEmitter.ParticlesPerEmissionMin = 1;
						m_flashEmitter.ParticlesPerEmissionMax = 2;
						m_flashEmitter.EmissionIntervalMin = 0f;
						m_flashEmitter.EmissionIntervalMax = 0.2f * flashDuration;
						m_flashEmitter.Life = flashDuration;
						m_flashEmitter.Velocity = Vector3.Zero;
						//m_flashEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
						//m_flashEmitter.OrientationMax = new Vector3 (0f, 0f, 2f*(float)Math.PI);
						m_flashEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						AddEmitter (m_flashEmitter);

						var flashColorEffector = new SSColorKeyframesEffector ();
						flashColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
						flashColorEffector.Keyframes.Add (flashDuration, new Color4 (1f, 1f, 1f, 0f));
						flashColorEffector.ColorMask = FlashColor;
						flashColorEffector.ParticleLifetime = flashDuration;
						AddEffector (flashColorEffector);

						var flashScaleEffector = new SSMasterScaleKeyframesEffector ();
						flashScaleEffector.ParticleLifetime = flashDuration;
						flashScaleEffector.Keyframes.Add (0f, 1f);
						flashScaleEffector.Keyframes.Add (flashDuration, 1.5f);
						AddEffector (flashScaleEffector);

						m_flashEmitter.EffectorMask = flashColorEffector.EffectorMask = flashScaleEffector.EffectorMask 
							= (ushort)ComponentMask.Flash;
					}

					// flying sparks
					{
						float flyingSparksDuration = FlyingSparksDuration;

						m_flyingSparksEmitter = new SSRadialEmitter ();
						m_flyingSparksEmitter.SpriteRectangles = (flyingSparksSprites != null ? flyingSparksSprites : c_flyingSparksSpritesDefault);
						//m_flyingSparksEmitter.SpriteRectangles = c_flameSmokeSpritesDefault;
						m_flyingSparksEmitter.ParticlesPerEmission = 16;
						m_flyingSparksEmitter.EmissionIntervalMin = 0f;
						m_flyingSparksEmitter.EmissionIntervalMax = 0.1f * flyingSparksDuration;
						m_flyingSparksEmitter.Life = flyingSparksDuration;
						m_flyingSparksEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
						m_flyingSparksEmitter.ComponentScale = new Vector3 (5f, 1f, 1f);
						m_flyingSparksEmitter.OrientAwayFromCenter = true;
						m_flyingSparksEmitter.Color = FlyingSparksColor;
						//m_flyingSparksEmitter.Phi = (float)Math.PI/4f;
						AddEmitter (m_flyingSparksEmitter);

						var flyingSparksColorEffector = new SSColorKeyframesEffector ();
						flyingSparksColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
						flyingSparksColorEffector.Keyframes.Add (flyingSparksDuration, new Color4 (1f, 1f, 1f, 0f));
						flyingSparksColorEffector.ColorMask = FlashColor;
						flyingSparksColorEffector.ParticleLifetime = flyingSparksDuration;
						AddEffector (flyingSparksColorEffector);

						m_flyingSparksOrientator = new RadialOrientator();
						AddEffector (m_flyingSparksOrientator);

						m_flyingSparksEmitter.EffectorMask 
							= flyingSparksColorEffector.EffectorMask
							= m_flyingSparksOrientator.EffectorMask 
							= (ushort)ComponentMask.FlyingSparks;
					}

					//m_sharedBillboarder = new SSBillboardEffector();
					//m_sharedBillboarder.EffectorMask = (ushort)ComponentMask.FlameSmoke | (ushort)ComponentMask.Flash;
					//AddEffector(m_sharedBillboarder);
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
				m_flashEmitter.ComponentScale = new Vector3(intensity, intensity, 1f);
				m_flashSphereGen.Center = position;
				m_flashSphereGen.Radius = 0.3f * intensity;
				m_flashEmitter.TotalEmissionsLeft = 2;
				#endif

				// flying sparks
				#if true
				m_flyingSparksEmitter.Center = position;
				m_flyingSparksEmitter.RadiusOffset = 0f;
				m_flyingSparksEmitter.VelocityMagnitudeMin = intensity * 5f;
				m_flyingSparksEmitter.VelocityMagnitudeMax = intensity * 5f;
				m_flyingSparksEmitter.TotalEmissionsLeft = 1;
				//m_flyingSparksEmitter.Color = Color4Helper.RandomDebugColor();

				m_flyingSparksOrientator.center = position;
				#endif
			}

			public override void Simulate(float timeDelta)
			{
				timeDelta *= TimeScale;
				base.Simulate(timeDelta);
			}

			public override void UpdateCamera (ref Matrix4 modelView, ref Matrix4 projection)
			{
				m_flyingSparksOrientator.modelViewMatrix = modelView;
				//m_sharedBillboarder.modelViewMatrix = modelView;
			}
		}

		public class RadialOrientator : SSParticleEffector
		{
			public Matrix4 modelViewMatrix = Matrix4.Identity;
			public Vector3 center = Vector3.Zero;

			protected override void effectParticle (SSParticle particle, float deltaT)
			{
				// undo instanced billboarding
				// TODO redundant; optimize
				//Quaternion quat = modelViewMatrix.ExtractRotation();
				//Vector3 euler = OpenTKHelper.QuaternionToEuler (ref quat);
				//particle.Orientation.X = euler.X;
				//particle.Orientation.Y = euler.Y;
				//particle.Orientation.Z = euler.Z;

				//Vector3 dir = Vector3.Transform (particle.Vel, modelViewMatrix);
				Vector3 dir = particle.Vel;

				// orient to look right
				float x = dir.X;
				float y = dir.Y;
				float z = dir.Z;
				float xy = dir.Xy.Length;
				float phi = (float)Math.Atan (z / xy);
				float theta = (float)Math.Atan2 (y, x);

				//particle.Orientation.Y += phi;
				//particle.Orientation.Z -= theta;
				particle.Orientation.X = 0f;
				particle.Orientation.Y = phi;
				particle.Orientation.Z = -theta;
				//particle.Orientation.Y = phi;
				//particle.Orientation.X = 0f;
			}
		} 
	}
}

