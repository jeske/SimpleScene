using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
	public class SimpleLaserManager
	{
		// TODO switch targets while firing

		/// <summary>
		/// Scene that the lasers will be added to/removed from
		/// </summary>
		protected readonly SSScene _beamScene;
        protected readonly SSScene _occDiskScene;
		protected readonly SSScene _flareScene;

		protected List<LaserRuntimeInfo> _laserRuntimes = new List<LaserRuntimeInfo>();

        public SimpleLaserManager (SSScene beamScene, SSScene occDiskScene, SSScene flareScene)
		{
			this._beamScene = beamScene;
            this._occDiskScene = occDiskScene;
			this._flareScene = flareScene;

			beamScene.preUpdateHooks += _update;
		}

		public SimpleLaser addLaser(SimpleLaserParameters laserParams, 
						     		SSObject srcObject, SSObject dstObject)
		{
			var newLaser = new SimpleLaser (laserParams);
			//newLaser.intensityEnvelope.sustainDuration = sustainDuration;
			newLaser.sourceObject = srcObject;
			newLaser.destObject = dstObject;

			var newLaserRuntime = new LaserRuntimeInfo (newLaser, _beamScene, _occDiskScene, _flareScene);
			_laserRuntimes.Add (newLaserRuntime);

			// debug hacks
			//newLaser.sourceObject = newLaserRuntime.beamRuntimes[0].emissionBillboard;
			//newLaser.sourceTxfm = Matrix4.Identity;

			return newLaser;
		}

		protected void _update(float timeElapsedS)
		{
			for (int i = 0; i < _laserRuntimes.Count; ++i) {
				var lrt = _laserRuntimes [i];
				lrt.update (timeElapsedS);
				if (lrt.laser.hasExpired) {
					lrt.requestDeleteFromScene ();
					_laserRuntimes.RemoveAt (i);
					--i;
				}
			}
		}

		protected class BeamRuntimeInfo
		{
			protected readonly SimpleLaser _laser;
			protected readonly int _beamId;

			protected readonly SSScene _beamScene;
            protected readonly SSScene _occDiskScene;
			protected readonly SSScene _flareScene;

			protected SSObjectOcclusionQueuery _occDiskObj = null;
            protected SimpleLaserFlareEffect _flareObj = null;
			protected SimpleLaserBeamObject _beamObj = null;

			public BeamRuntimeInfo(SimpleLaser laser, int beamId, 
                                   SSScene beamScene, SSScene occDiskScene, SSScene flareScene)
			{
				_laser = laser;
				_beamId = beamId;
				_beamScene = beamScene;
                _occDiskScene = occDiskScene;
				_flareScene = flareScene;
			}

			public void requestDeleteFromScene()
			{
				if (_beamObj != null) {
					_beamObj.renderState.toBeDeleted = true;
				}
				if (_occDiskObj != null) {
					_occDiskObj.renderState.toBeDeleted = true;
				}
				if (_flareObj != null) {
					_flareObj.renderState.toBeDeleted = true;
				}
			}

			public void update(float timeElapsed)
			{
				var beam = _laser.beam (_beamId);
				if (beam == null) return;

                _createRenderObjects();

                _occDiskObj.Pos = beam.startPos + _laser.direction() * _laser.parameters.occDiskDirOffset;
				// TODO consider per-beam orient
				_occDiskObj.Orient(_laser.sourceOrient());
			}

            protected void _createRenderObjects()
            {
                if (_occDiskObj == null) {
                    _occDiskObj = new SSObjectOcclusionQueuery (new SSMeshDisk ());
                    _occDiskObj.renderState.doBillboarding = false;
                    _occDiskObj.renderState.alphaBlendingOn = true;
                    _occDiskObj.renderState.lighted = false;
                    _occDiskObj.renderState.depthWrite = false;
                    var color = _laser.parameters.backgroundColor; // debugging
                    color.A = 0.0001f;
                    color.A = 0.5f;
                    _occDiskObj.MainColor = color;
                    _occDiskObj.Scale = new Vector3 (10f);
                    _occDiskScene.AddObject(_occDiskObj);
                }

                if (_beamObj == null) {
                    _beamObj = new SimpleLaserBeamObject (_laser, _beamId, _beamScene);
                    _beamScene.AddObject(_beamObj);
                }

                if (_flareObj == null) {
                    var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "flareOverlay.png");
                    var rect = new RectangleF (0f, 0f, 1f, 1f);
                    _flareObj = new SimpleLaserFlareEffect (_laser, _beamId, _beamScene, _occDiskObj,
                        tex, rect, rect);
                    _flareScene.AddObject(_flareObj);
                }
            }
		}

		protected class LaserRuntimeInfo
		{
			protected readonly SimpleLaser _laser;
			protected readonly BeamRuntimeInfo[] _beams;

			public SimpleLaser laser { get { return _laser; } }

			public LaserRuntimeInfo(SimpleLaser laser, 
                                    SSScene beamScene, SSScene occDiskScene, SSScene flareScene)
			{
				_laser = laser;
				var numBeams = laser.parameters.numBeams;
				_beams = new BeamRuntimeInfo[numBeams];
				for (int i = 0; i < numBeams; ++i) {
                    _beams[i] = new BeamRuntimeInfo(laser, i, beamScene, occDiskScene, flareScene);
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

