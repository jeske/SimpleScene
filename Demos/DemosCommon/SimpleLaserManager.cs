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
		protected SSScene _mainScene;
		protected SSScene _flareScene;

		protected List<LaserRuntimeInfo> _laserRuntimes = new List<LaserRuntimeInfo>();

		public SimpleLaserManager (SSScene mainScene, SSScene flareScene)
		{
			this._mainScene = mainScene;
			this._flareScene = flareScene;

			mainScene.preUpdateHooks += _update;
		}

		public SimpleLaser addLaser(SimpleLaserParameters laserParams, 
						     		SSObject srcObject, SSObject dstObject)
		{
			var newLaser = new SimpleLaser (laserParams);
			//newLaser.intensityEnvelope.sustainDuration = sustainDuration;
			newLaser.sourceObject = srcObject;
			newLaser.destObject = dstObject;

			var newLaserRuntime = new LaserRuntimeInfo (newLaser, _mainScene, _flareScene);
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
			protected readonly SSScene _flareScene;

			protected SSObjectOcclusionQueuery _emissionOccDisk = null;
            protected SimpleLaserFlareEffect _emissionFlareObj = null;
			protected SimpleLaserBeamObject _beamObj = null;

			public BeamRuntimeInfo(SimpleLaser laser, int beamId, SSScene mainScene, SSScene flareScene)
			{
				_laser = laser;
				_beamId = beamId;
				_beamScene = mainScene;
				_flareScene = flareScene;
			}

			public void requestDeleteFromScene()
			{
				if (_beamObj != null) {
					_beamObj.renderState.toBeDeleted = true;
				}
				if (_emissionOccDisk != null) {
					_emissionOccDisk.renderState.toBeDeleted = true;
				}
				if (_emissionFlareObj != null) {
					_emissionFlareObj.renderState.toBeDeleted = true;
				}
			}

			public void update(float timeElapsed)
			{
				var beam = _laser.beam (_beamId);
				if (beam == null) return;

                _createRenderObjects();

                _emissionOccDisk.Pos = beam.startPos + _laser.direction() * _laser.parameters.occDiskDirOffset;
				// TODO consider per-beam orient
				_emissionOccDisk.Orient(_laser.sourceOrient());
			}

            protected void _createRenderObjects()
            {
                if (_emissionOccDisk == null) {
                    _emissionOccDisk = new SSObjectOcclusionQueuery (new SSMeshDisk ());
                    _emissionOccDisk.renderState.doBillboarding = false;
                    _emissionOccDisk.renderState.alphaBlendingOn = true;
                    _emissionOccDisk.renderState.lighted = false;
                    _emissionOccDisk.renderState.depthWrite = false;
                    var color = _laser.parameters.backgroundColor; // debugging
                    color.A = 0.0001f;
                    //color.A = 0.5f;
                    _emissionOccDisk.MainColor = color;
                    _beamScene.AddObject(_emissionOccDisk);
                }

                if (_beamObj == null) {
                    _beamObj = new SimpleLaserBeamObject (_laser, _beamId, _beamScene);
                    _beamScene.AddObject(_beamObj);
                }

                if (_emissionFlareObj == null) {
                    var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "flareOverlay.png");
                    var rect = new RectangleF (0f, 0f, 1f, 1f);
                    _emissionFlareObj = new SimpleLaserFlareEffect (_laser, _beamId, _beamScene,_emissionOccDisk,
                        tex, rect, rect);
                    _flareScene.AddObject(_emissionFlareObj);
               }
            }
		}

		protected class LaserRuntimeInfo
		{
			protected readonly SimpleLaser _laser;
			protected readonly BeamRuntimeInfo[] _beams;

			public SimpleLaser laser { get { return _laser; } }

			public LaserRuntimeInfo(SimpleLaser laser, SSScene beamScene, SSScene flareScene)
			{
				_laser = laser;
				var numBeams = laser.parameters.numBeams;
				_beams = new BeamRuntimeInfo[numBeams];
				for (int i = 0; i < numBeams; ++i) {
					_beams[i] = new BeamRuntimeInfo(laser, i, beamScene, flareScene);
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

