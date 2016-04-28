// (C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    /// <summary>
    /// Manages multiple types of explosions and the renderers to make them happen
    /// </summary>
    public class SExplosionManager
    {
        protected readonly SSScene _renderScene;
        protected readonly int _particleCapacity;

        protected List<SExplosionRenderer> _renderers = new List<SExplosionRenderer> ();

        public SExplosionManager(SSScene renderScene, int particleCapacity = 500)
        {
            _renderScene = renderScene;
            _particleCapacity = particleCapacity;
        }

        public void showExplosion(SExplosionParameters eParams, float intensity,
            Vector3 position, Vector3 velocity, float simInterval = float.NaN)
        {
            _showExplosionCommon(null, eParams, position, velocity, intensity, simInterval);
        }

        public void showExplosionAttached(SExplosionParameters eParams, float intensity,
            SSObject attachTo, Vector3 localPos, float simInterval = float.NaN)
        {
            _showExplosionCommon(attachTo, eParams, localPos, Vector3.Zero, intensity, simInterval);
        }

        protected void _showExplosionCommon(SSObject attachTo, SExplosionParameters eParams,
            Vector3 position, Vector3 velocity, float intensity, float simInterval = float.NaN) 
        {
            SExplosionRenderer renderer = null;
            foreach (var existing in _renderers) {
                if (existing.particleSystem.numElements == 0) {
                    // recycle existing renderers...
                    renderer = existing;
                    renderer.attachTo = attachTo;
                    if (attachTo == null) {
                        renderer.worldMat = Matrix4.Identity;
                    } else {
                        renderer.worldMat = attachTo.worldMat;
                    }
                    renderer.parameters = eParams;
                    break;
                }
            }
            if (renderer == null) {
                // have to allocate a new renderer
                renderer = new SExplosionRenderer (_particleCapacity, eParams);
                renderer.attachTo = attachTo;
                _renderScene.AddObject(renderer);
                _renderers.Add(renderer);
            }
            if (!float.IsNaN(simInterval)) {
                renderer.particleSystem.simulationStep = simInterval;
            }
            renderer.showExplosion(position, intensity, velocity);
        }

        // TODO: automatic uncache on explosions not used in a long time?
        public void uncache()
        {
            foreach (var obj in _renderers) {
                obj.renderState.toBeDeleted = true;
            }
            _renderers.Clear();
        }
    }

    public class SExplosionRenderer : SSInstancedMeshRenderer
	{
        protected SSObject _attachTo = null;

        public SSObject attachTo {
            get { return _attachTo; }
            set { 
                _attachTo = value;
                if (_attachTo != null) {
                    this.worldMat = _attachTo.worldMat;
                }
            }
        }

        public SExplosionSystem particleSystem {
			get { return base.instanceData as SExplosionSystem; }
		}

        public SExplosionParameters parameters {
            get { return particleSystem.parameters; }
            set {
                this.particleSystem.parameters = value;
                var texture = particleSystem.parameters.getTexture();
                this.textureMaterial = new SSTextureMaterial(diffuse: texture);
            }
        }

        public SExplosionRenderer(int particleCapacity = 500, 
                                  SExplosionParameters eParams = null)
            : base(new SExplosionSystem(eParams ?? new SExplosionParameters(), particleCapacity),
				   SSTexturedQuad.DoubleFaceInstance,
				   _defaultUsageHint
			 )
		{
			renderState.castsShadow = false;
			renderState.receivesShadows = false;
            renderState.doBillboarding = false;
            renderState.alphaBlendingOn = true;
			//renderState.depthTest = true;
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

            var texture = particleSystem.parameters.getTexture();
            this.textureMaterial = new SSTextureMaterial(diffuse: texture);
		}

        public void showExplosion(Vector3 position, float intensity, 
            Vector3 baseVelocity = new Vector3())
		{
            particleSystem.showExplosion (position, intensity, baseVelocity);
            if (float.IsNaN(position.X)) {
                System.Console.WriteLine("bad position");
            }
		}

        public override void Render (SSRenderConfig renderConfig)
        {
            if (_attachTo != null) {
                this.worldMat = _attachTo.worldMat;
            }

            base.Render(renderConfig);
        }

		/// <summary>
		/// An explosion system based on a a gamedev.net article
		/// http://www.gamedev.net/page/resources/_/creative/visual-arts/make-a-particle-explosion-effect-r2701
		/// </summary>
		public class SExplosionSystem : SSParticleSystemData
		{
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

            protected SExplosionParameters _eParams;

			#region emitters and effectors
			// flame/smoke
            protected SSRadialEmitter _flameSmokeEmitter = null;
            protected SSColorKeyframesEffector _flamesSmokeColorEffector = null;
            protected SSMasterScaleKeyframesEffector _flameSmokeScaleEffector = null;
			// flash
            protected SSParticlesFieldEmitter _flashEmitter = null;
            protected SSColorKeyframesEffector _flashColorEffector = null;
            protected SSMasterScaleKeyframesEffector _flashScaleEffector = null;
			// flying sparks
            protected SSRadialEmitter _flyingSparksEmitter = null;
            protected SSColorKeyframesEffector _flyingSparksColorEffector = null;
			// smoke trails
            protected SSRadialEmitter _smokeTrailsEmitter = null;
            protected SSColorKeyframesEffector _smokeTrailsColorEffector = null;
            protected SSComponentScaleKeyframeEffector _smokeTrailsScaleEffector = null;
			// round sparks
            protected SSRadialEmitter _roundSparksEmitter = null;
            protected SSColorKeyframesEffector _roundSparksColorEffector = null;
            protected SSMasterScaleKeyframesEffector _roundSparksScaleEffector = null;
			// debris
            protected SSRadialEmitter _debrisEmitter = null;
            protected SSColorKeyframesEffector _debrisColorEffector = null;
			// shockwave
            protected ShockwaveEmitter _shockwaveEmitter = null;
            protected SSMasterScaleKeyframesEffector _shockwaveScaleEffector = null;
            protected SSColorKeyframesEffector _shockwaveColorEffector = null;
			// shared
            protected RadialBillboardOrientator _radialOrientator = null;
			#endregion

            public SExplosionParameters parameters {
                get { return _eParams; }
                set { _configureParameters(value); }
            }

            public SExplosionSystem (SExplosionParameters eParams, int particleCapacity)
				: base(particleCapacity)
			{
                _configureParameters(eParams);
				
                // shared
                _radialOrientator = new RadialBillboardOrientator();
				_radialOrientator.effectorMask 
					= (ushort)ComponentMask.FlyingSparks | (ushort)ComponentMask.SmokeTrails;
				addEffector (_radialOrientator);
			}

            public virtual void showExplosion(Vector3 position, float intensity, 
                Vector3 baseVelocity = new Vector3())
			{
                emitFlameSmoke (position, baseVelocity, intensity);
                emitFlash (position, baseVelocity, intensity);
                emitFlyingSparks (position, baseVelocity, intensity);
                emitSmokeTrails (position, baseVelocity, intensity);
                emitRoundSparks (position, baseVelocity, intensity);
                emitDebris (position, baseVelocity, intensity);
                emitShockwave (position, baseVelocity, intensity);
			}

			public override void update(float timeDelta)
			{
                timeDelta *= _eParams.timeScale;
				base.update(timeDelta);
			}

			public override void updateCamera (ref Matrix4 model, ref Matrix4 view, ref Matrix4 projection)
			{
                var modelView = model * view;
                if (_radialOrientator != null) {
                    _radialOrientator.updateModelView(ref modelView);
                }
                if (_shockwaveEmitter != null) {
    				_shockwaveEmitter.updateModelView (ref modelView);
                }
			}

            protected void _configureParameters(SExplosionParameters eParams)
            {
                _eParams = eParams;

                configureFlameSmoke();
                configureFlash();
                configureFlyingSparks();
                configureSmokeTrails();
                configureRoundSparks();
                configureDebris();
                configureShockwave();
            }

            protected void configureFlameSmoke()
			{
                if (_eParams.doFlameSmoke) {
                    if (_flameSmokeEmitter == null) {
                        _flameSmokeEmitter = new SSRadialEmitter ();
                        _flamesSmokeColorEffector = new SSColorKeyframesEffector ();
                        _flameSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
                        addEmitter(_flameSmokeEmitter);
                        addEffector(_flamesSmokeColorEffector);
                        addEffector(_flameSmokeScaleEffector);
                        _flameSmokeEmitter.effectorMask = _flameSmokeScaleEffector.effectorMask 
                            = _flamesSmokeColorEffector.effectorMask = (ushort)ComponentMask.FlameSmoke;
                    }

                    _flameSmokeEmitter.spriteRectangles = _eParams.flameSmokeSprites;
                    _flameSmokeEmitter.particlesPerEmission = 2;
                    _flameSmokeEmitter.emissionInterval = 0.03f * _eParams.flameSmokeDuration;
                    _flameSmokeEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
                    _flameSmokeEmitter.life = _eParams.flameSmokeDuration;
                    _flameSmokeEmitter.orientationMin = new Vector3 (0f, 0f, 0f);
                    _flameSmokeEmitter.orientationMax = new Vector3 (0f, 0f, 2f * (float)Math.PI);
                    _flameSmokeEmitter.billboardXY = true;
                    _flameSmokeEmitter.angularVelocityMin = new Vector3 (0f, 0f, -0.5f);
                    _flameSmokeEmitter.angularVelocityMax = new Vector3 (0f, 0f, +0.5f);
                    _flameSmokeEmitter.radiusOffsetMin = 0f;
                    _flameSmokeEmitter.radiusOffsetMax = 0.5f;

                    _flamesSmokeColorEffector.colorMask = _eParams.flameColor;
                    _flamesSmokeColorEffector.particleLifetime = _eParams.flameSmokeDuration;
                    _flamesSmokeColorEffector.keyframes.Clear();
                    _flamesSmokeColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _flamesSmokeColorEffector.keyframes.Add(0.4f, new Color4 (0f, 0f, 0f, 0.5f));
                    _flamesSmokeColorEffector.keyframes.Add(1f, new Color4 (0f, 0f, 0f, 0f));

                    _flameSmokeScaleEffector.particleLifetime = _eParams.flameSmokeDuration;
                    _flameSmokeScaleEffector.keyframes.Clear();
                    _flameSmokeScaleEffector.keyframes.Add(0f, 0.1f);
                    _flameSmokeScaleEffector.keyframes.Add(0.25f, 1f);
                    _flameSmokeScaleEffector.keyframes.Add(1f, 1.2f);
                } else if (_flameSmokeEmitter != null) {
                    removeEmitter(_flameSmokeEmitter);
                    removeEffector(_flamesSmokeColorEffector);
                    removeEffector(_flameSmokeScaleEffector);
                    _flameSmokeEmitter = null;
                    _flamesSmokeColorEffector = null;
                    _flameSmokeScaleEffector = null;
                }
			}

			protected void emitFlameSmoke(Vector3 position, Vector3 baseVelocity, float intensity)
			{
                if (!_eParams.doFlameSmoke) return;
				_flameSmokeEmitter.componentScale = new Vector3(intensity*3f, intensity*3f, 1f);
                _flameSmokeEmitter.velocityFromCenterMagnitudeMin = 0.60f * intensity;
				_flameSmokeEmitter.velocityFromCenterMagnitudeMax = 0.80f * intensity;
				_flameSmokeEmitter.center = position;
                _flameSmokeEmitter.velocity = baseVelocity;
				_flameSmokeEmitter.totalEmissionsLeft = 5;
			}

			protected void configureFlash()
			{
                if (_eParams.doFlash) {
                    if (_flashEmitter == null) {
                        _flashEmitter = new SSParticlesFieldEmitter (new ParticlesSphereGenerator ());
                        _flashColorEffector = new SSColorKeyframesEffector ();
                        _flashScaleEffector = new SSMasterScaleKeyframesEffector ();
                        addEmitter(_flashEmitter);
                        addEffector(_flashColorEffector);
                        addEffector(_flashScaleEffector);
                        _flashEmitter.effectorMask = _flashColorEffector.effectorMask 
                            = _flashScaleEffector.effectorMask = (ushort)ComponentMask.Flash;
                    }

                    _flashEmitter.spriteRectangles = _eParams.flashSprites;
                    _flashEmitter.particlesPerEmissionMin = 1;
                    _flashEmitter.particlesPerEmissionMax = 2;
                    _flashEmitter.emissionIntervalMin = 0f;
                    _flashEmitter.emissionIntervalMax = 0.2f * _eParams.flashDuration;
                    _flashEmitter.life = _eParams.flashDuration;
                    _flashEmitter.velocity = Vector3.Zero;
                    _flashEmitter.orientationMin = new Vector3 (0f, 0f, 0f);
                    _flashEmitter.orientationMax = new Vector3 (0f, 0f, 2f * (float)Math.PI);
                    _flashEmitter.billboardXY = true;
                    _flashEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()

                    _flashColorEffector.particleLifetime = _eParams.flashDuration;
                    _flashColorEffector.colorMask = _eParams.flashColor;
                    _flashColorEffector.keyframes.Clear();
                    _flashColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _flashColorEffector.keyframes.Add(1f, new Color4 (1f, 1f, 1f, 0f));

                    _flashScaleEffector.particleLifetime = _eParams.flashDuration;
                    _flashScaleEffector.keyframes.Clear();
                    _flashScaleEffector.keyframes.Add(0f, 1f);
                    _flashScaleEffector.keyframes.Add(1f, 1.5f);
                } else if (_flashEmitter != null) {
                    removeEmitter(_flashEmitter);
                    removeEffector(_flashColorEffector);
                    removeEffector(_flashScaleEffector);
                    _flashEmitter = null;
                    _flashColorEffector = null;
                    _flashScaleEffector = null;
                }

			}

            protected void emitFlash(Vector3 position, Vector3 baseVelocity, float intensity)
			{
                if (!_eParams.doFlash) return;
				_flashEmitter.componentScale = new Vector3(intensity*3f, intensity*3f, 1f);
				ParticlesSphereGenerator flashSphere = _flashEmitter.Field as ParticlesSphereGenerator;
				flashSphere.Center = position;
				flashSphere.Radius = 0.3f * intensity;
                _flashEmitter.velocity = baseVelocity;
				_flashEmitter.totalEmissionsLeft = 2;
			}

			protected void configureFlyingSparks()
			{
                if (_eParams.doFlyingSparks) {
                    if (_flyingSparksEmitter == null) {
                        _flyingSparksEmitter = new SSRadialEmitter ();
                        _flyingSparksColorEffector = new SSColorKeyframesEffector ();
                        addEmitter(_flyingSparksEmitter);
                        addEffector(_flyingSparksColorEffector);
                        _flyingSparksEmitter.effectorMask = _flyingSparksColorEffector.effectorMask
                            = (ushort)ComponentMask.FlyingSparks;
                    }

                    _flyingSparksEmitter.spriteRectangles = _eParams.flyingSparksSprites;
                    _flyingSparksEmitter.emissionIntervalMin = 0f;
                    _flyingSparksEmitter.emissionIntervalMax = 0.1f * _eParams.flyingSparksDuration;
                    _flyingSparksEmitter.life = _eParams.flyingSparksDuration;
                    _flyingSparksEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
                    _flyingSparksEmitter.componentScale = new Vector3 (5f, 1f, 1f);
                    _flyingSparksEmitter.color = _eParams.flyingSparksColor;

                    _flyingSparksColorEffector.colorMask = _eParams.flashColor;
                    _flyingSparksColorEffector.keyframes.Clear();
                    _flyingSparksColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _flyingSparksColorEffector.keyframes.Add(1f, new Color4 (1f, 1f, 1f, 0f));
                    _flyingSparksColorEffector.particleLifetime = _eParams.flyingSparksDuration;
                } else if (_flyingSparksEmitter != null) {
                    removeEmitter(_flyingSparksEmitter);
                    removeEffector(_flyingSparksColorEffector);
                    _flyingSparksEmitter = null;
                    _flyingSparksColorEffector = null;
                }
			}

            protected void emitFlyingSparks(Vector3 position, Vector3 baseVelocity, float intensity)
			{
                if (!_eParams.doFlyingSparks) return;
                _flyingSparksEmitter.center = position;
                _flyingSparksEmitter.velocity = baseVelocity;
                _flyingSparksEmitter.velocityFromCenterMagnitudeMin = intensity * 2f;
				_flyingSparksEmitter.velocityFromCenterMagnitudeMax = intensity * 3f;
				_flyingSparksEmitter.particlesPerEmission = (int)(5.0*Math.Log(intensity));
				_flyingSparksEmitter.totalEmissionsLeft = 1;
			}

			protected void configureSmokeTrails()
			{
                if (_eParams.doSmokeTrails) {
                    if (_smokeTrailsEmitter == null) {
                        _smokeTrailsEmitter = new SSRadialEmitter ();
                        _smokeTrailsColorEffector = new SSColorKeyframesEffector ();
                        _smokeTrailsScaleEffector = new SSComponentScaleKeyframeEffector ();
                        addEmitter(_smokeTrailsEmitter);
                        addEffector(_smokeTrailsColorEffector);
                        addEffector(_smokeTrailsScaleEffector);
                        _smokeTrailsEmitter.effectorMask = _smokeTrailsColorEffector.effectorMask
                            = _smokeTrailsScaleEffector.effectorMask = (ushort)ComponentMask.SmokeTrails;
                    }

                    _smokeTrailsEmitter.radiusOffset = 3f;
                    _smokeTrailsEmitter.spriteRectangles = _eParams.smokeTrailsSprites;
                    _smokeTrailsEmitter.particlesPerEmission = 16;
                    _smokeTrailsEmitter.emissionIntervalMin = 0f;
                    _smokeTrailsEmitter.emissionIntervalMax = 0.1f * _eParams.smokeTrailsDuration;
                    _smokeTrailsEmitter.life = _eParams.smokeTrailsDuration;
                    _smokeTrailsEmitter.totalEmissionsLeft = 0; // control this in ShowExplosion()
                    _smokeTrailsEmitter.color = _eParams.smokeTrailsColor;

                    _smokeTrailsColorEffector.particleLifetime = _eParams.smokeTrailsDuration;
                    _smokeTrailsColorEffector.colorMask = _eParams.smokeTrailsColor;
                    _smokeTrailsColorEffector.keyframes.Clear();
                    _smokeTrailsColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _smokeTrailsColorEffector.keyframes.Add(1f, new Color4 (0.3f, 0.3f, 0.3f, 0f));

                    _smokeTrailsScaleEffector.particleLifetime = _eParams.smokeTrailsDuration;
                    _smokeTrailsScaleEffector.baseOffset = new Vector3 (1f, 1f, 1f);
                    _smokeTrailsScaleEffector.keyframes.Clear();
                    _smokeTrailsScaleEffector.keyframes.Add(0f, new Vector3 (0f));
                    _smokeTrailsScaleEffector.keyframes.Add(0.5f, new Vector3 (12f, 1.5f, 0f));
                    _smokeTrailsScaleEffector.keyframes.Add(1f, new Vector3 (7f, 2f, 0f));
                } else if (_smokeTrailsEmitter != null) {
                    removeEmitter(_smokeTrailsEmitter);
                    removeEffector(_smokeTrailsColorEffector);
                    removeEffector(_smokeTrailsScaleEffector);
                    _smokeTrailsEmitter = null;
                    _smokeTrailsColorEffector = null;
                    _smokeTrailsScaleEffector = null;
                }
			}

			protected void emitSmokeTrails(Vector3 position, Vector3 baseVelocity, float intensity)
			{
                if (!_eParams.doSmokeTrails) return;

				_smokeTrailsEmitter.center = position;
                _smokeTrailsEmitter.velocity = baseVelocity;
                _smokeTrailsEmitter.velocityFromCenterMagnitudeMin = intensity * 0.8f;
				_smokeTrailsEmitter.velocityFromCenterMagnitudeMax = intensity * 1f;
				_smokeTrailsEmitter.particlesPerEmission = (int)(5.0*Math.Log(intensity));
				_smokeTrailsEmitter.totalEmissionsLeft = 1;
				_smokeTrailsEmitter.radiusOffset = intensity;

				_smokeTrailsScaleEffector.amplification = new Vector3(0.3f*intensity, 0.15f*intensity, 0f);
			}

			protected void configureRoundSparks()
			{

                if (_eParams.doRoundSparks) {
                    _roundSparksEmitter = new SSRadialEmitter ();
                    _roundSparksColorEffector = new SSColorKeyframesEffector ();
                    _roundSparksScaleEffector = new SSMasterScaleKeyframesEffector ();
                    addEmitter (_roundSparksEmitter);
                    addEffector (_roundSparksColorEffector);
                    addEffector (_roundSparksScaleEffector);
                    _roundSparksEmitter.effectorMask = _roundSparksScaleEffector.effectorMask 
                        = _roundSparksColorEffector.effectorMask = (ushort)ComponentMask.RoundSparks;

                    _roundSparksEmitter.spriteRectangles = _eParams.roundSparksSprites;
                    _roundSparksEmitter.particlesPerEmission = 6;
                    _roundSparksEmitter.emissionIntervalMin = 0f;
                    _roundSparksEmitter.emissionIntervalMax = 0.05f * _eParams.roundSparksDuration;
                    _roundSparksEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
                    _roundSparksEmitter.life = _eParams.roundSparksDuration;
                    _roundSparksEmitter.billboardXY = true;
                    _roundSparksEmitter.orientationMin = new Vector3 (0f, 0f, 0f);
                    _roundSparksEmitter.orientationMax = new Vector3 (0f, 0f, 2f * (float)Math.PI);
                    _roundSparksEmitter.angularVelocityMin = new Vector3 (0f, 0f, -0.25f);
                    _roundSparksEmitter.angularVelocityMax = new Vector3 (0f, 0f, +0.25f);
                    _roundSparksEmitter.radiusOffsetMin = 0f;
                    _roundSparksEmitter.radiusOffsetMax = 1f;

                    _roundSparksColorEffector.particleLifetime = _eParams.roundSparksDuration;
                    _roundSparksColorEffector.colorMask = _eParams.roundSparksColor;
                    _roundSparksColorEffector.keyframes.Clear();
                    _roundSparksColorEffector.keyframes.Add(0.1f, new Color4 (1f, 1f, 1f, 1f));
                    _roundSparksColorEffector.keyframes.Add(1f, new Color4 (1f, 1f, 1f, 0f));

                    _roundSparksScaleEffector.particleLifetime = _eParams.roundSparksDuration;
                    _roundSparksScaleEffector.keyframes.Clear();
                    _roundSparksScaleEffector.keyframes.Add(0f, 1f);
                    _roundSparksScaleEffector.keyframes.Add(0.25f, 3f);
                    _roundSparksScaleEffector.keyframes.Add(1f, 6f);
                } else if (_roundSparksEmitter != null) {
                    removeEmitter(_roundSparksEmitter);
                    removeEffector(_roundSparksColorEffector);
                    removeEffector(_roundSparksScaleEffector);
                    _roundSparksEmitter = null;
                    _roundSparksColorEffector = null;
                    _roundSparksScaleEffector = null;
                }  
			}

            protected void emitRoundSparks(Vector3 position, Vector3 baseVelocity, float intensity)
			{
                if (!_eParams.doRoundSparks) return;
				_roundSparksEmitter.componentScale = new Vector3(intensity, intensity, 1f);
                _roundSparksEmitter.velocityFromCenterMagnitudeMin = 0.7f * intensity;
				_roundSparksEmitter.velocityFromCenterMagnitudeMax = 1.2f * intensity;
				_roundSparksEmitter.center = position;
                _roundSparksEmitter.velocity = baseVelocity;
				_roundSparksEmitter.totalEmissionsLeft = 3;
			}

			protected void configureDebris()
			{
                if (_eParams.doDebris) {
                    if (_debrisEmitter == null) {
                        _debrisEmitter = new SSRadialEmitter ();
                        _debrisColorEffector = new SSColorKeyframesEffector ();
                        addEmitter (_debrisEmitter);
                        addEffector (_debrisColorEffector);
                        _debrisEmitter.effectorMask = _debrisColorEffector.effectorMask
                            = (ushort)ComponentMask.Debris;
                    }
                    _debrisEmitter.spriteRectangles = _eParams.debrisSprites;
                    _debrisEmitter.particlesPerEmissionMin = 7;
                    _debrisEmitter.particlesPerEmissionMax = 10;
                    _debrisEmitter.totalEmissionsLeft = 0; // Control this in ShowExplosion()
                    _debrisEmitter.life = _eParams.debrisDuration;
                    _debrisEmitter.orientationMin = new Vector3 (0f, 0f, 0f);
                    _debrisEmitter.orientationMax = new Vector3 (0f, 0f, 2f * (float)Math.PI);
                    _debrisEmitter.billboardXY = true;
                    _debrisEmitter.angularVelocityMin = new Vector3 (0f, 0f, -0.5f);
                    _debrisEmitter.angularVelocityMax = new Vector3 (0f, 0f, +0.5f);
                    _debrisEmitter.radiusOffsetMin = 0f;
                    _debrisEmitter.radiusOffsetMax = 1f;

                    var debrisColorFinal = new Color4 (_eParams.debrisColorEnd.R, 
                        _eParams.debrisColorEnd.G, _eParams.debrisColorEnd.B, 0f);
                    _debrisColorEffector.particleLifetime = _eParams.debrisDuration;
                    _debrisColorEffector.keyframes.Clear();
                    _debrisColorEffector.keyframes.Add(0f, _eParams.debrisColorStart);
                    _debrisColorEffector.keyframes.Add(0.3f, _eParams.debrisColorEnd);
                    _debrisColorEffector.keyframes.Add(1f, debrisColorFinal);
                } else if (_debrisEmitter != null) {
                    removeEmitter(_debrisEmitter);
                    removeEffector(_debrisColorEffector);
                    _debrisEmitter = null;
                    _debrisColorEffector = null;
                }
			}

            protected void emitDebris(Vector3 position, Vector3 baseVelocity, float intensity)
			{
                if (!_eParams.doDebris) return;
				//m_debrisEmitter.MasterScale = intensity / 2f;
				_debrisEmitter.masterScaleMin = 3f;
				_debrisEmitter.masterScaleMax = 0.4f*intensity;
                _debrisEmitter.velocityFromCenterMagnitudeMin = 1f * intensity;
				_debrisEmitter.velocityFromCenterMagnitudeMax = 3f * intensity;
				_debrisEmitter.center = position;
                _debrisEmitter.velocity = baseVelocity;
				_debrisEmitter.particlesPerEmission = (int)(2.5*Math.Log(intensity));
				_debrisEmitter.totalEmissionsLeft = 1;
			}

			protected void configureShockwave()
			{
                if (_eParams.doShockwave) {
                    if (_shockwaveEmitter == null) {
                        _shockwaveEmitter = new ShockwaveEmitter ();
                        _shockwaveColorEffector = new SSColorKeyframesEffector ();
                        _shockwaveScaleEffector = new SSMasterScaleKeyframesEffector ();
                        addEmitter(_shockwaveEmitter);
                        addEffector(_shockwaveScaleEffector);
                        addEffector(_shockwaveColorEffector);
                        _shockwaveEmitter.effectorMask = _shockwaveScaleEffector.effectorMask
                            = _shockwaveColorEffector.effectorMask = (ushort)ComponentMask.Shockwave;
                    }
                    _shockwaveEmitter.spriteRectangles = _eParams.shockwaveSprites;
                    _shockwaveEmitter.particlesPerEmission = 1;
                    _shockwaveEmitter.totalEmissionsLeft = 0;   // Control this in ShowExplosion()
                    _shockwaveEmitter.life = _eParams.shockwaveDuration;
                    _shockwaveEmitter.velocity = Vector3.Zero;

                    _shockwaveScaleEffector.particleLifetime = _eParams.shockwaveDuration;
                    _shockwaveScaleEffector.keyframes.Clear();
                    _shockwaveScaleEffector.keyframes.Add(0f, 0f);
                    _shockwaveScaleEffector.keyframes.Add(_eParams.shockwaveDuration, 7f);

                    _shockwaveColorEffector.colorMask = _eParams.shockwaveColor;
                    _shockwaveColorEffector.particleLifetime = _eParams.shockwaveDuration;
                    _shockwaveColorEffector.keyframes.Clear();
                    _shockwaveColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _shockwaveColorEffector.keyframes.Add(1f, new Color4 (1f, 1f, 1f, 0f));
                } else if (_shockwaveEmitter != null) {
                    removeEmitter(_shockwaveEmitter);
                    removeEffector(_shockwaveColorEffector);
                    removeEffector(_shockwaveScaleEffector);
                    _shockwaveEmitter = null;
                    _shockwaveColorEffector = null;
                    _shockwaveScaleEffector = null;
                }
			}

            protected void emitShockwave(Vector3 position, Vector3 baseVelocity, float intensity)
			{
                if (!_eParams.doShockwave) return;
				_shockwaveEmitter.componentScale = new Vector3(intensity, intensity, 1f);
				_shockwaveEmitter.position = position;
                _shockwaveEmitter.velocity = baseVelocity;
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

