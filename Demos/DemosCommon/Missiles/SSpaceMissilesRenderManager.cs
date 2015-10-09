using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    public class SSpaceMissilesRenderManager
    {
        enum ParticleEffectorMasks : ushort { Smoke=0 };

        protected readonly SSpaceMissilesVisualSimulation _simulation;
        protected readonly SSScene _objScene;

        protected readonly SSParticleSystemData _particlesData;
        protected readonly SSInstancedMeshRenderer _particleRenderer;

        protected SSColorKeyframesEffector _smokeColorEffector = null;
        protected SSMasterScaleKeyframesEffector _smokeScaleEffector = null;

        protected readonly Dictionary<SSpaceMissileData, SSpaceMissileRenderInfo>
            _missilesRuntimes = new Dictionary<SSpaceMissileData, SSpaceMissileRenderInfo>();

        public SSpaceMissilesVisualSimulation simulation { get { return _simulation; } }

        public SSpaceMissilesRenderManager (SSScene objScene, SSScene particleScene, 
                                            int particleCapacity = 2000)
        {
            _simulation = new SSpaceMissilesVisualSimulation ();

            _objScene = objScene;
            objScene.preRenderHooks += _preRenderUpdate;
            objScene.preUpdateHooks += _simulation.updateVisualization;

            // particle system
            _particlesData = new SSParticleSystemData (particleCapacity);
            _particleRenderer = new SSInstancedMeshRenderer (
                _particlesData, SSTexturedQuad.SingleFaceInstance);
            _particleRenderer.renderState.alphaBlendingOn = true;
            _particleRenderer.renderState.castsShadow = false;
            _particleRenderer.renderState.receivesShadows = false;
            _particleRenderer.renderState.depthTest = true;
            _particleRenderer.renderState.depthWrite = false;
            //_particleRenderer.renderMode = SSInstancedMeshRenderer.RenderMode.CpuFallback;
            //_particleRenderer.renderMode = SSInstancedMeshRenderer.RenderMode.GpuInstancing;

            _particleRenderer.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
            _particleRenderer.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
            _particleRenderer.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
            _particleRenderer.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
            _particleRenderer.ShininessMatColor = 0f;

            particleScene.AddObject(_particleRenderer);
        }

        ~SSpaceMissilesRenderManager()
        {
            foreach (var missile in _missilesRuntimes.Keys) {
                _removeMissile(missile);
            }
            _particleRenderer.renderState.toBeDeleted = true;
        }

        public SSpaceMissileClusterData launchCluster(
            Vector3 launchPos, Vector3 launchVel, int numMissiles,
            ISSpaceMissileTarget target, float timeToHit,
            SSpaceMissileVisualParameters clusterParams)
        {
            _initParamsSpecific(clusterParams);
            var cluster = _simulation.launchCluster(launchPos, launchVel, numMissiles,
                                                    target, timeToHit, clusterParams);
            foreach (var missile in cluster.missiles) {
                _addMissile(missile);
            }
            return cluster;
        }

        protected void _initParamsSpecific(SSpaceMissileVisualParameters mParams)
        {
            // smoke effectors
            if (_particleRenderer.textureMaterial == null) {
                _particleRenderer.textureMaterial = new SSTextureMaterial (mParams.particlesTexture);
            }
            if (_smokeColorEffector == null) {
                _smokeColorEffector = new SSColorKeyframesEffector ();
                _smokeColorEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                _smokeColorEffector.effectorMask = (ushort)ParticleEffectorMasks.Smoke;
                _smokeColorEffector.particleLifetime = mParams.smokeDuration;
                //_smokeColorEffector.colorMask = ;
                _smokeColorEffector.keyframes.Clear();
                _smokeColorEffector.keyframes.Add(0f, mParams.innerFlameColor);
                var flameColor = mParams.outerFlameColor;
                flameColor.A = 0.7f;
                _smokeColorEffector.keyframes.Add(0.15f, flameColor);
                var smokeColor = mParams.smokeColor;
                smokeColor.A = 0.2f;
                _smokeColorEffector.keyframes.Add(0.3f, smokeColor);
                smokeColor.A = 0f;
                _smokeColorEffector.keyframes.Add(1f, smokeColor);

                _particlesData.addEffector(_smokeColorEffector);
            }
            if (_smokeScaleEffector == null) {
                _smokeScaleEffector = new SSMasterScaleKeyframesEffector ();
                _smokeScaleEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                _smokeScaleEffector.effectorMask = (ushort)ParticleEffectorMasks.Smoke;
                _smokeScaleEffector.particleLifetime = mParams.smokeDuration;

                _smokeScaleEffector.particleLifetime = mParams.smokeDuration;
                _smokeScaleEffector.keyframes.Clear();
                _smokeScaleEffector.keyframes.Add(0f, 1f);
                _smokeScaleEffector.keyframes.Add(1f, 3f);

                _particlesData.addEffector(_smokeScaleEffector);
            }
        }

        // TODO: remove cluster??? or missile?

        protected void _addMissile(SSpaceMissileData missile)
        {
            var missileRuntime = new SSpaceMissileRenderInfo (missile);
            _objScene.AddObject(missileRuntime.bodyObj);
            _particlesData.addEmitter(missileRuntime.smokeEmitter);
            _missilesRuntimes [missile] = missileRuntime;
        }

        protected void _removeMissile(SSpaceMissileData missile)
        {
            var missileRuntime = _missilesRuntimes [missile];
            _missilesRuntimes.Remove(missile);

            missileRuntime.bodyObj.renderState.toBeDeleted = true;
            _particlesData.removeEmitter(missileRuntime.smokeEmitter);
            
        }

        protected void _preRenderUpdate(float timeElapsed)
        {
            foreach (var missile in _missilesRuntimes.Values) {
                missile.preRenderUpdate(timeElapsed);
            }
        }

        protected class SSpaceMissileRenderInfo
        {
            public readonly SSObjectMesh bodyObj;
            public readonly SSRadialEmitter smokeEmitter;
            protected SSpaceMissileData _missile;

            public SSpaceMissileRenderInfo(SSpaceMissileData missile)
            {
                _missile = missile;
                var mParams = missile.cluster.parameters;
                bodyObj = new SSObjectMesh(mParams.missileMesh);
                bodyObj.Scale = new Vector3(mParams.missileScale);
                bodyObj.renderState.castsShadow = false;
                bodyObj.renderState.receivesShadows = false;

                smokeEmitter = new SSRadialEmitter();
                smokeEmitter.effectorMask = (ushort)ParticleEffectorMasks.Smoke;
                smokeEmitter.life = mParams.smokeDuration;
                smokeEmitter.color = new Color4(1f, 1f, 1f, 0f);
                smokeEmitter.billboardXY = true;
                smokeEmitter.emissionInterval = 1f / mParams.smokeEmissionFrequency;
                smokeEmitter.spriteRectangles = mParams.flameSmokeSpriteRects;
                smokeEmitter.velocityMagnitude = 1f;
                smokeEmitter.particlesPerEmission = 3;

                // positions emitters and mesh
                preRenderUpdate(0f);
            }

            public void preRenderUpdate(float timeElapsed)
            {
                var mParams = _missile.cluster.parameters;

                bodyObj.Pos = _missile.position;

                bodyObj.Orient(_missile.direction, _missile.up);

                smokeEmitter.center = _missile.position
                    - _missile.direction * mParams.missileScale * mParams.jetPosition;
            }
        }
    }
}



