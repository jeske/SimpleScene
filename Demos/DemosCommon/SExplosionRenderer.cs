// (C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
	public class SExplosionRenderer : SSInstancedMeshRenderer
	{
		new public SExplosionSystem particleSystem {
			get { return base.instanceData as SExplosionSystem; }
		}

		public SExplosionRenderer(int particleCapacity = 100, SSTexture texture = null)
			: base(new SExplosionSystem(particleCapacity),
				   SSTexturedQuad.DoubleFaceInstance,
				   _defaultUsageHint
			 )
		{
			renderState.castsShadow = false;
			renderState.receivesShadows = false;
            renderState.doBillboarding = false;
            renderState.alphaBlendingOn = true;
			renderState.depthTest = true;
            renderState.depthWrite = false;
            renderState.lighted = false;
			
            simulateOnUpdate = true;
			Name = "simple expolsion renderer";

			base.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
			base.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
			base.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
			base.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
			base.ShininessMatColor = 0f;

			var tex = texture ?? SExplosionSystem.getDefaultTexture();
			textureMaterial = new SSTextureMaterial(null, null, tex, null);
		}

		public void showExplosion(Vector3 position, float intensity)
		{
			particleSystem.showExplosion (position, intensity);
		}

		/// <summary>
		/// An explosion system based on a a gamedev.net article
		/// http://www.gamedev.net/page/resources/_/creative/visual-arts/make-a-particle-explosion-effect-r2701
		/// </summary>
		public class SExplosionSystem : SSParticleSystemData
		{
			public static SSTexture getDefaultTexture()
			{
				return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7.png");
				//return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7_debug.png");
			}

			#region flame smoke parameters
			public RectangleF[] flameSmokeSprites {
				get { return _flameSmokeSprites; }
				set { _flameSmokeSprites = value; configureFlameSmoke (); }
			}
			public Color4 flameColor
			{
				get { return _flameColor; }
				set { _flameColor = value; configureFlameSmoke (); }
			}
			public float flameSmokeDuration
			{
				get { return _flameSmokeDuration; }
				set { _flameSmokeDuration = value; configureFlameSmoke (); }
			}
			#endregion

			#region flash parameters
			public RectangleF[] flashSprites {
				get { return _flashSprites; }
				set { _flashSprites = value; configureFlash (); }
			}
			public Color4 flashColor {
				get { return _flashColor; }
				set { _flashColor = value; configureFlash (); }
			}
			public float flashDuration {
				get { return _flashDuration; }
				set { _flashDuration = value; configureFlash (); }
			}
			#endregion

			#region flying sparks parameters
			public RectangleF[] flyingSparksSprites {
				get { return _flyingSparksSprites; }
				set { _flyingSparksSprites = value; configureFlyingSparks (); }
			}
			public Color4 flyingSparksColor {
				get { return _flyingSparksColor; }
				set { _flyingSparksColor = value; configureFlyingSparks (); }
			}
			public float flyingSparksDuration {
				get { return _flyingSparksDuration; }
				set { _flyingSparksDuration = value; configureFlyingSparks (); }
			}
			#endregion

			#region smoke trails parameters
			public RectangleF[] smokeTrailsSprites {
				get { return _smokeTrailsSprites; }
				set { _smokeTrailsSprites = value; configureSmokeTrails (); }
			}
			public Color4 smokeTrailsColor {
				get { return _smokeTrailsColor; }
				set { _smokeTrailsColor = value; configureSmokeTrails (); }
			}
			public float smokeTrailsDuration {
				get { return _smokeTrailsDuration; }
				set { _smokeTrailsDuration = value; configureSmokeTrails (); }
			}
			#endregion

			#region round sparks parameters
			public RectangleF[] roundSparksSprites {
				get { return _roundSparksSprites; }
				set { _roundSparksSprites = value; configureRoundSparks (); }
			}
			public Color4 roundSparksColor {
				get { return _roundSparksColor; }
				set { _roundSparksColor = value; configureRoundSparks (); }
			}
			public float roundSparksDuration {
				get { return _roundSparksDuration; }
				set { _roundSparksDuration = value; configureRoundSparks (); }
			}
			#endregion

			#region debris parameters
			public RectangleF[] debrisSprites {
				get { return _debrisSprites; }
				set { _debrisSprites = value; configureDebris (); }
			}
			public Color4 debrisColorStart {
				get { return _debrisColorStart; }
				set { _debrisColorStart = value; configureDebris (); }
			}
			public Color4 debrisColorEnd {
				get { return _debrisColorEnd; }
				set { _debrisColorEnd = value; configureDebris (); }
			}
			public float debrisDuration {
				get { return _debrisDuration; }
				set { _debrisDuration = value; configureDebris (); }
			}
			#endregion

			#region shockwave parameters
			public RectangleF[] shockwaveSprites {
				get { return _shockwaveSprites; }
				set { _shockwaveSprites = value; configureShockwave (); }
			}
			public Color4 shockwaveColor {
				get { return _shockwaveColor; }
				set { _shockwaveColor = value; configureShockwave (); }
			}
			public float shockwaveDuration {
				get { return _shockwaveDuration; }
				set { _shockwaveDuration = value; configureShockwave (); }
			}
			#endregion

			/// <summary>
			/// Used to match emitted particles with effectors
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
			protected RectangleF[] _flameSmokeSprites = {
				new RectangleF(0f,    0f,    0.25f, 0.25f),
				new RectangleF(0f,    0.25f, 0.25f, 0.25f),
				new RectangleF(0.25f, 0.25f, 0.25f, 0.25f),
				new RectangleF(0.25f, 0f,    0.25f, 0.25f),
			};

			/// <summary>
			/// Default locations of flash sprites in fig7.png
			/// </summary>
			protected RectangleF[] _flashSprites = {
				new RectangleF(0.5f,  0f,    0.25f, 0.25f),
				new RectangleF(0.75f, 0f,    0.25f, 0.25f),
				new RectangleF(0.5f,  0.25f, 0.25f, 0.25f),
				new RectangleF(0.75f, 0.25f, 0.25f, 0.25f),
			};

			/// <summary>
			/// Default locations of smoke trails sprites in fig7.png
			/// </summary>
			protected RectangleF[] _smokeTrailsSprites = {
				new RectangleF(0f, 0.5f,   0.5f, 0.125f),
				new RectangleF(0f, 0.625f, 0.5f, 0.125f),
				new RectangleF(0f, 0.75f,  0.5f, 0.125f),
			};

			protected RectangleF[] _flyingSparksSprites = {
				new RectangleF(0.75f, 0.85f, 0.25f, 0.05f)
			};

			protected RectangleF[] _roundSparksSprites = {
				new RectangleF(0.5f, 0.75f, 0.25f, 0.25f)
			};

			protected RectangleF[] _debrisSprites = {
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

			protected RectangleF[] _shockwaveSprites = {
				new RectangleF (0.75f, 0.5f, 0.25f, 0.25f)
			};
			#endregion

			#region effects' colors
			protected Color4 _flameColor = Color4.DarkOrange;
			protected Color4 _flashColor = Color4.Yellow;
			protected Color4 _flyingSparksColor = Color4.DarkGoldenrod;
			protected Color4 _smokeTrailsColor = Color4.Orange;
			protected Color4 _roundSparksColor = Color4.OrangeRed;
			protected Color4 _debrisColorStart = Color4.Orange;
			protected Color4 _debrisColorEnd = Color4.Silver;
			protected Color4 _shockwaveColor = Color4.Orange;
			#endregion

			#region timing settings
			protected float _timeScale = 1f;
			protected float _flameSmokeDuration = 2.5f;
			protected float _flashDuration = 0.5f;
			protected float _flyingSparksDuration = 2.5f;
			protected float _smokeTrailsDuration = 1.5f;
			protected float _roundSparksDuration = 2.5f;
			protected float _debrisDuration = 4f;
			protected float _shockwaveDuration = 2f;
			#endregion

			#region emitters and effectors
			// flame/smoke
			protected readonly SSRadialEmitter _flameSmokeEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector _flamesSmokeColorEffector
				= new SSColorKeyframesEffector();
			protected readonly SSMasterScaleKeyframesEffector _flameSmokeScaleEffector
				= new SSMasterScaleKeyframesEffector();
			// flash
			protected readonly SSParticlesFieldEmitter _flashEmitter
				= new SSParticlesFieldEmitter(new ParticlesSphereGenerator());
			protected readonly SSColorKeyframesEffector _flashColorEffector 
				= new SSColorKeyframesEffector ();
			protected readonly SSMasterScaleKeyframesEffector _flashScaleEffector 
				= new SSMasterScaleKeyframesEffector ();
			// flying sparks
			protected readonly SSRadialEmitter _flyingSparksEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector _flyingSparksColorEffector 
				= new SSColorKeyframesEffector ();
			// smoke trails
			protected readonly SSRadialEmitter _smokeTrailsEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector _smokeTrailsColorEffector 
				= new SSColorKeyframesEffector();
			protected readonly SSComponentScaleKeyframeEffector _smokeTrailsScaleEffector
				= new SSComponentScaleKeyframeEffector ();
			// round sparks
			protected readonly SSRadialEmitter _roundSparksEmitter
				= new SSRadialEmitter();
			protected readonly SSColorKeyframesEffector _roundSparksColorEffector 
				= new SSColorKeyframesEffector ();
			protected readonly SSMasterScaleKeyframesEffector _roundSparksScaleEffector 
				= new SSMasterScaleKeyframesEffector ();
			// debris
			protected readonly SSRadialEmitter _debrisEmitter
				= new SSRadialEmitter ();
			protected readonly SSColorKeyframesEffector _debrisColorEffector 
				= new SSColorKeyframesEffector ();
			// shockwave
			protected readonly ShockwaveEmitter _shockwaveEmitter
				= new ShockwaveEmitter();
			protected readonly SSMasterScaleKeyframesEffector _shockwaveScaleEffector 
				= new SSMasterScaleKeyframesEffector();
			protected readonly SSColorKeyframesEffector _shockwaveColorEffector 
				= new SSColorKeyframesEffector();
			// shared
			protected readonly RadialBillboardOrientator _radialOrientator
				= new RadialBillboardOrientator();
			#endregion

			public SExplosionSystem (int particleCapacity)
				: base(particleCapacity)
			{
				// flame/smoke
				{
					_flameSmokeEmitter.effectorMask 
						= _flameSmokeScaleEffector.effectorMask 
						= _flamesSmokeColorEffector.effectorMask
						= (ushort)ComponentMask.FlameSmoke;
					addEmitter(_flameSmokeEmitter);
					addEffector(_flamesSmokeColorEffector);
					addEffector(_flameSmokeScaleEffector);
					configureFlameSmoke();
				}
				// flash
				{
					_flashEmitter.effectorMask 
						= _flashColorEffector.effectorMask 
						= _flashScaleEffector.effectorMask 
						= (ushort)ComponentMask.Flash;
					addEmitter (_flashEmitter);
					addEffector (_flashColorEffector);
					addEffector (_flashScaleEffector);
					configureFlash();
				}
				// flying sparks
				{
					_flyingSparksEmitter.effectorMask 
						= _flyingSparksColorEffector.effectorMask
						= (ushort)ComponentMask.FlyingSparks;
					addEmitter (_flyingSparksEmitter);
					addEffector (_flyingSparksColorEffector);
					configureFlyingSparks();
				}
				// smoke trails
				{
					_smokeTrailsEmitter.effectorMask
						= _smokeTrailsColorEffector.effectorMask
						= _smokeTrailsScaleEffector.effectorMask
						= (ushort)ComponentMask.SmokeTrails;
					addEmitter(_smokeTrailsEmitter);
					addEffector(_smokeTrailsColorEffector);
					addEffector(_smokeTrailsScaleEffector);
					configureSmokeTrails();
				}
				// round sparks
				{
					_roundSparksEmitter.effectorMask 
						= _roundSparksScaleEffector.effectorMask 
						= _roundSparksColorEffector.effectorMask
						= (ushort)ComponentMask.RoundSparks;
					addEmitter (_roundSparksEmitter);
					addEffector (_roundSparksColorEffector);
					addEffector (_roundSparksScaleEffector);
					configureRoundSparks();
				}
				// debris
				{
					_debrisEmitter.effectorMask 
						= _debrisColorEffector.effectorMask
						= (ushort)ComponentMask.Debris;
					addEmitter (_debrisEmitter);
					addEffector (_debrisColorEffector);
					configureDebris();
				}
				// shockwave
				{
					_shockwaveEmitter.effectorMask
						= _shockwaveScaleEffector.effectorMask
						= _shockwaveColorEffector.effectorMask
						= (ushort)ComponentMask.Shockwave;
					addEmitter(_shockwaveEmitter);
					addEffector(_shockwaveScaleEffector);
					addEffector(_shockwaveColorEffector);
					configureShockwave();
				}
				// shared
				{
					_radialOrientator.effectorMask 
						= (ushort)ComponentMask.FlyingSparks | (ushort)ComponentMask.SmokeTrails;
					addEffector (_radialOrientator);
				}
			}

			public virtual void showExplosion(Vector3 position, float intensity)
			{
				emitFlameSmoke (position, intensity);
				emitFlash (position, intensity);
				emitFlyingSparks (position, intensity);
				emitSmokeTrails (position, intensity);
				emitRoundSparks (position, intensity);
				emitDebris (position, intensity);
				emitShockwave (position, intensity);
			}

			public override void update(float timeDelta)
			{
				timeDelta *= _timeScale;
				base.update(timeDelta);
			}

			public override void updateCamera (ref Matrix4 model, ref Matrix4 view, ref Matrix4 projection)
			{
                var modelView = model * view;
				_radialOrientator.updateModelView (ref modelView);
				_shockwaveEmitter.updateModelView (ref modelView);
			}

			protected void configureFlameSmoke()
			{
				_flameSmokeEmitter.spriteRectangles = _flameSmokeSprites;
				_flameSmokeEmitter.particlesPerEmission = 2;
				_flameSmokeEmitter.emissionInterval = 0.03f * _flameSmokeDuration;
				_flameSmokeEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
				_flameSmokeEmitter.life = _flameSmokeDuration;
				_flameSmokeEmitter.orientationMin = new Vector3(0f, 0f, 0f);
				_flameSmokeEmitter.orientationMax = new Vector3(0f, 0f, 2f*(float)Math.PI);
				_flameSmokeEmitter.billboardXY = true;
				_flameSmokeEmitter.angularVelocityMin = new Vector3 (0f, 0f, -0.5f);
				_flameSmokeEmitter.angularVelocityMax = new Vector3 (0f, 0f, +0.5f);
				_flameSmokeEmitter.radiusOffsetMin = 0f;
				_flameSmokeEmitter.radiusOffsetMax = 0.5f;

				_flamesSmokeColorEffector.colorMask = _flameColor;
				_flamesSmokeColorEffector.particleLifetime = _flameSmokeDuration;
				_flamesSmokeColorEffector.keyframes.Clear ();
				_flamesSmokeColorEffector.keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
				_flamesSmokeColorEffector.keyframes.Add (0.4f, new Color4 (0f, 0f, 0f, 0.5f));
				_flamesSmokeColorEffector.keyframes.Add (1f, new Color4 (0f, 0f, 0f, 0f));

				_flameSmokeScaleEffector.particleLifetime = _flameSmokeDuration;
				_flameSmokeScaleEffector.keyframes.Clear ();
				_flameSmokeScaleEffector.keyframes.Add (0f, 0.1f);
				_flameSmokeScaleEffector.keyframes.Add (0.25f, 1f);
				_flameSmokeScaleEffector.keyframes.Add (1f, 1.2f);
			}

			protected void emitFlameSmoke(Vector3 position, float intensity)
			{
				_flameSmokeEmitter.componentScale = new Vector3(intensity*3f, intensity*3f, 1f);
				_flameSmokeEmitter.velocityMagnitudeMin = 0.60f * intensity;
				_flameSmokeEmitter.velocityMagnitudeMax = 0.80f * intensity;
				_flameSmokeEmitter.center = position;
				_flameSmokeEmitter.totalEmissionsLeft = 5;
			}

			protected void configureFlash()
			{
				_flashEmitter.spriteRectangles = _flashSprites;
				_flashEmitter.particlesPerEmissionMin = 1;
				_flashEmitter.particlesPerEmissionMax = 2;
				_flashEmitter.emissionIntervalMin = 0f;
				_flashEmitter.emissionIntervalMax = 0.2f * _flashDuration;
				_flashEmitter.life = _flashDuration;
				_flashEmitter.velocity = Vector3.Zero;
				_flashEmitter.orientationMin = new Vector3 (0f, 0f, 0f);
				_flashEmitter.orientationMax = new Vector3 (0f, 0f, 2f*(float)Math.PI);
				_flashEmitter.billboardXY = true;
				_flashEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()

				_flashColorEffector.particleLifetime = _flashDuration;
				_flashColorEffector.colorMask = _flashColor;
				_flashColorEffector.keyframes.Clear ();
				_flashColorEffector.keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
				_flashColorEffector.keyframes.Add (1f, new Color4 (1f, 1f, 1f, 0f));

				_flashScaleEffector.particleLifetime = _flashDuration;
				_flashScaleEffector.keyframes.Clear ();
				_flashScaleEffector.keyframes.Add (0f, 1f);
				_flashScaleEffector.keyframes.Add (1f, 1.5f);
			}

			protected void emitFlash(Vector3 position, float intensity)
			{
				_flashEmitter.componentScale = new Vector3(intensity*3f, intensity*3f, 1f);
				ParticlesSphereGenerator flashSphere = _flashEmitter.Field as ParticlesSphereGenerator;
				flashSphere.Center = position;
				flashSphere.Radius = 0.3f * intensity;
				_flashEmitter.totalEmissionsLeft = 2;
			}

			protected void configureFlyingSparks()
			{
				_flyingSparksEmitter.spriteRectangles = _flyingSparksSprites;
				_flyingSparksEmitter.emissionIntervalMin = 0f;
				_flyingSparksEmitter.emissionIntervalMax = 0.1f * _flyingSparksDuration;
				_flyingSparksEmitter.life = _flyingSparksDuration;
				_flyingSparksEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
				_flyingSparksEmitter.componentScale = new Vector3 (5f, 1f, 1f);
				_flyingSparksEmitter.color = _flyingSparksColor;

				_flyingSparksColorEffector.colorMask = _flashColor;
				_flyingSparksColorEffector.keyframes.Clear ();
				_flyingSparksColorEffector.keyframes.Add (0f, new Color4 (1f, 1f, 1f, 1f));
				_flyingSparksColorEffector.keyframes.Add (1f, new Color4 (1f, 1f, 1f, 0f));
				_flyingSparksColorEffector.particleLifetime = _flyingSparksDuration;
			}

			protected void emitFlyingSparks(Vector3 position, float intensity)
			{
				_flyingSparksEmitter.center = position;
				_flyingSparksEmitter.velocityMagnitudeMin = intensity * 2f;
				_flyingSparksEmitter.velocityMagnitudeMax = intensity * 3f;
				_flyingSparksEmitter.particlesPerEmission = (int)(5.0*Math.Log(intensity));
				_flyingSparksEmitter.totalEmissionsLeft = 1;
			}

			protected void configureSmokeTrails()
			{
				_smokeTrailsEmitter.radiusOffset = 3f;
				_smokeTrailsEmitter.spriteRectangles = _smokeTrailsSprites;
				_smokeTrailsEmitter.particlesPerEmission = 16;
				_smokeTrailsEmitter.emissionIntervalMin = 0f;
				_smokeTrailsEmitter.emissionIntervalMax = 0.1f * _smokeTrailsDuration;
				_smokeTrailsEmitter.life = _smokeTrailsDuration;
				_smokeTrailsEmitter.totalEmissionsLeft = 0; // control this in ShowExplosion()
				_smokeTrailsEmitter.color = _smokeTrailsColor;

				_smokeTrailsColorEffector.particleLifetime = _smokeTrailsDuration;
				_smokeTrailsColorEffector.colorMask = _smokeTrailsColor;
				_smokeTrailsColorEffector.keyframes.Clear ();
				_smokeTrailsColorEffector.keyframes.Add(0f, new Color4(1f, 1f, 1f, 1f));
				_smokeTrailsColorEffector.keyframes.Add(1f, new Color4(0.3f, 0.3f, 0.3f, 0f));

				_smokeTrailsScaleEffector.particleLifetime = _smokeTrailsDuration;
				_smokeTrailsScaleEffector.baseOffset = new Vector3(1f, 1f, 1f);
				_smokeTrailsScaleEffector.keyframes.Clear ();
				_smokeTrailsScaleEffector.keyframes.Add(0f, new Vector3(0f));
				_smokeTrailsScaleEffector.keyframes.Add(0.5f, new Vector3(12f, 1.5f, 0f));
				_smokeTrailsScaleEffector.keyframes.Add(1f, new Vector3(7f, 2f, 0f));
			}

			protected void emitSmokeTrails(Vector3 position, float intensity)
			{
				_smokeTrailsEmitter.center = position;
				_smokeTrailsEmitter.velocityMagnitudeMin = intensity * 0.8f;
				_smokeTrailsEmitter.velocityMagnitudeMax = intensity * 1f;
				_smokeTrailsEmitter.particlesPerEmission = (int)(5.0*Math.Log(intensity));
				_smokeTrailsEmitter.totalEmissionsLeft = 1;
				_smokeTrailsEmitter.radiusOffset = intensity;

				_smokeTrailsScaleEffector.amplification = new Vector3(0.3f*intensity, 0.15f*intensity, 0f);
			}

			protected void configureRoundSparks()
			{
				_roundSparksEmitter.spriteRectangles = _roundSparksSprites;
				_roundSparksEmitter.particlesPerEmission = 6;
				_roundSparksEmitter.emissionIntervalMin = 0f;
				_roundSparksEmitter.emissionIntervalMax = 0.05f * _roundSparksDuration;
				_roundSparksEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
				_roundSparksEmitter.life = _roundSparksDuration;
				_roundSparksEmitter.billboardXY = true;
				_roundSparksEmitter.orientationMin = new Vector3 (0f, 0f, 0f);
				_roundSparksEmitter.orientationMax = new Vector3 (0f, 0f, 2f*(float)Math.PI);
				_roundSparksEmitter.angularVelocityMin = new Vector3 (0f, 0f, -0.25f);
				_roundSparksEmitter.angularVelocityMax = new Vector3 (0f, 0f, +0.25f);
				_roundSparksEmitter.radiusOffsetMin = 0f;
				_roundSparksEmitter.radiusOffsetMax = 1f;

				_roundSparksColorEffector.particleLifetime = _roundSparksDuration;
				_roundSparksColorEffector.colorMask = _roundSparksColor;
				_roundSparksColorEffector.keyframes.Clear ();
				_roundSparksColorEffector.keyframes.Add (0.1f, new Color4 (1f, 1f, 1f, 1f));
				_roundSparksColorEffector.keyframes.Add (1f, new Color4 (1f, 1f, 1f, 0f));

				_roundSparksScaleEffector.particleLifetime = _roundSparksDuration;
				_roundSparksScaleEffector.keyframes.Clear ();
				_roundSparksScaleEffector.keyframes.Add (0f, 1f);
				_roundSparksScaleEffector.keyframes.Add (0.25f, 3f);
				_roundSparksScaleEffector.keyframes.Add (1f, 6f);
			}

			protected void emitRoundSparks(Vector3 position, float intensity)
			{
				_roundSparksEmitter.componentScale = new Vector3(intensity, intensity, 1f);
				_roundSparksEmitter.velocityMagnitudeMin = 0.7f * intensity;
				_roundSparksEmitter.velocityMagnitudeMax = 1.2f * intensity;
				_roundSparksEmitter.center = position;
				_roundSparksEmitter.totalEmissionsLeft = 3;
			}

			protected void configureDebris()
			{
				_debrisEmitter.spriteRectangles = _debrisSprites;
				_debrisEmitter.particlesPerEmissionMin = 7;
				_debrisEmitter.particlesPerEmissionMax = 10;
				_debrisEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
				_debrisEmitter.life = _debrisDuration;
				_debrisEmitter.orientationMin = new Vector3(0f, 0f, 0f);
				_debrisEmitter.orientationMax = new Vector3(0f, 0f, 2f*(float)Math.PI);
				_debrisEmitter.billboardXY = true;
				_debrisEmitter.angularVelocityMin = new Vector3 (0f, 0f, -0.5f);
				_debrisEmitter.angularVelocityMax = new Vector3 (0f, 0f, +0.5f);
				_debrisEmitter.radiusOffsetMin = 0f;
				_debrisEmitter.radiusOffsetMax = 1f;

				var debrisColorFinal = new Color4(_debrisColorEnd.R, _debrisColorEnd.G, _debrisColorEnd.B, 0f);
				_debrisColorEffector.particleLifetime = _debrisDuration;
				_debrisColorEffector.keyframes.Clear();
				_debrisColorEffector.keyframes.Add (0f, _debrisColorStart);
				_debrisColorEffector.keyframes.Add (0.3f, _debrisColorEnd);
				_debrisColorEffector.keyframes.Add (1f, debrisColorFinal);
			}

			protected void emitDebris(Vector3 position, float intensity)
			{
				//m_debrisEmitter.MasterScale = intensity / 2f;
				_debrisEmitter.masterScaleMin = 3f;
				_debrisEmitter.masterScaleMax = 0.4f*intensity;
				_debrisEmitter.velocityMagnitudeMin = 1f * intensity;
				_debrisEmitter.velocityMagnitudeMax = 3f * intensity;
				_debrisEmitter.center = position;
				_debrisEmitter.particlesPerEmission = (int)(2.5*Math.Log(intensity));
				_debrisEmitter.totalEmissionsLeft = 1;
			}

			protected void configureShockwave()
			{
				_shockwaveEmitter.spriteRectangles = _shockwaveSprites;
				_shockwaveEmitter.particlesPerEmission = 1;
				_shockwaveEmitter.totalEmissionsLeft = 0;   // Control this in ShowExplosion()
				_shockwaveEmitter.life = _shockwaveDuration;
				_shockwaveEmitter.velocity = Vector3.Zero;

				_shockwaveScaleEffector.particleLifetime = _shockwaveDuration;
				_shockwaveScaleEffector.keyframes.Clear();
				_shockwaveScaleEffector.keyframes.Add(0f, 0f);
				_shockwaveScaleEffector.keyframes.Add(_shockwaveDuration, 7f);

				_shockwaveColorEffector.colorMask = _shockwaveColor;
                _shockwaveColorEffector.particleLifetime = _shockwaveDuration;
				_shockwaveColorEffector.keyframes.Clear ();
				_shockwaveColorEffector.keyframes.Add(0f, new Color4(1f, 1f, 1f, 1f));
				_shockwaveColorEffector.keyframes.Add(1f, new Color4(1f, 1f, 1f, 0f));
			}

			protected void emitShockwave(Vector3 position, float intensity)
			{
				_shockwaveEmitter.componentScale = new Vector3(intensity, intensity, 1f);
				_shockwaveEmitter.position = position;
				_shockwaveEmitter.totalEmissionsLeft = 1;
			}
		}

		public class RadialBillboardOrientator : SSParticleEffector
		{
			protected float _orientationX = 0f;

			/// <summary>
			/// Compute orientation around X once per frame to orient the sprites towards the viewer
			/// </summary>
			public void updateModelView(ref Matrix4 modelViewMatrix)
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
				_orientationX = -angle;
			}

			protected override void effectParticle (SSParticle particle, float deltaT)
			{
				Vector3 dir = particle.vel;

				// orient to look right
				float x = dir.X;
				float y = dir.Y;
				float z = dir.Z;
				float xy = dir.Xy.Length;
				float phi = (float)Math.Atan (z / xy);
				float theta = (float)Math.Atan2 (y, x);

				particle.orientation.Y = -phi;
				particle.orientation.Z = theta;
				particle.orientation.X = -_orientationX;
			}
		}

		public class ShockwaveEmitter : SSFixedPositionEmitter
		{
			public void updateModelView(ref Matrix4 modelViewMatrix)
			{
				Quaternion quat = modelViewMatrix.ExtractRotation ().Inverted();
				Vector3 euler = OpenTKHelper.QuaternionToEuler (ref quat);
				Vector3 baseVec = new Vector3 (euler.X + 0.5f*(float)Math.PI, euler.Y, euler.Z); 
				orientationMin = baseVec + new Vector3((float)Math.PI/8f, 0f, 0f);
				orientationMax = baseVec + new Vector3((float)Math.PI*3f/8f, 2f*(float)Math.PI, 0f);
			}
		}
	}
}

