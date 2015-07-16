using System;
using System.Collections.Generic;
using OpenTK;

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

		protected Dictionary<SimpleLaser, LaserRuntime> _laserRuntimes 
			= new Dictionary<SimpleLaser, LaserRuntime> ();

		public SimpleLaserManager (SSScene mainScene, SSScene flareScene)
		{
			this.mainScene = mainScene;
			this.flareScene = flareScene;

			mainScene.preUpdateHooks += _update;
		}

		public SimpleLaser addLaser(SimpleLaserParameters laserParams, 
						     		SSObject srcObject, SSObject dstObject,
							    	float sustainDuration = float.PositiveInfinity)
		{
			var newLaser = new SimpleLaser (laserParams);
			newLaser.parameters.intensityEnvelope.sustainDuration = sustainDuration;
			newLaser.sourceObject = srcObject;
			newLaser.destObject = dstObject;
			newLaser.postReleaseFunc = this._deleteLaser;

			var newLaserRuntime = new LaserRuntime ();
			//newLaserRuntime.laser = newLaser;
			newLaserRuntime.beamRuntimes = new BeamRuntime[laserParams.numBeams];
			for (int i = 0; i < laserParams.numBeams; ++i) {
				var newBeamRuntime = new BeamRuntime ();

				newBeamRuntime.beamObj = new SimpleLaserBeamObject (newLaser, i, this.mainScene);
				mainScene.AddObject (newBeamRuntime.beamObj);

				newLaserRuntime.beamRuntimes [i] = newBeamRuntime;
			}
			_laserRuntimes.Add (newLaser, newLaserRuntime);

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
				LaserRuntime runTime = _laserRuntimes [laser];
				foreach (var beam in runTime.beamRuntimes) {
					if (beam.beamObj != null) {
						beam.beamObj.renderState.toBeDeleted = true;
					}
					if (beam.emissionBillboard != null) {
						beam.emissionBillboard.renderState.toBeDeleted = true;
					}
					if (beam.emissionFlareObj != null) {
						beam.emissionFlareObj.renderState.toBeDeleted = true;
					}
				}
				_laserRuntimes.Remove (laser);
			}
		}

		protected void _update(float timeElapsed)
		{
			foreach (var hashPair in _laserRuntimes) {
				var laser = hashPair.Key;
				var sourcePos = laser.sourcePos();
				foreach (var beam in hashPair.Value.beamRuntimes) {
					if (beam.emissionBillboard == null) {
						beam.emissionBillboard = new SSObjectBillboard (new SSMeshDisk (), true);
						beam.emissionBillboard.MainColor = laser.parameters.backgroundColor;
						mainScene.AddObject (beam.emissionBillboard);
					}
					beam.emissionBillboard.Pos = sourcePos;
				}
			}
		}

		protected class BeamRuntime
		{
			public SimpleLaserBeamObject beamObj;

			public SSObjectBillboard emissionBillboard = null;
			public SimpleSunFlareMesh emissionFlareMesh = null;
			public SSObjectMesh emissionFlareObj = null;
		}

		protected class LaserRuntime
		{
			//public SimpleLaser laser;
			public BeamRuntime[] beamRuntimes;
		}
	}
}

