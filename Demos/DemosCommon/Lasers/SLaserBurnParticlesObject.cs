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
        public new SLaserBurnParticleSystem particleSystem {
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

            simulateOnUpdate = true;

            base.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
            base.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
            base.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.ShininessMatColor = 0f;

            var tex = texture ?? SLaserBurnParticleSystem.getDefaultTexture();
            base.textureMaterial = new SSTextureMaterial(null, null, tex, null);
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

        protected enum ComponentMask : ushort { 
            FlameSmoke = 0x1, 
            Flash = 0x2,
        };

        public static SSTexture getDefaultTexture()
        {
            return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7.png");
            //return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7_debug.png");
        }

        protected readonly Dictionary<SLaser, HitSpotData> _hitSpots 
            = new Dictionary<SLaser, HitSpotData>();

        public SLaserBurnParticleSystem(int particleCapacity)
            : base(particleCapacity)
        {
        }

        public void addHitSpots(SLaser laser)
        {
            var newHitSpot = new HitSpotData (laser);
            _hitSpots.Add(laser, newHitSpot);
            foreach (var emitter in newHitSpot.emitters()) {
                base.addEmitter(emitter);
            }
            foreach (var effector in newHitSpot.effectors()) {
                base.addEffector(effector);
            }
        }

        public void removeHitSpots(SLaser laser)
        {
            var hitSpot = _hitSpots [laser];
            foreach (var emitter in hitSpot.emitters()) {
                base.removeEmitter(emitter);
            }
            _hitSpots.Remove(laser);
        }

        public override void updateCamera (ref Matrix4 model, ref Matrix4 view, 
                                           ref Matrix4 projection)
        {
            //base.updateCamera(ref model, ref view, ref projection);
            foreach (var hitSpot in _hitSpots.Values) {
                hitSpot.updateLaserBeamData(ref model);
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

            public HitSpotData(SLaser laser)
            {
                // TODO generate byte mask from hit spot id
                _laser = laser;
                int numBeams = _laser.parameters.numBeams;

                // initialize emitters
                _flashEmitters = new SSRadialEmitter[numBeams];
                _smokeEmitters = new SSRadialEmitter[numBeams];
                var laserParams = _laser.parameters;

                for (int i = 0; i < numBeams; ++i) {
                    var beam = laser.beam(i);
                    // hit spot flame/smoke
                    {
                        var newFlameSmokeEmitter = new SSRadialEmitter();
                        newFlameSmokeEmitter.effectorMask = (ushort)ComponentMask.FlameSmoke;
                        newFlameSmokeEmitter.billboardXY = true;
                        newFlameSmokeEmitter.spriteRectangles = laserParams.flameSmokeSpriteRects;
                        newFlameSmokeEmitter.emissionInterval = 1f / laserParams.flameSmokeEmitFrequency;
                        newFlameSmokeEmitter.masterScaleMin = laserParams.flameSmokeScaleMin;
                        newFlameSmokeEmitter.masterScaleMax = laserParams.flameSmokeScaleMax;
                        newFlameSmokeEmitter.particlesPerEmission = 0; // init to 0 to not emit until updated
                        newFlameSmokeEmitter.life = laserParams.flameSmokeLifetime;
                        _smokeEmitters[i] = newFlameSmokeEmitter;
                    }
                    // hit spot flash
                    {
                        var newFlashEmitter = new SSRadialEmitter();
                        newFlashEmitter.effectorMask = (ushort)ComponentMask.Flash;
                        newFlashEmitter.velocity = Vector3.Zero;
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
                    var flameSmokeDuration = laserParams.flameSmokeLifetime;
                    _flamesSmokeColorEffector = new SSColorKeyframesEffector ();
                    _flamesSmokeColorEffector.effectorMask = (ushort)ComponentMask.FlameSmoke;
                    _flamesSmokeColorEffector.keyframes.Clear();
                    _flamesSmokeColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _flamesSmokeColorEffector.keyframes.Add(0.4f * flameSmokeDuration, new Color4 (0f, 0f, 0f, 0.5f));
                    _flamesSmokeColorEffector.keyframes.Add(flameSmokeDuration, new Color4 (0f, 0f, 0f, 0f));
                    _flamesSmokeColorEffector.particleLifetime = laserParams.flameSmokeLifetime;
                }

                {
                    // laser-specific flash effector
                    var flashDuration = laserParams.flashLifetime;
                    _flashColorEffector = new SSColorKeyframesEffector ();
                    _flashColorEffector.effectorMask = (ushort)ComponentMask.Flash;
                    _flashColorEffector.keyframes.Clear();
                    _flashColorEffector.keyframes.Add(0f, new Color4 (1f, 1f, 1f, 1f));
                    _flashColorEffector.keyframes.Add(flashDuration, new Color4 (1f, 1f, 1f, 0f));
                    _flashColorEffector.particleLifetime = laserParams.flameSmokeLifetime;
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

            public void updateLaserBeamData(ref Matrix4 rendererWorldMat)
            {
                for (int i = 0; i < _laser.parameters.numBeams; ++i) {
                    var beam = _laser.beam(i);
                    // TODO need intersection location
                    if (beam.hitsAnObstacle) {
                        var hitPos = Vector3.Transform(beam.endPos, rendererWorldMat);
                        foreach (var flashEmitter in _flashEmitters) {
                            flashEmitter.center = hitPos;
                            flashEmitter.particlesPerEmission = 5;
                        }
                        foreach (var smokeEmitter in _smokeEmitters) {
                            smokeEmitter.center = hitPos;
                            smokeEmitter.particlesPerEmission = 5;
                        }
                    } else {
                        foreach (var flashEmitter in _flashEmitters) {
                            flashEmitter.particlesPerEmission = 0;
                        }
                        foreach (var smokeEmitter in _smokeEmitters) {
                            smokeEmitter.particlesPerEmission = 0;
                        }
                    }
                }
                _flamesSmokeColorEffector.colorMask = _laser.parameters.backgroundColor;
                _flamesSmokeColorEffector.colorMask.A = _laser.envelopeIntensity;

                _flashColorEffector.colorMask = _laser.parameters.overlayColor;
                _flashColorEffector.colorMask.A = _laser.envelopeIntensity;
            }
        }
    }
}

