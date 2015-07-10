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

		protected List<SimpleLaserObject> _laserObjects;

		public void update()
		{
			// TODO implement laser ADSR
		}

		public SSLaser addLaser(SSLaserParameters laserParams, 
								SSObject srcObject, SSObject dstObject)
		{
			var newLaser = new SSLaser ();
			newLaser.parameters = laserParams;
			newLaser.srcObj = srcObject;
			newLaser.dstObj = dstObject;

			var newObj = new SimpleLaserObject (newLaser);
			_laserObjects.Add (newObj);

			scene.AddObject (newObj);

			return newLaser;
		}

		// TODO update laser destination and source
		// TODO move laser
		public void removeLaser(SSLaser laser)
		{
			for (int i = 0; i < _laserObjects.Count; ++i) {
				var lo = _laserObjects [i];
				if (lo.laser == laser) {
					scene.RemoveObject (lo);
					_laserObjects.RemoveAt (i);
					return;
				}
			}
		}

		public SimpleLaserManager (SSScene scene)
		{
			this.scene = scene;
		}
	}
}

