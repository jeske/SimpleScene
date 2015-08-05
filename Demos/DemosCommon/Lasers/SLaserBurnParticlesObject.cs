using System;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

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
            renderState.alphaBlendingOn = true;
            renderState.depthTest = true;
            renderState.depthWrite = false;
            renderState.lighted = false;

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
                    {
                        var newFlashEmitter = new SSRadialEmitter();
                        newFlashEmitter.velocity = Vector3.Zero;
                        newFlashEmitter.billboardXY = true;
                        newFlashEmitter.spriteRectangles = laserParams.flashSpriteRects;
                        var flashColor = laserParams.overlayColor;
                        flashColor.A = _laser.envelopeIntensity * beam.periodicIntensity;
                        newFlashEmitter.color = flashColor;
                        newFlashEmitter.emissionInterval = 1f / laserParams.flashEmitFrequency;
                        newFlashEmitter.masterScaleMin = laserParams.flashScaleMin;
                        newFlashEmitter.masterScaleMax = laserParams.flashScaleMax;
                        _flashEmitters[i] = newFlashEmitter;
                    }
                    {
                        var newFlameSmokeEmitter = new SSRadialEmitter();
                        newFlameSmokeEmitter.billboardXY = true;
                        newFlameSmokeEmitter.spriteRectangles = laserParams.flameSmokeSpriteRects;
                        var flameSmokeColor = laserParams.backgroundColor;
                        flameSmokeColor.A = _laser.envelopeIntensity * beam.periodicIntensity;
                        newFlameSmokeEmitter.color = flameSmokeColor;
                        newFlameSmokeEmitter.emissionInterval = 1f / laserParams.flameSmokeEmitFrequency;
                        newFlameSmokeEmitter.masterScaleMin = laserParams.flameSmokeScaleMin;
                        newFlameSmokeEmitter.masterScaleMax = laserParams.flameSmokeScaleMax;
                        _smokeEmitters[i] = newFlameSmokeEmitter;
                    }
                }
            }

            public List<SSParticleEmitter> emitters()
            {
                var ret = new List<SSParticleEmitter> (_flashEmitters);
                ret.AddRange(_smokeEmitters);
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
            }

        }
    }
}

