using System;
using SimpleScene;
using SimpleScene.Demos;
using OpenTK;
using OpenTK.Graphics;

namespace TestBench2
{
	public class TestBench2 : TestBenchBaseWindow
	{
		public TestBench2 ()
			: base("TestBench2: Lasers")
		{
		}

		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (var game = new TestBench2()) {
				game.Run(30.0);
			}
		}

		protected override void setupScene ()
		{
			base.setupScene ();

			SSLaserParameters laserParams = new SSLaserParameters();
			laserParams.backgroundColor = Color4.Lime;
			laserParams.overlayColor = Color4.White;
			laserParams.backgroundWidth = 10f;

			SSLaser laser = new SSLaser ();
			laser.start = new Vector3 (-10f, 0f, 0f);
			laser.end = new Vector3 (10f, 0f, 0f);
			laser.parameters = laserParams;

			SimpleLaserObject lo = new SimpleLaserObject (laser);
			scene.AddObject (lo);
		}
	}
}

