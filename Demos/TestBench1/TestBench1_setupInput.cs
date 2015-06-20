// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

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
	partial class TestBench1 : TestBenchBaseWindow
	{
		private void keyUpHandler1(object sender, KeyboardKeyEventArgs e)
		{
			if (!base.Focused) return;

			switch (e.Key) {
			case Key.Q:
				renderMesh4AttackSm.requestTransition ("attack");
				renderMesh5AttackSm.requestTransition ("attack");
				break;
			}
		}

		protected override void setupInput ()
		{
			base.setupInput ();
			this.KeyUp += keyUpHandler1;
		}
	}
}

