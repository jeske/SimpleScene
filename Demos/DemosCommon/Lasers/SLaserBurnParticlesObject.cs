using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SLaserBurnParticlesObject : SSInstancedMeshRenderer
    {
        public  SLaserBurnParticleSystem particleSystem {
            get { return base.instanceData as SLaserBurnParticleSystem; }
        }

        public SLaserBurnParticlesObject (int particleCapacity = 100, SSTexture texture = null)
            : base(new SLaserBurnParticleSystem (particleCapacity), 
                   SSTexturedQuad.DoubleFaceInstance, _defaultUsageHint)
        {
            renderState.castsShadow = false;
            renderState.receivesShadows = false;
            renderState.doBillboarding = false;
            renderState.depthTest = true;
            renderState.depthWrite = false;
            renderState.lighted = false;

            renderState.alphaBlendingOn = true;
            renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            renderState.blendFactorDest = BlendingFactorDest.One;

            // bypass frustum culling so camera updates are sent to activate particle emission
            renderState.frustumCulling = false;

            base.selectable = false;
            base.simulateOnUpdate = true;

            base.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
            base.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
            base.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.ShininessMatColor = 0f;

            base.textureMaterial = new SSTextureMaterial(null, null, texture, null);
        }
    }

    /// <summary>
    /// Particle system for laser hit effects in 3d. Can be used as a shared "singleton" instance 
    /// of a particle system for all laser effects, or could be instantiated for each beam? or each laser?
    /// </summary>
    public class SLaserBurnParticleSystem : SSParticleSystemData
    {
        // TODO consider making this a particle system for all laser-related 3d rendering
        // including middle section

        protected readonly Dictionary<SLaser, HitSpotData> _hitSpots 
            = new Dictionary<SLaser, HitSpotData>();
        protected readonly SSColorKeyframesEffector _globalSmokeDimmerEffector;
        protected readonly SSMasterScaleKeyframesEffector _globalSmokeScaleEffector;

        protected ushort _nextEffectorMask = 2;

        public SLaserBurnParticleSystem(int particleCapacity)
            : base(particleCapacity)
        {
            this.simulationStep = 0.1f;

            // global flame/smoke effector to dim smoke particles of already-released laser hit spots
            _globalSmokeDimmerEffector = new SSColorKeyframesEffector ();
            _globalSmokeDimmerEffector.effectorMask = 1;
            _globalSmokeDimmerEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
            _globalSmokeDimmerEffector.particleLifetime = 0f;
            _globalSmokeDimmerEffector.colorMask = Color4.LightGray;
            _globalSmokeDimmerEffector.keyframes.Clear();
            _globalSmokeDimmerEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
            _globalSmokeDimmerEffector.keyframes.Add(0.4f, new Color4 (0f, 0f, 0f, 1f));
            _globalSmokeDimmerEffector.keyframes.Add(1f, new Color4 (0f, 0f, 0f, 0f));
            addEffector(_globalSmokeDimmerEffector);

            _globalSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
            _globalSmokeScaleEffector.effectorMask = 0x1; // all smoke
            _globalSmokeScaleEffector.maskMatchFunction = SSParticleEffector.MatchFunction.And; // all smoke
            _globalSmokeScaleEffector.keyframes.Clear ();
            _globalSmokeScaleEffector.keyframes.Add (0f, 0.1f);
            _globalSmokeScaleEffector.keyframes.Add (0.25f, 1f);
            _globalSmokeScaleEffector.keyframes.Add (1f, 1.2f);
            addEffector(_globalSmokeScaleEffector);
        }

        public void addHitSpots(SLaser laser)
        {
            var newHitSpot = new HitSpotData (laser, (ushort)(_nextEffectorMask + 1), _nextEffectorMask);
            _nextEffectorMask += 2;
            if (_nextEffectorMask == 0) { // 0 is reserved for the global smoke dimmer
                _nextEffectorMask = 2;
            }
            _hitSpots.Add(laser, newHitSpot);
            foreach (var emitter in newHitSpot.emitters()) {
                base.addEmitter(emitter);
            }
            foreach (var effector in newHitSpot.effectors()) {
                base.addEffector(effector);
            }

            // update global smoke smoke effectors with particle lifetime
            float newFlameSmokeDuration = laser.parameters.flameSmokeLifetime;
            _globalSmokeDimmerEffector.particleLifetime = newFlameSmokeDuration;
            _globalSmokeScaleEffector.particleLifetime = newFlameSmokeDuration;
        }

        public void removeHitSpots(SLaser laser)
        {
            var hitSpot = _hitSpots [laser];
            foreach (var emitter in hitSpot.emitters()) {
                base.removeEmitter(emitter);
            }
            foreach (var effector in hitSpot.effectors()) {
                base.removeEffector(effector);
            }
            _hitSpots.Remove(laser);
        }

        public override void updateCamera (ref Matrix4 model, ref Matrix4 view, ref Matrix4 projection)
        {
            Matrix4 modelViewInv = (model * view).Inverted();
            Vector3 cameraPosLocal = Vector3.Transform(Vector3.Zero, modelViewInv);

            foreach (var hitspot in _hitSpots.Values) {
                hitspot.updateCamera(ref model, ref cameraPosLocal);
            }
        }

        protected class HitSpotData
        {
            // ID?
            protected SSRadialEmitter[] _flashEmitters;
            protected SSRadialEmitter[] _smokeEmitters;
            protected SSColorKeyframesEffector _flamesSmokeColorEffector = null;
            protected SSColorKeyframesEffector _flashColorEffector = null;

            protected SLaser _laser;

            public HitSpotData(SLaser laser, ushort flameEffectorMask, ushort flashEffectorMask)
            {
                _laser = laser;
                int numBeams = _laser.parameters.numBeams;

                // initialize emitters
                _flashEmitters = new SSRadialEmitter[numBeams];
                _smokeEmitters = new SSRadialEmitter[numBeams];
                var laserParams = _laser.parameters;

                for (int i = 0; i < numBeams; ++i) {
                    // hit spot flame/smoke
                    {
                        var newFlameSmokeEmitter = new SSRadialEmitter();
                        newFlameSmokeEmitter.effectorMask = flameEffectorMask;
                        newFlameSmokeEmitter.billboardXY = true;
                        newFlameSmokeEmitter.phiMin = (float)Math.PI/3f;
                        newFlameSmokeEmitter.phiMax = (float)Math.PI/2f;
                        newFlameSmokeEmitter.spriteRectangles = laserParams.flameSmokeSpriteRects;
                        newFlameSmokeEmitter.emissionInterval = 1f / laserParams.flameSmokeEmitFrequency;
                        newFlameSmokeEmitter.componentScaleMin = new Vector3(laserParams.flameSmokeScaleMin);
                        newFlameSmokeEmitter.componentScaleMax = new Vector3(laserParams.flameSmokeScaleMax);
                        newFlameSmokeEmitter.particlesPerEmission = 0; // init to 0 to not emit until updated
                        newFlameSmokeEmitter.life = laserParams.flameSmokeLifetime;
                        newFlameSmokeEmitter.velocityMagnitudeMin = laserParams.flameSmokeRadialVelocityMin;
                        newFlameSmokeEmitter.velocityMagnitudeMax = laserParams.flameSmokeRadialVelocityMax;
                        _smokeEmitters[i] = newFlameSmokeEmitter;
                    }
                    // hit spot flash
                    {
                        var newFlashEmitter = new SSRadialEmitter();
                        newFlashEmitter.effectorMask = flashEffectorMask;
                        newFlashEmitter.velocityMagnitude = 0f;
                        newFlashEmitter.billboardXY = true;
                        newFlashEmitter.spriteRectangles = laserParams.flashSpriteRects;
                        newFlashEmitter.emissionInterval = 1f / laserParams.flashEmitFrequency;
                        newFlashEmitter.masterScaleMin = laserParams.flashScaleMin;
                        newFlashEmitter.masterScaleMax = laserParams.flashScaleMax;
                        newFlashEmitter.particlesPerEmission = 0; // init to 0 to not emit until updated
                        newFlashEmitter.life = laserParams.flashLifetime;
                        _flashEmitters[i] = newFlashEmitter;
                    }
                }

                {
                   // laser-specific flame/smoke effector
                    _flamesSmokeColorEffector = new SSColorKeyframesEffector ();
                    _flamesSmokeColorEffector.effectorMask = flameEffectorMask;
                    _flamesSmokeColorEffector.preRemoveHook = _releaseSmokeParticle;
                    _flamesSmokeColorEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                    _flamesSmokeColorEffector.keyframes.Clear();
                    _flamesSmokeColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _flamesSmokeColorEffector.keyframes.Add(0.4f, new Color4 (0f, 0f, 0f, 1f));
                    _flamesSmokeColorEffector.keyframes.Add(1f, new Color4 (0f, 0f, 0f, 0f));
                    _flamesSmokeColorEffector.particleLifetime = laserParams.flameSmokeLifetime;
                }

                {
                    // laser-specific flash effector
                    _flashColorEffector = new SSColorKeyframesEffector ();
                    _flashColorEffector.effectorMask = flashEffectorMask;
                    _flashColorEffector.preRemoveHook = _releaseFlashParticle;
                    _flashColorEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                    _flashColorEffector.keyframes.Clear();
                    _flashColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 0.5f));
                    _flashColorEffector.keyframes.Add(1f, new Color4 (1f, 1f, 1f, 0f));
                    _flashColorEffector.particleLifetime = laserParams.flashLifetime;
                }
            }

            public List<SSParticleEmitter> emitters()
            {
                var ret = new List<SSParticleEmitter> (_flashEmitters);
                ret.AddRange(_smokeEmitters);
                return ret;
            }

            public List<SSParticleEffector> effectors()
            {
                var ret = new List<SSParticleEffector> ();
                ret.Add(_flamesSmokeColorEffector);
                ret.Add(_flashColorEffector);
                return ret;
            }

            public void updateCamera(ref Matrix4 rendererWorldMat, ref Vector3 cameraPosLocal)
            {
                var laserParams = _laser.parameters;
                for (int i = 0; i < laserParams.numBeams; ++i) {
                    var beam = _laser.beam(i);
                    var flashEmitter = _flashEmitters [i];
                    var smokeEmitter = _smokeEmitters [i];
                    // TODO need intersection location
                    if (beam.hitsAnObstacle) {
                        var hitPos = Vector3.Transform(beam.endPos, rendererWorldMat);
                        flashEmitter.center = hitPos;
                        flashEmitter.particlesPerEmissionMin = laserParams.flashParticlesPerEmissionMin;
                        flashEmitter.particlesPerEmissionMax = laserParams.flashParticlesPerEmissionMax;
                        smokeEmitter.center = hitPos;
                        smokeEmitter.up = (cameraPosLocal - smokeEmitter.center).Normalized();
                        smokeEmitter.particlesPerEmissionMin = laserParams.flameSmokeParticlesPerEmissionMin;
                        smokeEmitter.particlesPerEmissionMax = laserParams.flameSmokeParticlesPerEmissionMax;
                    } else {
                        flashEmitter.particlesPerEmission = 0;
                        smokeEmitter.particlesPerEmission = 0;
                    }
                }
                _flamesSmokeColorEffector.colorMask = _laser.parameters.backgroundColor;
                _flamesSmokeColorEffector.colorMask.A = _laser.envelopeIntensity;

                _flashColorEffector.colorMask = _laser.parameters.overlayColor;
                _flashColorEffector.colorMask.A = _laser.envelopeIntensity;
            }

            protected void _releaseSmokeParticle(SSParticle particle)
            {
                particle.effectorMask = 1; // mark to be controlled by the global smoke dimming effector
                particle.life = Math.Min(particle.life, 1.5f);
            }

            protected void _releaseFlashParticle(SSParticle particle)
            {
                particle.life = 0.1f;
            }
        }
    }
}

