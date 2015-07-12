using System;
using System.Collections.Generic;

namespace SimpleScene.Demos
{
	public class SimpleLaserManager
	{
		/// <summary>
		/// Scene that the lasers will be added to/removed from
		/// </summary>
		public SSScene scene;

		protected List<SimpleLaserObject> _laserObjects = new List<SimpleLaserObject>();

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

			for (int i = 0; i < laserParams.numBeams; ++i) {
				var newObj = new SimpleLaserObject (newLaser, i, this.scene);
				_laserObjects.Add (newObj);
				scene.AddObject (newObj);
			}

			return newLaser;
		}

		/// <summary>
		/// To be called by laser objects
		/// </summary>
		protected void _deleteLaser(SimpleLaser laser)
		{
			for (int i = 0; i < _laserObjects.Count; ++i) {
				var lo = _laserObjects [i];
				if (lo.laser == laser) {
					lo.renderState.toBeDeleted = true;
					_laserObjects.RemoveAt (i);
					return;
				}
			}
		}

		// TODO switch targets while firing
	}
}

