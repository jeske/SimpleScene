using System;
using System.Collections.Generic;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    public class SLaserHitParticlesObject : SSInstancedMeshRenderer
    {
        new SLaserHitParticleSystem particleSystem {
            get { return base.instanceData as SLaserHitParticleSystem; }
        }

        public SLaserHitParticlesObject (int particleCapacity = 100, SSTexture texture = null)
            : base(new SLaserHitParticleSystem (particleCapacity), 
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

            var tex = texture ?? SLaserHitParticleSystem.getDefaultTexture();
            base.textureMaterial = new SSTextureMaterial(null, null, tex, null);
        }
    }

    /// <summary>
    /// Particle system for laser hit effects in 3d. Can be used as a shared "singleton" instance 
    /// of a particle system for all laser effects, or could be instantiated for each beam? or each laser?
    /// </summary>
    public class SLaserHitParticleSystem : SSParticleSystemData
    {
        public static SSTexture getDefaultTexture()
        {
            //return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7.png");
            return SSAssetManager.GetInstance<SSTextureWithAlpha> ("explosions", "fig7_debug.png");
        }

        protected Dictionary<SLaser, HitSpotData> _hitSpots;

        public SLaserHitParticleSystem(int particleCapacity)
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
        }

        public void updateHitSpots()
        {
            foreach (var hitSpot in _hitSpots.Values) {
                hitSpot.updateLaserBeamData();
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
                for (int i = 0; i < numBeams; ++i) {
                    {
                        var newFlashEmitter = new SSRadialEmitter();

                        _flashEmitters[i] = newFlashEmitter;
                    }
                    {
                        var newSmokeEmitter = new SSRadialEmitter();
                        _smokeEmitters[i] = newSmokeEmitter;
                    }
                }
            }

            public List<SSParticleEmitter> emitters()
            {
                var ret = new List<SSParticleEmitter> (_flashEmitters);
                ret.AddRange(_smokeEmitters);
                return ret;
            }

            public void updateLaserBeamData()
            {
                for (int i = 0; i < _laser.parameters.numBeams; ++i) {
                    var beam = _laser.beam(i);
                    // TODO need intersection location
                    // update flash positioning
                    // update smoke positioning
                }
            }

        }
    }
}

