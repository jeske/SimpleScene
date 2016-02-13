using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util;

namespace SimpleScene.Demos
{
	public class SLaserManager
	{
		/// <summary>
		/// Scene that the lasers will be added to/removed from
		/// </summary>
		protected readonly SSScene _beamScene3d;
        protected readonly SSScene _occDiskScene;
		protected readonly SSScene _flareScene2d;

        protected readonly SInstancedSpriteData _2dEffectInstanceData;
        protected readonly SSInstancedSpriteRenderer _2dEffectRenderer; 

        protected readonly int _maxBurnParticlesPerObject;
        protected readonly Dictionary<SSObject, SLaserBurnParticlesObject> _laserBurnParticleRenderers
            = new Dictionary<SSObject, SLaserBurnParticlesObject> ();
		protected List<LaserRuntimeInfo> _laserRuntimes = new List<LaserRuntimeInfo>();

		//public SLaserBurnParticlesObject laserBurnParticlesObject {
		//	get { return _laserBurnParticles; }
		//}

        public SLaserManager (SSScene beamScene3d, SSScene occDiskScene, SSScene flareScene2d,
            int sprite2dCapacity = 1000, int laserBurnParticlesCapacity = 500)
		{
			_beamScene3d = beamScene3d;
            _occDiskScene = occDiskScene;
            _flareScene2d = flareScene2d;

            _2dEffectInstanceData = new SInstancedSpriteData (sprite2dCapacity);
            _2dEffectRenderer = new SSInstancedSpriteRenderer (_beamScene3d, _2dEffectInstanceData);
            _2dEffectRenderer.Name = "laser manager's 2d screen effect renderer";

            _2dEffectRenderer.renderState.alphaBlendingOn = true;
            _2dEffectRenderer.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            _2dEffectRenderer.renderState.blendFactorDest = BlendingFactorDest.One;
            //_2dEffectRenderer.renderMode = SSInstancedMeshRenderer.RenderMode.GpuInstancing;
            _flareScene2d.AddObject(_2dEffectRenderer);

            _beamScene3d.preRenderHooks += this._update;

            _maxBurnParticlesPerObject = laserBurnParticlesCapacity;
		}

        ~SLaserManager()
        {
            foreach (var psRenderer in _laserBurnParticleRenderers.Values) {
                if (psRenderer != null) {
                    psRenderer.renderState.toBeDeleted = true;
                }
            }
            _2dEffectRenderer.renderState.toBeDeleted = true;
        }

		public SLaser addLaser(SLaserParameters laserParams, 
		   		     		   SSObject srcObject, SSObject dstObject,
                               SLaser.TargetVelFunc targetVelFunc = null)
		{
            if (_2dEffectRenderer.textureMaterial == null) {
                _2dEffectRenderer.textureMaterial = new SSTextureMaterial (laserParams.emissionSpritesTexture());
            }

            var newLaser = new SLaser (laserParams, targetVelFunc);
			newLaser.sourceObject = srcObject;
			newLaser.targetObject = dstObject;

            var newLaserRuntime = new LaserRuntimeInfo (newLaser, _beamScene3d, _occDiskScene, _2dEffectRenderer);
			_laserRuntimes.Add (newLaserRuntime);

            if (laserParams.doLaserBurn) {
                SLaserBurnParticlesObject burnRenderer;
                bool ok = _laserBurnParticleRenderers.TryGetValue(dstObject, out burnRenderer);
                if (!ok) {
                    burnRenderer = new SLaserBurnParticlesObject (dstObject, _maxBurnParticlesPerObject, null);
                    burnRenderer.Name = "laser manager's laser burn particle system renderer";
                    burnRenderer.textureMaterial = new SSTextureMaterial(laserParams.laserBurnParticlesTexture());
                    //_laserBurnParticles.renderMode = SSInstancedMeshRenderer.RenderMode.GpuInstancing;
                    burnRenderer.simulateOnUpdate = false;
                    burnRenderer.simulateOnRender = true;
                    _beamScene3d.AddObject(burnRenderer);
                    _laserBurnParticleRenderers [dstObject] = burnRenderer;
                }
                burnRenderer.particleSystem.addHitSpots(newLaser);
            }	


            // debug hacks
			//newLaser.sourceObject = newLaserRuntime.beamRuntimes[0].emissionBillboard;
			//newLaser.sourceTxfm = Matrix4.Identity;

			return newLaser;
		}

