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
		public SSScene mainScene;
		public SSScene flareScene;

		protected Dictionary<SimpleLaser, BeamRuntimeInfo[]> _laserRuntimes 
			= new Dictionary<SimpleLaser, BeamRuntimeInfo[]> ();

		public SimpleLaserManager (SSScene mainScene, SSScene flareScene)
		{
			this.mainScene = mainScene;
			this.flareScene = flareScene;

			mainScene.preUpdateHooks += _update;
		}

		public SimpleLaser addLaser(SimpleLaserParameters laserParams, 
						     		SSObject srcObject, SSObject dstObject)
		{
			var newLaser = new SimpleLaser (laserParams);
			//newLaser.intensityEnvelope.sustainDuration = sustainDuration;
			newLaser.sourceObject = srcObject;
			newLaser.destObject = dstObject;
			newLaser.postReleaseFunc = this._deleteLaser;

			var beamInfos = new BeamRuntimeInfo[laserParams.numBeams];
			for (int i = 0; i < laserParams.numBeams; ++i) {
				var newBeamInfo = new BeamRuntimeInfo (newLaser, i, mainScene, flareScene);
				beamInfos [i] = newBeamInfo;
			}
			_laserRuntimes.Add (newLaser, beamInfos);

			// debug hacks
			//newLaser.sourceObject = newLaserRuntime.beamRuntimes[0].emissionBillboard;
			//newLaser.sourceTxfm = Matrix4.Identity;

			return newLaser;
		}

		/// <summary>
		/// To be called by laser objects
		/// </summary>
		protected void _deleteLaser(SimpleLaser laser)
		{
			if (_laserRuntimes.ContainsKey(laser)) {
				BeamRuntimeInfo[] beamInfos = _laserRuntimes [laser];
				foreach (var beam in beamInfos) {
					beam.requestDeleteFromScene ();
				}
				_laserRuntimes.Remove (laser);
			}
		}

		protected void _update(float timeElapsed)
		{
			foreach (var hashPair in _laserRuntimes) {
				var laser = hashPair.Key;
				laser.update (timeElapsed); // updates laser and beam data models

				foreach (var beamRuntime in hashPair.Value) {
					beamRuntime.update (timeElapsed);
				}
			}
		}

		protected class BeamRuntimeInfo
		{
			protected readonly SimpleLaser _laser;
			protected readonly int _beamId;

			protected readonly SSScene _mainScene;
			protected readonly SSScene _flareScene;

			protected SSObjectOcclusionQueuery _emissionBillboard = null;
			protected InstancedFlareEffect _emissionFlareObj = null;
			protected SimpleLaserBeamObject _beamObj = null;

			public BeamRuntimeInfo(SimpleLaser laser, int beamId, SSScene mainScene, SSScene flareScene)
			{
				_laser = laser;
				_beamId = beamId;
				_mainScene = mainScene;
				_flareScene = flareScene;
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

				if (_emissionBillboard == null) {
					_emissionBillboard = new SSObjectOcclusionQueuery (new SSMeshDisk ());
					_emissionBillboard.doBillboarding = false;
					var color = _laser.parameters.backgroundColor; // debugging
					color.A = 0.1f;
					_emissionBillboard.MainColor = color;
					_mainScene.AddObject (_emissionBillboard);
				}
				_emissionBillboard.Pos = beam.startPos;
				// TODO consider per-beam orientation
				_emissionBillboard.Orient(_laser.sourceOrient());
			}
		}
			
	}
}

