using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using SimpleScene.Util;

namespace SimpleScene
{
	// TODO have an easy way to update laser start and finish
	// TODO ability to "hold" laser active

	// TODO pulse, interference effects
	// TODO laser "drift"

	public class SSLaserParameters
	{
		public Color4 backgroundColor = Color4.Purple;
		public Color4 overlayColor = Color4.White;
		public float laserWidth = 0.1f;    // width in world units
	}

	public class SSObjectLaserRenderer : SSObject
	{
		protected List<SSLaser> _lasers;

		public SSObjectLaserRenderer ()
		{
		}

		/// <summary>
		/// Adds the laser to a list of lasers. Laser start, ends, fade and other effects
		/// are to be updated from somewhere else.
		/// </summary>
		/// <returns>The handle to LaserInfo which can be used for updating start and 
		/// end.</returns>
		public SSLaser AddLaser(SSLaserParameters parameters)
		{
			var li = new SSLaser();
			li.start = Vector3.Zero;
			li.end = Vector3.Zero;
			li.parameters = parameters;
			_lasers.Add (li);
			return li;
		}

		public class SSLaser
		{
			public Vector3 start;
			public Vector3 end;
			public SSLaserParameters parameters;

			public float laserOpacity;
		}
	}
}

