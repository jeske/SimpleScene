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

namespace TestBench1
{
	partial class TestBench1 : TestBenchBootstrap
	{
		protected SSPolarJoint renderMesh5NeckJoint =  null;
		protected SSAnimationStateMachineSkeletalController renderMesh4AttackSm = null;
		protected SSAnimationStateMachineSkeletalController renderMesh5AttackSm = null;

		protected SSSimpleObjectTrackingJoint tracker0;
		protected SSSimpleObjectTrackingJoint tracker4;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (TestBench1 game = new TestBench1())
			{
				game.Run(30.0);
			}
		}

		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public TestBench1()
			: base("TestBench1")
		{
		}

		/// <summary>
		/// Called when it is time to setup the next frame. Add you game logic here.
		/// </summary>
		/// <param name="e">Contains timing information for framerate independent logic.</param>
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			if (renderMesh5NeckJoint != null) {
				renderMesh5NeckJoint.theta.value += (float)Math.PI / 2f * (float)e.Time;
			}
		}
	}
}