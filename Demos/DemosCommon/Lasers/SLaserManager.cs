using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene.Demos
{
	public class SLaserManager
	{
		// TODO switch targets while firing

		/// <summary>
		/// Scene that the lasers will be added to/removed from
		/// </summary>
		protected readonly SSScene _beamScene;
        protected readonly SSScene _occDiskScene;
		protected readonly SSScene _flareScene;

		protected List<LaserRuntimeInfo> _laserRuntimes = new List<LaserRuntimeInfo>();

        public SLaserManager (SSScene beamScene, SSScene occDiskScene, SSScene flareScene)
		{
			this._beamScene = beamScene;
            this._occDiskScene = occDiskScene;
			this._flareScene = flareScene;

			beamScene.preUpdateHooks += _update;
		}

		public SLaser addLaser(SLaserParameters laserParams, 
						     		SSObject srcObject, SSObject dstObject)
		{
			var newLaser = new SLaser (laserParams);
			//newLaser.intensityEnvelope.sustainDuration = sustainDuration;
			newLaser.sourceObject = srcObject;
			newLaser.targetObject = dstObject;

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
			protected readonly SLaser _laser;
			protected readonly int _beamId;

			protected readonly SSScene _beamScene;
            protected readonly SSScene _occDiskScene;
			protected readonly SSScene _flareScene;

			protected SSObjectOcclusionQueuery _occDiskFlatObj = null;
            protected SSObjectOcclusionQueuery _occDiskPerspObj = null;
            protected SLaserEmissionFlareObject _emissionFlareObj = null;
            protected SLaserHitFlareObject _hitFlareObj = null;
			protected SLaserBeamObject _beamObj = null;

			public BeamRuntimeInfo(SLaser laser, int beamId, 
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
                if (_occDiskFlatObj != null) {
                    _occDiskFlatObj.renderState.toBeDeleted = true;
				}
                if (_occDiskPerspObj != null) {
                    _occDiskPerspObj.renderState.toBeDeleted = true;
                }
                if (_emissionFlareObj != null) {
                    _emissionFlareObj.renderState.toBeDeleted = true;
                }
                if (_hitFlareObj != null) {
                    _hitFlareObj.renderState.toBeDeleted = true;
                }
			}

			public void update(float timeElapsed)
			{
				var beam = _laser.beam (_beamId);
				if (beam == null) return;

                _createRenderObjects();

                _occDiskFlatObj.Pos = _occDiskPerspObj.Pos 
                    = beam.startPos + _laser.direction() * _laser.parameters.occDiskDirOffset;
				// TODO consider per-beam orient

                _occDiskFlatObj.Orient(_laser.sourceOrient());
                _occDiskPerspObj.Orient(_laser.sourceOrient());
			}

            protected void _createRenderObjects()
            {
                if (_occDiskFlatObj == null) {
                    _occDiskFlatObj = new SSObjectOcclusionQueuery (new SSMeshDisk ());
                    _occDiskFlatObj.renderState.alphaBlendingOn = true;
                    _occDiskFlatObj.renderState.lighted = false;
                    _occDiskFlatObj.renderState.depthWrite = false;
                    _occDiskFlatObj.renderState.doBillboarding = false;
                    _occDiskFlatObj.renderState.matchScaleToScreenPixels = true;
                    _occDiskFlatObj.Scale = new Vector3 (_laser.parameters.occDisk1RadiusPx);
                    var color = _laser.parameters.backgroundColor; // debugging
                    color.A = _laser.parameters.occDisksAlpha;
                    _occDiskFlatObj.MainColor = color;
                    _occDiskScene.AddObject(_occDiskFlatObj);
                }


                if (_occDiskPerspObj == null) {
                    _occDiskPerspObj = new SSObjectOcclusionQueuery (new SSMeshDisk ());
                    _occDiskPerspObj.renderState.alphaBlendingOn = true;
                    _occDiskPerspObj.renderState.lighted = false;
                    _occDiskPerspObj.renderState.depthWrite = false;
                    _occDiskPerspObj.renderState.doBillboarding = false;
                    _occDiskPerspObj.renderState.matchScaleToScreenPixels = false;
                    _occDiskPerspObj.Scale = new Vector3 (_laser.parameters.occDisk2RadiusWU);
                    var color = _laser.parameters.backgroundColor; // debugging
                    color.A = _laser.parameters.occDisksAlpha;
                    _occDiskPerspObj.MainColor = color;
                    _occDiskScene.AddObject(_occDiskPerspObj);
                }

                if (_beamObj == null) {
                    _beamObj = new SLaserBeamObject (_laser, _beamId, _beamScene);
                    _beamScene.AddObject(_beamObj);
                }

                if (_emissionFlareObj == null) {
                //if (false) {
                    _emissionFlareObj = new SLaserEmissionFlareObject (_laser, _beamId, _beamScene, _occDiskFlatObj, _occDiskPerspObj);
                    _flareScene.AddObject(_emissionFlareObj);
                }

                if (_hitFlareObj == null) {
                    _hitFlareObj = new SLaserHitFlareObject (_laser, _beamId, _beamScene);
                    _flareScene.AddObject(_hitFlareObj);
                }
            }
		}

		protected class LaserRuntimeInfo
		{
			protected readonly SLaser _laser;
			protected readonly BeamRuntimeInfo[] _beams;

			public SLaser laser { get { return _laser; } }

			public LaserRuntimeInfo(SLaser laser, 
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

