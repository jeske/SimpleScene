#define MISSILE_DEBUG

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util;

#if MISSILE_DEBUG
using System.Drawing; // RectangleF
#endif

namespace SimpleScene.Demos
{
    public class SSpaceMissilesRenderManager
    {
        enum ParticleEffectorMasks : ushort { Smoke=0 };

        protected readonly SSpaceMissilesVisualSimulation _simulation;
        protected readonly SSScene _objScene;
        protected readonly SSScene _screenScene;

        protected readonly SSParticleSystemData _particlesData;
        protected readonly SSInstancedMeshRenderer _particleRenderer;

        protected SSColorKeyframesEffector _smokeColorEffector = null;
        protected SSMasterScaleKeyframesEffector _smokeScaleEffector = null;

        readonly List<SSpaceMissileRenderInfo> _missileRuntimes = new List<SSpaceMissileRenderInfo> ();

        public SSpaceMissilesVisualSimulation simulation { get { return _simulation; } }

        public SSpaceMissilesRenderManager (SSScene objScene, SSScene particleScene, SSScene screenScene,
                                            int particleCapacity = 2000)
        {
            _simulation = new SSpaceMissilesVisualSimulation ();

            _objScene = objScene;
            objScene.preRenderHooks += _preRenderUpdate;
            objScene.preUpdateHooks += _simulation.updateSimulation;

            _screenScene = screenScene;

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
            foreach (var missile in _missileRuntimes) {
                _removeMissileRender(missile);
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
                _addMissileRender(missile);
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
                _smokeColorEffector.keyframes.Add(0.1f, flameColor);
                var smokeColor = mParams.smokeColor;
                smokeColor.A = 0.2f;
                _smokeColorEffector.keyframes.Add(0.2f, smokeColor);
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
                _smokeScaleEffector.keyframes.Add(0f, 0.3f);
                _smokeScaleEffector.keyframes.Add(0.1f, 1f);
                _smokeScaleEffector.keyframes.Add(1f, 2f);

                _particlesData.addEffector(_smokeScaleEffector);
            }
        }

        // TODO: remove cluster??? or missile?

        protected void _addMissileRender(SSpaceMissileData missile)
        {
            var missileRuntime = new SSpaceMissileRenderInfo (missile);
            _objScene.AddObject(missileRuntime.bodyObj);
            _particlesData.addEmitter(missileRuntime.smokeEmitter);
            _missileRuntimes.Add(missileRuntime);

            #if MISSILE_DEBUG
            _objScene.AddObject(missileRuntime.debugRays);
            _screenScene.AddObject(missileRuntime.debugCountdown);
            #endif
        }

        protected void _removeMissileRender(SSpaceMissileRenderInfo missileRuntime)
        {
            missileRuntime.bodyObj.renderState.toBeDeleted = true;
            _particlesData.removeEmitter(missileRuntime.smokeEmitter);

            #if MISSILE_DEBUG
            missileRuntime.debugRays.renderState.toBeDeleted = true;
            missileRuntime.debugCountdown.renderState.toBeDeleted = true;
            #endif
        }

        protected void _preRenderUpdate(float timeElapsed)
        {
            for (int i = 0; i < _missileRuntimes.Count; ++i) {
                var missileRuntime = _missileRuntimes [i];
                var missile = missileRuntime.missile;
                if (missile.state == SSpaceMissileData.State.Terminated) {
                    _removeMissileRender(missileRuntime);
                    //if (_explosionRenderer != null) {
                    //    _explosionRenderer.showExplosion(missile.position, missile.cluster.parameters.explosionIntensity);
                    //}
                } else {
                    missileRuntime.preRenderUpdate(timeElapsed);
                }
            }
            _missileRuntimes.RemoveAll(
                (missileRuntime) => missileRuntime.missile.state == SSpaceMissileData.State.Terminated);
        }

        protected class SSpaceMissileRenderInfo
        {
            public readonly SSObjectMesh bodyObj;
            public readonly MissileDebugRays debugRays;
            public readonly SSObject2DSurface_AGGText debugCountdown;

            public readonly SSRadialEmitter smokeEmitter;
            public readonly SSpaceMissileData missile;

