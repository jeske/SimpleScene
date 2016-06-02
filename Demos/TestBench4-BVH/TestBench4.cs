// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using System.Threading;
using System.Globalization;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

using SimpleScene;
using SimpleScene.Demos;

namespace TestBench4
{
	partial class TestBench4 : TestBenchBootstrap
	{
        
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (TestBench4 game = new TestBench4())
			{
				game.Run(30.0);
			}
		}

		public TestBench4()
			: base("TestBench4: BVH")
		{
		}


		/// <summary>
		/// Called when it is time to setup the next frame. Add you game logic here.
		/// </summary>
		/// <param name="e">Contains timing information for framerate independent logic.</param>
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame (e);

		}


	}
}