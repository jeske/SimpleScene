#define MISSILE_SHOW
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
        enum ParticleEffectorMasks : ushort { FlameToSmoke=1, EjectionSmoke=2 };

        protected readonly SSpaceMissilesSimulation _simulation;
        protected readonly SSScene _objScene;
        protected readonly SSScene _screenScene;

        protected readonly SSParticleSystemData _particlesData;
        protected readonly SSInstancedMeshRenderer _particleRenderer;

        protected SSColorKeyframesEffector _flameSmokeColorEffector = null;
        protected SSColorKeyframesEffector _ejectionSmokeColorEffector = null;
        protected SSMasterScaleKeyframesEffector _flameSmokeScaleEffector = null;
        protected SSMasterScaleKeyframesEffector _ejectionSmokeScaleEffector = null;

        readonly List<SSpaceMissileRenderInfo> _missileRuntimes = new List<SSpaceMissileRenderInfo> ();

        public SSpaceMissilesSimulation simulation { get { return _simulation; } }

        public int numRenderedMissiles { get { return _missileRuntimes.Count; } }
        public int numRenderParticles { get { return _particlesData.numElements; } }
        public int numMissileClusters { get { return _simulation.numClusters; } }

        public SSpaceMissilesRenderManager (SSScene objScene, SSScene particleScene, SSScene screenScene,
                                            int particleCapacity = 2000)
        {
            _simulation = new SSpaceMissilesSimulation ();

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
            //_particleRenderer.renderState.visible = false;

            _particleRenderer.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
            _particleRenderer.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
            _particleRenderer.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
            _particleRenderer.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
            _particleRenderer.ShininessMatColor = 0f;

            _particleRenderer.Name = "missile smoke renderer";
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
            SSpaceMissileParameters clusterParams)
        {
            _initParamsSpecific(clusterParams);
            var cluster = _simulation.launchCluster(launchPos, launchVel, numMissiles,
                                                    target, timeToHit, clusterParams);
            foreach (var missile in cluster.missiles) {
                _addMissileRender(missile);
            }
            return cluster;
        }



        protected void _initParamsSpecific(SSpaceMissileParameters mParams)
        {
            // smoke effectors
            if (_particleRenderer.textureMaterial == null) {
                _particleRenderer.textureMaterial = new SSTextureMaterial (mParams.smokeParticlesTexture);
            }
            if (_flameSmokeColorEffector == null) {
                _flameSmokeColorEffector = new SSColorKeyframesEffector ();
                _flameSmokeColorEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                _flameSmokeColorEffector.effectorMask = (ushort)ParticleEffectorMasks.FlameToSmoke;
                _flameSmokeColorEffector.particleLifetime = mParams.flameSmokeDuration;
                //_smokeColorEffector.colorMask = ;
                _flameSmokeColorEffector.keyframes.Clear();
                var flameColor = mParams.innerFlameColor;
                flameColor.A = 0.9f;
                _flameSmokeColorEffector.keyframes.Add(0f, flameColor);
                flameColor = mParams.outerFlameColor;
                flameColor.A = 0.7f;
                _flameSmokeColorEffector.keyframes.Add(0.1f, flameColor);
                var smokeColor = mParams.smokeColor;
                smokeColor.A = 0.2f;
                _flameSmokeColorEffector.keyframes.Add(0.2f, smokeColor);
                smokeColor.A = 0f;
                _flameSmokeColorEffector.keyframes.Add(1f, smokeColor);

                _particlesData.addEffector(_flameSmokeColorEffector);
            }
            if (_ejectionSmokeColorEffector == null) {
                _ejectionSmokeColorEffector = new SSColorKeyframesEffector ();
                _ejectionSmokeColorEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                _ejectionSmokeColorEffector.effectorMask = (ushort)ParticleEffectorMasks.EjectionSmoke;
                _ejectionSmokeColorEffector.particleLifetime = mParams.flameSmokeDuration;
                _ejectionSmokeColorEffector.keyframes.Clear();
                var smokeColor = mParams.smokeColor;
                smokeColor.A = 0.3f;
                _ejectionSmokeColorEffector.keyframes.Add(0f, smokeColor);
                smokeColor.A = 0.14f;
                _ejectionSmokeColorEffector.keyframes.Add(0.2f, smokeColor);
                smokeColor.A = 0f;
                _ejectionSmokeColorEffector.keyframes.Add(1f, smokeColor);

                _particlesData.addEffector(_ejectionSmokeColorEffector);
            }
            if (_flameSmokeScaleEffector == null) {
                _flameSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
                _flameSmokeScaleEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                _flameSmokeScaleEffector.effectorMask = (ushort)ParticleEffectorMasks.FlameToSmoke;
                _flameSmokeScaleEffector.particleLifetime = mParams.flameSmokeDuration;
                _flameSmokeScaleEffector.keyframes.Clear();
                _flameSmokeScaleEffector.keyframes.Add(0f, 0.3f);
                _flameSmokeScaleEffector.keyframes.Add(0.1f, 1f);
                _flameSmokeScaleEffector.keyframes.Add(1f, 2f);

                _particlesData.addEffector(_flameSmokeScaleEffector);
            }
            if (_ejectionSmokeScaleEffector == null) {
                _ejectionSmokeScaleEffector = new SSMasterScaleKeyframesEffector ();
                _ejectionSmokeScaleEffector.maskMatchFunction = SSParticleEffector.MatchFunction.Equals;
                _ejectionSmokeScaleEffector.effectorMask = (ushort)ParticleEffectorMasks.EjectionSmoke;
                _ejectionSmokeScaleEffector.particleLifetime = mParams.ejectionSmokeDuration;
                _ejectionSmokeScaleEffector.keyframes.Clear();
                _ejectionSmokeScaleEffector.keyframes.Add(0f, 0.1f);
                _ejectionSmokeScaleEffector.keyframes.Add(0.5f, 1f);
                _ejectionSmokeScaleEffector.keyframes.Add(1f, 1.5f);

                _particlesData.addEffector(_ejectionSmokeScaleEffector);
            }
        }

        // TODO: remove cluster??? or missile?

        protected void _addMissileRender(SSpaceMissileData missile)
        {
            var missileRuntime = new SSpaceMissileRenderInfo (missile);
            #if MISSILE_SHOW
            _objScene.AddObject(missileRuntime.bodyObj);
            _particlesData.addEmitter(missileRuntime.flameSmokeEmitter);
            #endif
            _missileRuntimes.Add(missileRuntime);

            #if MISSILE_DEBUG
            _objScene.AddObject(missileRuntime.debugRays);
            _screenScene.AddObject(missileRuntime.debugCountdown);
            #endif
        }

        protected void _removeMissileRender(SSpaceMissileRenderInfo missileRuntime)
        {
            #if MISSILE_SHOW
            missileRuntime.bodyObj.renderState.toBeDeleted = true;
            _particlesData.removeEmitter(missileRuntime.flameSmokeEmitter);
            #endif

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
                } else {
                    missileRuntime.preRenderUpdate(timeElapsed);
                }
            }
            _missileRuntimes.RemoveAll(
                (missileRuntime) => missileRuntime.missile.state == SSpaceMissileData.State.Terminated);

            // debugging
            #if false
            System.Console.WriteLine("num missiles = " + _missileRuntimes.Count);
            System.Console.WriteLine("num particles = " + _particlesData.numElements);
            #endif
        }

        protected class SSpaceMissileRenderInfo
        {
            public readonly SSObjectMesh bodyObj;
            public readonly MissileDebugRays debugRays;
            public readonly SSObject2DSurface_AGGText debugCountdown;

            public readonly SSRadialEmitter flameSmokeEmitter;
            public readonly SSpaceMissileData missile;

            public SSpaceMissileRenderInfo(SSpaceMissileData missile)
            {
                this.missile = missile;
                var mParams = missile.cluster.parameters;
                bodyObj = new SSObjectMesh(mParams.missileMesh);
                bodyObj.Scale = new Vector3(mParams.missileBodyScale);
                bodyObj.renderState.castsShadow = false;
                bodyObj.renderState.receivesShadows = false;
                //bodyObj.renderState.visible = false;
                bodyObj.Name = "a missile body";

                #if MISSILE_DEBUG
                debugRays = new MissileDebugRays(missile);
                debugCountdown = new SSObject2DSurface_AGGText();
                debugCountdown.Size = 2f;
                debugCountdown.MainColor = Color4Helper.RandomDebugColor();
                #endif

                flameSmokeEmitter = new SSRadialEmitter();
                flameSmokeEmitter.effectorMask = (ushort)ParticleEffectorMasks.EjectionSmoke;
                flameSmokeEmitter.life = mParams.flameSmokeDuration;
                flameSmokeEmitter.color = new Color4(1f, 1f, 1f, 1f);
                flameSmokeEmitter.billboardXY = true;
                flameSmokeEmitter.particlesPerEmissionMin = mParams.smokePerEmissionMin;
                flameSmokeEmitter.particlesPerEmissionMax = mParams.smokePerEmissionMax;
                flameSmokeEmitter.spriteRectangles = mParams.smokeSpriteRects;
                //smokeEmitter.phiMin = 0f;
                //smokeEmitter.phiMax = (float)Math.PI/6f;
                flameSmokeEmitter.phiMin = (float)Math.PI/3f;
                flameSmokeEmitter.phiMax = (float)Math.PI/2f;
                flameSmokeEmitter.orientationMin = new Vector3 (0f, 0f, 0f);
                flameSmokeEmitter.orientationMax = new Vector3 (0f, 0f, 2f * (float)Math.PI);
                flameSmokeEmitter.angularVelocityMin = new Vector3 (0f, 0f, -0.5f);
                flameSmokeEmitter.angularVelocityMax = new Vector3 (0f, 0f, +0.5f);

                flameSmokeEmitter.radiusOffsetMin = 0f;
                flameSmokeEmitter.radiusOffsetMax = 0.1f;  

                // positions emitters and mesh
                preRenderUpdate(0f);
            }

            public void preRenderUpdate(float timeElapsed)
            {
                var mParams = missile.cluster.parameters;

                bodyObj.Pos = missile.position;
                bodyObj.Orient(missile.visualDirection, missile.up);

                bool ejection = missile.state == SSpaceMissileData.State.Ejection;
                float smokeFrequency = Interpolate.Lerp(
                    mParams.smokeEmissionFrequencyMin, mParams.smokeEmissionFrequencyMax, missile.visualSmokeAmmount);
                float sizeMin = ejection ? mParams.ejectionSmokeSizeMin : mParams.flameSmokeSizeMin;
                float sizeMax = ejection ? mParams.ejectionSmokeSizeMax : mParams.flameSmokeSizeMax;
                float smokeSize = Interpolate.Lerp(sizeMin, sizeMax, 
                    (float)SSpaceMissilesSimulation.rand.NextDouble());

                //flameSmokeEmitter.velocity = -missile.velocity;
                flameSmokeEmitter.center = missile.jetPosition();
                flameSmokeEmitter.up = -missile.visualDirection;
                flameSmokeEmitter.emissionInterval = 1f / smokeFrequency;
                flameSmokeEmitter.componentScale = new Vector3(smokeSize);
                flameSmokeEmitter.effectorMask = (ushort)
                    (ejection ? ParticleEffectorMasks.EjectionSmoke : ParticleEffectorMasks.FlameToSmoke);
                var vel = missile.velocity.LengthFast;
                flameSmokeEmitter.velocity = missile.velocity;
                flameSmokeEmitter.velocityMagnitudeMin = ejection ? -vel : (-vel / 4f);
                flameSmokeEmitter.velocityMagnitudeMax = vel;
                flameSmokeEmitter.life = ejection ? mParams.ejectionSmokeDuration : mParams.flameSmokeDuration;

                #if MISSILE_DEBUG
                RectangleF clientRect = OpenTKHelper.GetClientRect();
                var xy = OpenTKHelper.WorldToScreen(
                    missile.position, ref debugRays.viewProjMat, ref clientRect);
                debugCountdown.Pos = new Vector3(xy.X, xy.Y, 0f);
                debugCountdown.Label = Math.Floor(missile.cluster.timeToHit).ToString();
                //debugCountdown.Label = missile.losRate.ToString("G3") + " : " + missile.losRateRate.ToString("G3");

                debugRays.renderState.visible = mParams.debuggingAid;
                debugCountdown.renderState.visible = mParams.debuggingAid;
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
                    GL.LineWidth(3f);
                    GL.Begin(PrimitiveType.Lines);
                    GL.Color4(Color4.LightCyan);
                    GL.Vertex3(Vector3.Zero);
                    GL.Vertex3(_missile._lataxDebug);
                    GL.Color4(Color4.Magenta);
                    GL.Vertex3(Vector3.Zero);
                    GL.Vertex3(_missile._hitTimeCorrAccDebug);
                    GL.End();

                    viewProjMat = renderConfig.invCameraViewMatrix * renderConfig.projectionMatrix;
                }
            }
        }
    }
}