            public SSpaceMissileRenderInfo(SSpaceMissileData missile)
            {
                this.missile = missile;
                var mParams = missile.cluster.parameters;
                bodyObj = new SSObjectMesh(mParams.missileMesh);
                bodyObj.Scale = new Vector3(mParams.missileScale);
                bodyObj.renderState.castsShadow = false;
                bodyObj.renderState.receivesShadows = false;

                #if MISSILE_DEBUG
                debugRays = new MissileDebugRays(missile);
                debugCountdown = new SSObject2DSurface_AGGText();
                debugCountdown.MainColor = Color4Helper.RandomDebugColor();
                #endif

                smokeEmitter = new SSRadialEmitter();
                smokeEmitter.effectorMask = (ushort)ParticleEffectorMasks.Smoke;
                smokeEmitter.life = mParams.smokeDuration;
                smokeEmitter.color = new Color4(1f, 1f, 1f, 0f);
                smokeEmitter.billboardXY = true;
                smokeEmitter.particlesPerEmissionMin = mParams.smokeParticlesPerEmissionMin;
                smokeEmitter.particlesPerEmissionMax = mParams.smokeParticlesPerEmissionMax;
                smokeEmitter.spriteRectangles = mParams.flameSmokeSpriteRects;
                smokeEmitter.velocityMagnitudeMin = 11f;
                smokeEmitter.velocityMagnitudeMax = 20f;
                //smokeEmitter.phiMin = 0f;
                //smokeEmitter.phiMax = (float)Math.PI/6f;
                smokeEmitter.phiMin = (float)Math.PI/3f;
                smokeEmitter.phiMax = (float)Math.PI/2f;

                smokeEmitter.radiusOffsetMin = 0f;
                smokeEmitter.radiusOffsetMax = 0.1f;

                // positions emitters and mesh
                preRenderUpdate(0f);
            }

            public void preRenderUpdate(float timeElapsed)
            {
                var mParams = missile.cluster.parameters;

                bodyObj.Pos = missile.position;
                bodyObj.Orient(missile.direction, missile.up);

                var emissionRatio = missile.jetStrength / mParams.fullSmokeEmissionAcc;
                var emissionFreq = 20f;
                //var emissionFreq = Interpolate.Lerp(
                //   mParams.smokeEmissionFrequencyMin, mParams.smokeEmissionFrequencyMax, emissionRatio);
                
                smokeEmitter.center = missile.position
                    - missile.direction * mParams.missileScale * mParams.jetPosition;
                smokeEmitter.up = -missile.direction;

                smokeEmitter.emissionInterval = 1f / emissionFreq;
                smokeEmitter.componentScale = new Vector3(emissionRatio);
                //smokeEmitter.emissionInterval = 1f / 80f;

                #if MISSILE_DEBUG
                RectangleF clientRect = OpenTKHelper.GetClientRect();
                var xy = OpenTKHelper.WorldToScreen(
                    missile.position, ref debugRays.viewProjMat, ref clientRect);
                debugCountdown.Pos = new Vector3(xy.X, xy.Y, 0f);
                debugCountdown.Label = Math.Floor(missile.cluster.timeToHit).ToString();
                #endif

            }

            public class MissileDebugRays : SSObject
            {
                protected readonly SSpaceMissileData _missile;
                public Matrix4 viewProjMat; // maintain this matrix to held 2d countdown renderer 

                public MissileDebugRays(SSpaceMissileData missile)
                {
                    _missile = missile;
                    renderState.castsShadow = false;
                    renderState.receivesShadows = false;
                    renderState.frustumCulling = false;
                }

                public override void Render (SSRenderConfig renderConfig)
                {
                    this.Pos = _missile.position;
                    base.Render(renderConfig);
                    SSShaderProgram.DeactivateAll();
                    GL.Color4(Color4.LightCyan);
                    GL.Begin(PrimitiveType.Lines);
                    GL.LineWidth(3f);
                    GL.Vertex3(Vector3.Zero);
                    GL.Vertex3(_missile._lataxDebug);
                    GL.End();

                    viewProjMat = renderConfig.invCameraViewMatrix * renderConfig.projectionMatrix;
                }
            }
        }
    }
}



