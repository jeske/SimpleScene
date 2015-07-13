using System;
using System.Collections.Generic;

namespace SimpleScene.Demos
{
	public class SimpleLaserManager
	{
		// TODO switch targets while firing

		/// <summary>
		/// Scene that the lasers will be added to/removed from
		/// </summary>
		public SSScene scene;

		protected List<LaserRuntime> _laserRuntimes = new List<LaserRuntime>();

		public SimpleLaserManager (SSScene scene)
		{
			this.scene = scene;
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
			newLaserRuntime.laser = newLaser;
			newLaserRuntime.beams = new List<BeamRuntime> (laserParams.numBeams);
			for (int i = 0; i < laserParams.numBeams; ++i) {
				var beam = new BeamRuntime ();
				beam.beamObj = new SimpleLaserBeamObject (newLaser, i, this.scene);
			}
			_laserRuntimes.Add (newLaserRuntime);

			return newLaser;
		}

		/// <summary>
		/// To be called by laser objects
		/// </summary>
		protected void _deleteLaser(SimpleLaser laser)
		{
			for (int i = 0; i < _laserRuntimes.Count; ++i) {
				var runTime = _laserRuntimes [i];
				if (runTime.laser == laser) {
					foreach (var beam in runTime.beams) {
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
					_laserRuntimes.RemoveAt (i);
					return;
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
			public SimpleLaser laser;
			public List<BeamRuntime> beams;
		}
	}
}

