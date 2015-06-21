using System;

using SimpleScene;
using SimpleScene.Demos;

namespace TestBench2
{
	public class TestBench2 : TestBenchBaseWindow
	{
		public TestBench2 ()
			: base("TestBench2: Lasers")
		{
		}

		/// <summary>
		/// How to declare a window in a derived test bench:
		/// </summary>
		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (var game = new TestBench2()) {
				game.Run(30.0);
			}
		}
	}
}

