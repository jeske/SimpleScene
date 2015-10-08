using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
    public class SSpaceMissilesRenderManager
    {
        protected readonly SSpaceMissilesVisualSimulation _simulation;
        protected readonly SSScene _objScene;

        protected readonly SSParticleSystemData _particlesData;
        protected readonly SSInstancedMeshRenderer _particleRenderer;

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

            _particlesData = new SSParticleSystemData (particleCapacity);
            _particleRenderer = new SSInstancedMeshRenderer (
                _particlesData, SSTexturedQuad.SingleFaceInstance);
            _particleRenderer.renderState.alphaBlendingOn = true;
            _particleRenderer.renderState.castsShadow = false;
            _particleRenderer.renderState.receivesShadows = false;
            //_particleRenderer.renderMode = SSInstancedMeshRenderer.RenderMode.CpuFallback;
            //_particleRenderer.renderMode = SSInstancedMeshRenderer.RenderMode.GpuInstancing;

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
            if (_particleRenderer.textureMaterial == null) {
                _particleRenderer.textureMaterial = new SSTextureMaterial (clusterParams.particlesTexture);
            }
            var cluster = _simulation.launchCluster(launchPos, launchVel, numMissiles,
              target, timeToHit, clusterParams);
            foreach (var missile in cluster.missiles) {
                _addMissile(missile);
            }
            return cluster;
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
                smokeEmitter.color = Color4.Red;
                smokeEmitter.billboardXY = true;
                smokeEmitter.emissionInterval = 0.1f;
                smokeEmitter.spriteRectangles = mParams.flameSmokeSpriteRects;

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