        public void removeLaser(SLaser laser)
        {
            int idx = _laserRuntimes.FindIndex(lrt => lrt.laser == laser);
            _removeLaser(idx);
        }

        protected void _removeLaser(int i) 
        {
            var lrt = _laserRuntimes [i];
            SLaserBurnParticlesObject burnRenderer;
            bool ok = _laserBurnParticleRenderers.TryGetValue(lrt.laser.targetObject, out burnRenderer);
            if (ok) {
                burnRenderer.particleSystem.removeHitSpots(lrt.laser);
                if (burnRenderer.particleSystem.numHitSpots <= 0) {
                    burnRenderer.renderState.toBeDeleted = true;
                    _laserBurnParticleRenderers.Remove(lrt.laser.targetObject);
                }
            }
            lrt.requestDeleteFromScene();
            _laserRuntimes.RemoveAt(i);
        }

        public void releaseLaser(SLaser laser)
        {
            laser.release();
        }

		protected void _update(float timeElapsedS)
		{
			for (int i = 0; i < _laserRuntimes.Count; ++i) {
				var lrt = _laserRuntimes [i];
                #if false
                SLaserBurnParticlesObject burnRenderer;
                bool ok = _laserBurnParticleRenderers.TryGetValue(lrt.laser.targetObject, out burnRenderer);
                if (ok) {
                    burnRenderer.particleSystem.updateHitSpotVelocity(
                        lrt.laser, Vector3.Zero);                    
                }
                #endif
				lrt.update (timeElapsedS);
				if (lrt.laser.hasExpired) {
                    _removeLaser(i);
				}
			}

            // TODO remove objects that are being deleted

            var toBeDeleted = new List<SSObject> ();
            foreach (var key in _laserBurnParticleRenderers.Keys) {
                if (key != null && key.renderState.toBeDeleted) {
                    toBeDeleted.Add(key);
                }
            }
            foreach (var key in toBeDeleted) {
                _laserBurnParticleRenderers.Remove(key);
            }

            //_laserBurnParticles.particleSystem.update(timeElapsedS);

            #if false
            // debugging
            System.Console.WriteLine("lasers: #sprites = " + _2dEffectInstanceData.numElements);
            System.Console.WriteLine("lasers: #3d particles = " + _laserBurnParticles.particleSystem.numElements);
            #endif
		}

		protected class BeamRuntimeInfo
		{
			protected readonly SLaser _laser;
			protected readonly int _beamId;

			protected readonly SSScene _beamScene;
            protected readonly SSScene _occDiskScene;
            protected readonly SSInstancedSpriteRenderer _sprite2dRenderer;

			protected SSObjectOcclusionQueuery _occDiskFlatObj = null;

            protected SLaserBeamMiddleObject _beamObj = null;
            protected SLaserEmissionFlareUpdater _emissionFlareUpdater = null;
            protected SLaserScreenHitFlareUpdater _hitFlareUpdater = null;

			public BeamRuntimeInfo(SLaser laser, int beamId, 
                                   SSScene beamScene, SSScene occDiskScene,
                                   SSInstancedSpriteRenderer sprite2dRenderer)
			{
				_laser = laser;
				_beamId = beamId;
				_beamScene = beamScene;
                _occDiskScene = occDiskScene;
                _sprite2dRenderer = sprite2dRenderer;
			}

			public void requestDeleteFromScene()
			{
				if (_beamObj != null) {
					_beamObj.renderState.toBeDeleted = true;
				}
                if (_occDiskFlatObj != null) {
                    _occDiskFlatObj.renderState.toBeDeleted = true;
				}
                if (_emissionFlareUpdater != null) {
                    _sprite2dRenderer.removeUpdater(_emissionFlareUpdater);
                    _emissionFlareUpdater = null;
                }
                if (_hitFlareUpdater != null) {
                    _sprite2dRenderer.removeUpdater(_hitFlareUpdater);
                    _hitFlareUpdater = null;
                }
			}

