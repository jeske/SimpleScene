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
		new public AcmeExplosionSystem ParticleSystem {
			get { return base.ParticleSystem as AcmeExplosionSystem; }
		}

		public AcmeExplosionRenderer(int particleCapacity = 100, SSTexture texture = null)
			: base(new AcmeExplosionSystem(particleCapacity),
				SSTexturedQuad.DoubleFaceInstance,   
				c_defaultUsageHint
			 )
		{
			renderState.castsShadow = false;
			renderState.receivesShadows = false;
			GlobalBillboarding = false;
			AlphaBlendingEnabled = true;
			DepthRead = true;
			DepthWrite = false;
			SimulateOnUpdate = true;
			Name = "acme expolsion renderer";

			base.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
			base.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
			base.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
			base.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
			base.ShininessMatColor = 0f;

			base.AmbientTexture 
				= (texture != null) ? texture : AcmeExplosionSystem.GetDefaultTexture();
		}

		public void ShowExplosion(Vector3 position, float intensity)
		{
			ParticleSystem.ShowExplosion (position, intensity);
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

			#region flame smoke parameters
			public RectangleF[] FlameSmokeSprites {
				get { return m_flameSmokeSprites; }
				set { m_flameSmokeSprites = value; configureFlameSmoke (); }
			}
			public Color4 FlameColor
			{
				get { return m_flameColor; }
				set { m_flameColor = value; configureFlameSmoke (); }
			}
			public float FlameSmokeDuration
			{
				get { return m_flameSmokeDuration; }
				set { m_flameSmokeDuration = value; configureFlameSmoke (); }
			}
			#endregion

			#region flash parameters
			public RectangleF[] FlashSprites {
				get { return m_flashSprites; }
				set { m_flashSprites = value; configureFlash (); }
			}
			public Color4 FlashColor {
				get { return m_flashColor; }
				set { m_flashColor = value; configureFlash (); }
			}
			public float FlashDuration {
				get { return m_flashDuration; }
				set { m_flashDuration = value; configureFlash (); }
			}
			#endregion

			#region flying sparks parameters
			public RectangleF[] FlyingSparksSprites {
				get { return m_flyingSparksSprites; }
				set { m_flyingSparksSprites = value; configureFlyingSparks (); }
			}
			public Color4 FlyingSparksColor {
				get { return m_flyingSparksColor; }
				set { m_flyingSparksColor = value; configureFlyingSparks (); }
			}
			public float FlyingSparksDuration {
				get { return m_flyingSparksDuration; }
				set { m_flyingSparksDuration = value; configureFlyingSparks (); }
			}
			#endregion

			#region smoke trails parameters
			public RectangleF[] SmokeTrailsSprites {
				get { return m_smokeTrailsSprites; }
				set { m_smokeTrailsSprites = value; configureSmokeTrails (); }
			}
			public Color4 SmokeTrailsColor {
				get { return m_smokeTrailsColor; }
				set { m_smokeTrailsColor = value; configureSmokeTrails (); }
			}
			public float SmokeTrailsDuration {
				get { return m_smokeTrailsDuration; }
				set { m_smokeTrailsDuration = value; configureSmokeTrails (); }
			}
			#endregion

			#region round sparks parameters
			public RectangleF[] RoundSparksSprites {
				get { return m_roundSparksSprites; }
				set { m_roundSparksSprites = value; configureRoundSparks (); }
			}
			public Color4 RoundSparksColor {
				get { return m_roundSparksColor; }
				set { m_roundSparksColor = value; configureRoundSparks (); }
			}
			public float RoundSparksDuration {
				get { return m_roundSparksDuration; }
				set { m_roundSparksDuration = value; configureRoundSparks (); }
			}
			#endregion

			#region debris parameters
			public RectangleF[] DebrisSprites {
				get { return m_debrisSprites; }
				set { m_debrisSprites = value; configureDebris (); }
			}
			public Color4 DebrisColorStart {
				get { return m_debrisColorStart; }
				set { m_debrisColorStart = value; configureDebris (); }
			}
			public Color4 DebrisColorEnd {
				get { return m_debrisColorEnd; }
				set { m_debrisColorEnd = value; configureDebris (); }
			}
			public float DebrisDuration {
				get { return m_debrisDuration; }
				set { m_debrisDuration = value; configureDebris (); }
			}
			#endregion

			#region shockwave parameters
			public RectangleF[] ShockwaveSprites {
				get { return m_shockwaveSprites; }
				set { m_shockwaveSprites = value; configureShockwave (); }
			}
			public Color4 ShockwaveColor {
				get { return m_shockwaveColor; }
				set { m_shockwaveColor = value; configureShockwave (); }
			}
			public float ShockwaveDuration {
				get { return m_shockwaveDuration; }
				set { m_shockwaveDuration = value; configureShockwave (); }
			}
			#endregion

			/// <summary>
			/// Used to maatch emitted particles with effectors
			/// </summary>
			protected enum ComponentMask : ushort { 
				FlameSmoke = 0x1, 
				Flash = 0x2,
				FlyingSparks = 0x4, 
				SmokeTrails = 0x8,
				RoundSparks = 0x10,
				Debris = 0x20,
				Shockwave = 0x40,
			};

			#region sprite rectangles
			// default to fig7.png asset by Mike McClelland

			/// <summary>
			/// Default locations of flame sprites in fig7.png
			/// </summary>
			protected RectangleF[] m_flameSmokeSprites = {
				new RectangleF(0f,    0f,    0.25f, 0.25f),
				new RectangleF(0f,    0.25f, 0.25f, 0.25f),
				new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
				new RectangleF(0.25f, 0f,    0.25f, 0.25f),
			};

			/// <summary>
			/// Default locations of flash sprites in fig7.png
			/// </summary>
			protected RectangleF[] m_flashSprites = {
				new RectangleF(0.5f,  0f,    0.25f, 0.25f),
				new RectangleF(0.75f, 0f,    0.25f, 0.25f),
				new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
				new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
			};

			/// <summary>
			/// Default locations of smoke trails sprites in fig7.png
			/// </summary>
			protected RectangleF[] m_smokeTrailsSprites = {
				new RectangleF(0f, 0.5f,   0.5f, 0.125f),
				new RectangleF(0f, 0.625f, 0.5f, 0.125f),
				new RectangleF(0f, 0.75f,  0.5f, 0.125f),
			};

			protected RectangleF[] m_flyingSparksSprites = {
				new RectangleF(0.75f, 0.85f, 0.25f, 0.05f)
			};

			protected RectangleF[] m_roundSparksSprites = {
				new RectangleF(0.5f, 0.75f, 0.25f, 0.25f)
			};

			protected RectangleF[] m_debrisSprites = {
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

			protected RectangleF[] m_shockwaveSprites = {
				new RectangleF (0.75f, 0.5f, 0.25f, 0.25f)
			};
			#endregion

			#region effects' colors
			protected Color4 m_flameColor = Color4.DarkOrange;
			protected Color4 m_flashColor = Color4.Yellow;
			protected Color4 m_flyingSparksColor = Color4.DarkGoldenrod;
			protected Color4 m_smokeTrailsColor = Color4.Orange;
			protected Color4 m_roundSparksColor = Color4.OrangeRed;
			protected Color4 m_debrisColorStart = Color4.Orange;
			protected Color4 m_debrisColorEnd = Color4.Silver;
			protected Color4 m_shockwaveColor = Color4.Orange;
			#endregion

			#region timing settings
			protected float TimeScale = 1f;
			protected float m_flameSmokeDuration = 2.5f;
			protected float m_flashDuration = 0.5f;
			protected float m_flyingSparksDuration = 2.5f;
			protected float m_smokeTrailsDuration = 1.5f;
			protected float m_roundSparksDuration = 2.5f;
			protected float m_debrisDuration = 4f;
			protected float m_shockwaveDuration = 2f;
			#endregion

			#region emitters and effectors
			// flame/smoke
			protected readonly SSRadialEmitter m_flameSmokeEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector m_flamesSmokeColorEffector
				= new SSColorKeyframesEffector();
			protected readonly SSMasterScaleKeyframesEffector m_flameSmokeScaleEffector
				= new SSMasterScaleKeyframesEffector();
			// flash
			protected readonly SSParticlesFieldEmitter m_flashEmitter
				= new SSParticlesFieldEmitter(new ParticlesSphereGenerator());
			protected readonly SSColorKeyframesEffector m_flashColorEffector 
				= new SSColorKeyframesEffector ();
			protected readonly SSMasterScaleKeyframesEffector m_flashScaleEffector 
				= new SSMasterScaleKeyframesEffector ();
			// flying sparks
			protected readonly SSRadialEmitter m_flyingSparksEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector m_flyingSparksColorEffector 
				= new SSColorKeyframesEffector ();
			// smoke trails
			protected readonly SSRadialEmitter m_smokeTrailsEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector m_smokeTrailsColorEffector 
				= new SSColorKeyframesEffector();
			protected readonly SSComponentScaleKeyframeEffector m_smokeTrailsScaleEffector
				= new SSComponentScaleKeyframeEffector ();
			// round sparks
			protected readonly SSRadialEmitter m_roundSparksEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector m_roundSparksColorEffector 
				= new SSColorKeyframesEffector ();
			protected readonly SSMasterScaleKeyframesEffector m_roundSparksScaleEffector 
				= new SSMasterScaleKeyframesEffector ();
			// debris
			protected readonly SSRadialEmitter m_debrisEmitter
				= new SSRadialEmitter ();
			protected readonly SSColorKeyframesEffector m_debrisColorEffector 
				= new SSColorKeyframesEffector ();
			// shockwave
			protected readonly ShockwaveEmitter m_shockwaveEmitter
				= new ShockwaveEmitter();
			protected readonly SSMasterScaleKeyframesEffector m_shockwaveScaleEffector 
				= new SSMasterScaleKeyframesEffector();
			protected readonly SSColorKeyframesEffector m_shockwaveColorEffector 
				= new SSColorKeyframesEffector();
			// shared
			protected readonly RadialBillboardOrientator m_radialOrientator
				= new RadialBillboardOrientator();
			#endregion

			public AcmeExplosionSystem (int particleCapacity)
				: base(particleCapacity)
			{
				// flame/smoke
				{
					m_flameSmokeEmitter.EffectorMask 
						= m_flameSmokeScaleEffector.EffectorMask 
						= m_flamesSmokeColorEffector.EffectorMask
						= (ushort)ComponentMask.FlameSmoke;
					AddEmitter(m_flameSmokeEmitter);
					AddEffector(m_flamesSmokeColorEffector);
					AddEffector(m_flameSmokeScaleEffector);
					configureFlameSmoke();
				}
				// flash
				{
					m_flashEmitter.EffectorMask 
						= m_flashColorEffector.EffectorMask 
						= m_flashScaleEffector.EffectorMask 
						= (ushort)ComponentMask.Flash;
					AddEmitter (m_flashEmitter);
					AddEffector (m_flashColorEffector);
					AddEffector (m_flashScaleEffector);
					configureFlash();
				}
				// flying sparks
				{
					m_flyingSparksEmitter.EffectorMask 
						= m_flyingSparksColorEffector.EffectorMask
						= (ushort)ComponentMask.FlyingSparks;
					AddEmitter (m_flyingSparksEmitter);
					AddEffector (m_flyingSparksColorEffector);
					configureFlyingSparks();
				}
				// smoke trails
				{
					m_smokeTrailsEmitter.EffectorMask
						= m_smokeTrailsColorEffector.EffectorMask
						= m_smokeTrailsScaleEffector.EffectorMask
						= (ushort)ComponentMask.SmokeTrails;
					AddEmitter(m_smokeTrailsEmitter);
					AddEffector(m_smokeTrailsColorEffector);
					AddEffector(m_smokeTrailsScaleEffector);
					configureSmokeTrails();
				}
				// round sparks
				{
					m_roundSparksEmitter.EffectorMask 
						= m_roundSparksScaleEffector.EffectorMask 
						= m_roundSparksColorEffector.EffectorMask
						= (ushort)ComponentMask.RoundSparks;
					AddEmitter (m_roundSparksEmitter);
					AddEffector (m_roundSparksColorEffector);
					AddEffector (m_roundSparksScaleEffector);
					configureRoundSparks();
				}
				// debris
				{
					m_debrisEmitter.EffectorMask 
						= m_debrisColorEffector.EffectorMask
						= (ushort)ComponentMask.Debris;
					AddEmitter (m_debrisEmitter);
					AddEffector (m_debrisColorEffector);
					configureDebris();
				}
				// shockwave
				{
					m_shockwaveEmitter.EffectorMask
						= m_shockwaveScaleEffector.EffectorMask
						= m_shockwaveColorEffector.EffectorMask
						= (ushort)ComponentMask.Shockwave;
					AddEmitter(m_shockwaveEmitter);
					AddEffector(m_shockwaveScaleEffector);
					AddEffector(m_shockwaveColorEffector);
					configureShockwave();
				}
				// shared
				{
					m_radialOrientator.EffectorMask 
						= (ushort)ComponentMask.FlyingSparks | (ushort)ComponentMask.SmokeTrails;
					AddEffector (m_radialOrientator);
				}
			}

			public virtual void ShowExplosion(Vector3 position, float intensity)
			{
				emitFlameSmoke (position, intensity);
				emitFlash (position, intensity);
				emitFlyingSparks (position, intensity);
				emitSmokeTrails (position, intensity);
				emitRoundSparks (position, intensity);
				emitDebris (position, intensity);
				emitShockwave (position, intensity);
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

			protected void configureFlameSmoke()
			{
				m_flameSmokeEmitter.SpriteRectangles = m_flameSmokeSprites;
				m_flameSmokeEmitter.ParticlesPerEmission = 2;
				m_flameSmokeEmitter.EmissionInterval = 0.03f * m_flameSmokeDuration;
				m_flameSmokeEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
				m_flameSmokeEmitter.Life = m_flameSmokeDuration;
				m_flameSmokeEmitter.OrientationMin = new Vector3(0f, 0f, 0f);
				m_flameSmokeEmitter.OrientationMax = new Vector3(0f, 0f, 2f*(float)Math.PI);
				m_flameSmokeEmitter.BillboardXY = true;
				m_flameSmokeEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.5f);
				m_flameSmokeEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.5f);
				m_flameSmokeEmitter.RadiusOffsetMin = 0f;
				m_flameSmokeEmitter.RadiusOffsetMax = 0.5f;

				m_flamesSmokeColorEffector.ColorMask = m_flameColor;
				m_flamesSmokeColorEffector.ParticleLifetime = m_flameSmokeDuration;
				m_flamesSmokeColorEffector.Keyframes.Clear ();
				m_flamesSmokeColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
				m_flamesSmokeColorEffector.Keyframes.Add (0.4f*m_flameSmokeDuration, new Color4 (0f, 0f, 0f, 0.5f));
				m_flamesSmokeColorEffector.Keyframes.Add (m_flameSmokeDuration, new Color4 (0f, 0f, 0f, 0f));

				m_flameSmokeScaleEffector.ParticleLifetime = m_flameSmokeDuration;
				m_flameSmokeScaleEffector.Keyframes.Clear ();
				m_flameSmokeScaleEffector.Keyframes.Add (0f, 0.1f);
				m_flameSmokeScaleEffector.Keyframes.Add (0.25f * m_flameSmokeDuration, 1f);
				m_flameSmokeScaleEffector.Keyframes.Add (m_flameSmokeDuration, 1.2f);
			}

			protected void emitFlameSmoke(Vector3 position, float intensity)
			{
				m_flameSmokeEmitter.ComponentScale = new Vector3(intensity*3f, intensity*3f, 1f);
				m_flameSmokeEmitter.VelocityMagnitudeMin = 0.60f * intensity;
				m_flameSmokeEmitter.VelocityMagnitudeMax = 0.80f * intensity;
				m_flameSmokeEmitter.Center = position;
				m_flameSmokeEmitter.TotalEmissionsLeft = 5;
			}

			protected void configureFlash()
			{
				m_flashEmitter.SpriteRectangles = m_flashSprites;
				m_flashEmitter.ParticlesPerEmissionMin = 1;
				m_flashEmitter.ParticlesPerEmissionMax = 2;
				m_flashEmitter.EmissionIntervalMin = 0f;
				m_flashEmitter.EmissionIntervalMax = 0.2f * m_flashDuration;
				m_flashEmitter.Life = m_flashDuration;
				m_flashEmitter.Velocity = Vector3.Zero;
				m_flashEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
				m_flashEmitter.OrientationMax = new Vector3 (0f, 0f, 2f*(float)Math.PI);
				m_flashEmitter.BillboardXY = true;
				m_flashEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()

				m_flashColorEffector.ParticleLifetime = m_flashDuration;
				m_flashColorEffector.ColorMask = m_flashColor;
				m_flashColorEffector.Keyframes.Clear ();
				m_flashColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
				m_flashColorEffector.Keyframes.Add (m_flashDuration, new Color4 (1f, 1f, 1f, 0f));

				m_flashScaleEffector.ParticleLifetime = m_flashDuration;
				m_flashScaleEffector.Keyframes.Clear ();
				m_flashScaleEffector.Keyframes.Add (0f, 1f);
				m_flashScaleEffector.Keyframes.Add (m_flashDuration, 1.5f);
			}

			protected void emitFlash(Vector3 position, float intensity)
			{
				m_flashEmitter.ComponentScale = new Vector3(intensity*3f, intensity*3f, 1f);
				ParticlesSphereGenerator flashSphere = m_flashEmitter.Field as ParticlesSphereGenerator;
				flashSphere.Center = position;
				flashSphere.Radius = 0.3f * intensity;
				m_flashEmitter.TotalEmissionsLeft = 2;
			}

			protected void configureFlyingSparks()
			{
				m_flyingSparksEmitter.SpriteRectangles = m_flyingSparksSprites;
				m_flyingSparksEmitter.EmissionIntervalMin = 0f;
				m_flyingSparksEmitter.EmissionIntervalMax = 0.1f * m_flyingSparksDuration;
				m_flyingSparksEmitter.Life = m_flyingSparksDuration;
				m_flyingSparksEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
				m_flyingSparksEmitter.ComponentScale = new Vector3 (5f, 1f, 1f);
				m_flyingSparksEmitter.Color = m_flyingSparksColor;

				m_flyingSparksColorEffector.ColorMask = m_flashColor;
				m_flyingSparksColorEffector.Keyframes.Clear ();
				m_flyingSparksColorEffector.Keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
				m_flyingSparksColorEffector.Keyframes.Add (m_flyingSparksDuration, new Color4 (1f, 1f, 1f, 0f));
				m_flyingSparksColorEffector.ParticleLifetime = m_flyingSparksDuration;
			}

			protected void emitFlyingSparks(Vector3 position, float intensity)
			{
				m_flyingSparksEmitter.Center = position;
				m_flyingSparksEmitter.VelocityMagnitudeMin = intensity * 2f;
				m_flyingSparksEmitter.VelocityMagnitudeMax = intensity * 3f;
				m_flyingSparksEmitter.ParticlesPerEmission = (int)(5.0*Math.Log(intensity));
				m_flyingSparksEmitter.TotalEmissionsLeft = 1;
			}

			protected void configureSmokeTrails()
			{
				m_smokeTrailsEmitter.RadiusOffset = 3f;
				m_smokeTrailsEmitter.SpriteRectangles = m_smokeTrailsSprites;
				m_smokeTrailsEmitter.ParticlesPerEmission = 16;
				m_smokeTrailsEmitter.EmissionIntervalMin = 0f;
				m_smokeTrailsEmitter.EmissionIntervalMax = 0.1f * m_smokeTrailsDuration;
				m_smokeTrailsEmitter.Life = m_smokeTrailsDuration;
				m_smokeTrailsEmitter.TotalEmissionsLeft = 0; // control this in ShowExplosion()
				m_smokeTrailsEmitter.Color = m_smokeTrailsColor;

				m_smokeTrailsColorEffector.ParticleLifetime = m_smokeTrailsDuration;
				m_smokeTrailsColorEffector.ColorMask = m_smokeTrailsColor;
				m_smokeTrailsColorEffector.Keyframes.Clear ();
				m_smokeTrailsColorEffector.Keyframes.Add(0f, new Color4(1f, 1f, 1f, 1f));
				m_smokeTrailsColorEffector.Keyframes.Add(m_smokeTrailsDuration, new Color4(0.3f, 0.3f, 0.3f, 0f));

				m_smokeTrailsScaleEffector.ParticleLifetime = m_smokeTrailsDuration;
				m_smokeTrailsScaleEffector.BaseOffset = new Vector3(1f, 1f, 1f);
				m_smokeTrailsScaleEffector.Keyframes.Clear ();
				m_smokeTrailsScaleEffector.Keyframes.Add(0f, new Vector3(0f));
				m_smokeTrailsScaleEffector.Keyframes.Add(0.5f*m_smokeTrailsDuration, new Vector3(12f, 1.5f, 0f));
				m_smokeTrailsScaleEffector.Keyframes.Add(m_smokeTrailsDuration, new Vector3(7f, 2f, 0f));
			}

			protected void emitSmokeTrails(Vector3 position, float intensity)
			{
				m_smokeTrailsEmitter.Center = position;
				m_smokeTrailsEmitter.VelocityMagnitudeMin = intensity * 0.8f;
				m_smokeTrailsEmitter.VelocityMagnitudeMax = intensity * 1f;
				m_smokeTrailsEmitter.ParticlesPerEmission = (int)(5.0*Math.Log(intensity));
				m_smokeTrailsEmitter.TotalEmissionsLeft = 1;
				m_smokeTrailsEmitter.RadiusOffset = intensity;

				m_smokeTrailsScaleEffector.Amplification = new Vector3(0.3f*intensity, 0.15f*intensity, 0f);
			}

			protected void configureRoundSparks()
			{
				m_roundSparksEmitter.SpriteRectangles = m_roundSparksSprites;
				m_roundSparksEmitter.ParticlesPerEmission = 6;
				m_roundSparksEmitter.EmissionIntervalMin = 0f;
				m_roundSparksEmitter.EmissionIntervalMax = 0.05f * m_roundSparksDuration;
				m_roundSparksEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
				m_roundSparksEmitter.Life = m_roundSparksDuration;
				m_roundSparksEmitter.BillboardXY = true;
				m_roundSparksEmitter.OrientationMin = new Vector3 (0f, 0f, 0f);
				m_roundSparksEmitter.OrientationMax = new Vector3 (0f, 0f, 2f*(float)Math.PI);
				m_roundSparksEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.25f);
				m_roundSparksEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.25f);
				m_roundSparksEmitter.RadiusOffsetMin = 0f;
				m_roundSparksEmitter.RadiusOffsetMax = 1f;

				m_roundSparksColorEffector.ParticleLifetime = m_roundSparksDuration;
				m_roundSparksColorEffector.ColorMask = m_roundSparksColor;
				m_roundSparksColorEffector.Keyframes.Clear ();
				m_roundSparksColorEffector.Keyframes.Add (0.1f*m_roundSparksDuration, new Color4 (1f, 1f, 1f, 1f));
				m_roundSparksColorEffector.Keyframes.Add (m_roundSparksDuration, new Color4 (1f, 1f, 1f, 0f));

				m_roundSparksScaleEffector.ParticleLifetime = m_roundSparksDuration;
				m_roundSparksScaleEffector.Keyframes.Clear ();
				m_roundSparksScaleEffector.Keyframes.Add (0f, 1f);
				m_roundSparksScaleEffector.Keyframes.Add (0.25f * m_roundSparksDuration, 3f);
				m_roundSparksScaleEffector.Keyframes.Add (m_roundSparksDuration, 6f);
			}

			protected void emitRoundSparks(Vector3 position, float intensity)
			{
				m_roundSparksEmitter.ComponentScale = new Vector3(intensity, intensity, 1f);
				m_roundSparksEmitter.VelocityMagnitudeMin = 0.7f * intensity;
				m_roundSparksEmitter.VelocityMagnitudeMax = 1.2f * intensity;
				m_roundSparksEmitter.Center = position;
				m_roundSparksEmitter.TotalEmissionsLeft = 3;
			}

			protected void configureDebris()
			{
				m_debrisEmitter.SpriteRectangles = m_debrisSprites;
				m_debrisEmitter.ParticlesPerEmissionMin = 7;
				m_debrisEmitter.ParticlesPerEmissionMax = 10;
				m_debrisEmitter.TotalEmissionsLeft = 0; // Control this in ShowExplosion()
				m_debrisEmitter.Life = m_debrisDuration;
				m_debrisEmitter.OrientationMin = new Vector3(0f, 0f, 0f);
				m_debrisEmitter.OrientationMax = new Vector3(0f, 0f, 2f*(float)Math.PI);
				m_debrisEmitter.BillboardXY = true;
				m_debrisEmitter.AngularVelocityMin = new Vector3 (0f, 0f, -0.5f);
				m_debrisEmitter.AngularVelocityMax = new Vector3 (0f, 0f, +0.5f);
				m_debrisEmitter.RadiusOffsetMin = 0f;
				m_debrisEmitter.RadiusOffsetMax = 1f;

				var debrisColorFinal = new Color4(m_debrisColorEnd.R, m_debrisColorEnd.G, m_debrisColorEnd.B, 0f);
				m_debrisColorEffector.ParticleLifetime = m_debrisDuration;
				m_debrisColorEffector.Keyframes.Clear();
				m_debrisColorEffector.Keyframes.Add (0f, m_debrisColorStart);
				m_debrisColorEffector.Keyframes.Add (0.3f*m_debrisDuration, m_debrisColorEnd);
				m_debrisColorEffector.Keyframes.Add (m_debrisDuration, debrisColorFinal);
			}

			protected void emitDebris(Vector3 position, float intensity)
			{
				//m_debrisEmitter.MasterScale = intensity / 2f;
				m_debrisEmitter.MasterScaleMin = 3f;
				m_debrisEmitter.MasterScaleMax = 0.4f*intensity;
				m_debrisEmitter.VelocityMagnitudeMin = 1f * intensity;
				m_debrisEmitter.VelocityMagnitudeMax = 3f * intensity;
				m_debrisEmitter.Center = position;
				m_debrisEmitter.ParticlesPerEmission = (int)(2.5*Math.Log(intensity));
				m_debrisEmitter.TotalEmissionsLeft = 1;
			}

			protected void configureShockwave()
			{
				m_shockwaveEmitter.SpriteRectangles = m_shockwaveSprites;
				m_shockwaveEmitter.ParticlesPerEmission = 1;
				m_shockwaveEmitter.TotalEmissionsLeft = 0;   // Control this in ShowExplosion()
				m_shockwaveEmitter.Life = m_shockwaveDuration;
				m_shockwaveEmitter.Velocity = Vector3.Zero;

				m_shockwaveScaleEffector.ParticleLifetime = m_shockwaveDuration;
				m_shockwaveScaleEffector.Keyframes.Clear();
				m_shockwaveScaleEffector.Keyframes.Add(0f, 0f);
				m_shockwaveScaleEffector.Keyframes.Add(m_shockwaveDuration, 7f);

				m_shockwaveColorEffector.ParticleLifetime = m_shockwaveDuration;
				m_shockwaveColorEffector.ColorMask = m_shockwaveColor;
				m_shockwaveColorEffector.Keyframes.Clear ();
				m_shockwaveColorEffector.Keyframes.Add(0f, new Color4(1f, 1f, 1f, 1f));
				m_shockwaveColorEffector.Keyframes.Add(m_shockwaveDuration, new Color4(1f, 1f, 1f, 0f));
			}

			protected void emitShockwave(Vector3 position, float intensity)
			{
				m_shockwaveEmitter.ComponentScale = new Vector3(intensity, intensity, 1f);
				m_shockwaveEmitter.Position = position;
				m_shockwaveEmitter.TotalEmissionsLeft = 1;
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

