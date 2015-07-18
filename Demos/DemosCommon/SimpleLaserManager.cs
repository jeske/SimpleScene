using System;
using System.Collections.Generic;
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

			protected readonly SSScene _mainScene;
			protected readonly SSScene _flareScene;

			protected readonly SSObjectOcclusionQueuery _emissionBillboard = null;
			protected readonly InstancedFlareEffect _emissionFlareObj = null;
			protected readonly SimpleLaserBeamObject _beamObj = null;

			public BeamRuntimeInfo(SimpleLaser laser, int beamId, SSScene mainScene, SSScene flareScene)
			{
				_laser = laser;
				_beamId = beamId;
				_mainScene = mainScene;
				_flareScene = flareScene;

				_emissionBillboard = new SSObjectOcclusionQueuery (new SSMeshDisk ());
				_emissionBillboard.renderState.doBillboarding = false;
				var color = _laser.parameters.backgroundColor; // debugging
				color.A = 0.1f;
				_emissionBillboard.MainColor = color;
				_mainScene.AddObject (_emissionBillboard);

				_beamObj = new SimpleLaserBeamObject(_laser, _beamId, mainScene);
				_mainScene.AddObject(_beamObj);
			}

			public void requestDeleteFromScene()
			{
				if (_beamObj != null) {
					_beamObj.renderState.toBeDeleted = true;
				}
				if (_emissionBillboard != null) {
					_emissionBillboard.renderState.toBeDeleted = true;
				}
				if (_emissionFlareObj != null) {
					_emissionFlareObj.renderState.toBeDeleted = true;
				}
			}

			public void update(float timeElapsed)
			{
				var beam = _laser.beam (_beamId);
				if (beam == null) return;

				_emissionBillboard.Pos = beam.startPos;
				// TODO consider per-beam orient
				_emissionBillboard.Orient(_laser.sourceOrient());
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

				// do an early update to make sure we don't render bogus data on the first render frame
				update(0f);
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