			public void update(float timeElapsed)
			{
				var beam = _laser.beam (_beamId);
				if (beam == null) return;

                _createRenderObjects();

                if (_occDiskFlatObj != null) {
                    _occDiskFlatObj.Pos = beam.startPosWorld 
                        + beam.directionWorld() * _laser.parameters.emissionOccDiskDirOffset;

                    _occDiskFlatObj.Orient(_laser.sourceOrient());
                }
                // TODO consider per-beam orient?
			}

            protected void _createRenderObjects()
            {
                var laserParams = this._laser.parameters;

                if (laserParams.doEmissionFlare && _occDiskFlatObj == null) {
                    _occDiskFlatObj = new SSObjectOcclusionQueuery (new SSMeshDisk ());
                    _occDiskFlatObj.renderState.alphaBlendingOn = true;
                    _occDiskFlatObj.renderState.lighted = false;
                    _occDiskFlatObj.renderState.depthTest = true;
                    _occDiskFlatObj.renderState.depthWrite = false; 
                    _occDiskFlatObj.renderState.doBillboarding = false;
                    _occDiskFlatObj.renderState.matchScaleToScreenPixels = true;
                    _occDiskFlatObj.Scale = new Vector3 (_laser.parameters.emissionOccDiskRadiusPx);
                    var color = _laser.parameters.backgroundColor; // debugging
                    color.A = _laser.parameters.emissionOccDisksAlpha;
                    _occDiskFlatObj.MainColor = color;
                    _occDiskFlatObj.Name = "occlusion disk object for laser beam's emission flare";
                    _occDiskScene.AddObject(_occDiskFlatObj);
                }

                if (_beamObj == null) {
                    _beamObj = new SLaserBeamMiddleObject (
                        _laser, _beamId, _beamScene,
                        laserParams.middleBackgroundTexture(), laserParams.middleOverlayTexture(),
                        laserParams.middleInterferenceTexture());
                    _beamObj.Name = "laser beam middle section object";
                    _beamScene.AddObject(_beamObj);
                }

                if (laserParams.doEmissionFlare && _emissionFlareUpdater == null) {
                    _emissionFlareUpdater = new SLaserEmissionFlareUpdater (
                        _laser, _beamId, _occDiskFlatObj, 
                        laserParams.emissionBackgroundRect, laserParams.emissionOverlayRect);
                    _sprite2dRenderer.addUpdater(_emissionFlareUpdater);
                }

                if (laserParams.doScreenHitFlare && _hitFlareUpdater == null) {
                    float[] masterScales = { laserParams.hitFlareCoronaOverlayScale, laserParams.hitFlareCoronaOverlayScale,
                                             laserParams.hitFlareRing1Scale, laserParams.hitFlareRing2Scale };
                    _hitFlareUpdater = new SLaserScreenHitFlareUpdater (_laser, _beamId, _beamScene, null, masterScales);
                    _sprite2dRenderer.addUpdater(_hitFlareUpdater);
                }
            }
		}

		protected class LaserRuntimeInfo
		{
			protected readonly SLaser _laser;
			protected readonly BeamRuntimeInfo[] _beams;

			public SLaser laser { get { return _laser; } }

			public LaserRuntimeInfo(SLaser laser, SSScene beamScene, SSScene occDiskScene, 
                                    SSInstancedSpriteRenderer sprite2dRenderer)
			{
				_laser = laser;
				var numBeams = laser.parameters.numBeams;
				_beams = new BeamRuntimeInfo[numBeams];
				for (int i = 0; i < numBeams; ++i) {
                    _beams[i] = new BeamRuntimeInfo(laser, i, beamScene, occDiskScene, sprite2dRenderer);
				}
			}

			public void update(float timeElapsedS)
			{
				_laser.update (timeElapsedS);

				foreach (var beam in _beams) {
					beam.update (timeElapsedS);
				}
			}

			public void requestDeleteFromScene()
			{
				foreach (var beam in _beams) {
					beam.requestDeleteFromScene ();
				}
			}
		}
	}
}

