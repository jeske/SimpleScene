using System;
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

        public SLaserHitParticleSystem(int particleCapacity)
            : base(particleCapacity)
        {

        }

        public void addHitSpots(SLaser laser)
        {
            // add emitters, etc
        }

        public void removeHitSpots(SLaser laser)
        {

        }

        public void updateHitSpots(SLaser laser)
        {
            // TODO :
            // update flash location
            // update smoke emitter location
        }

    }
}

