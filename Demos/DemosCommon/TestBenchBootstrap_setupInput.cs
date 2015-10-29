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

namespace SimpleScene.Demos
{
	partial class TestBenchBootstrap : OpenTK.GameWindow 
	{
		protected SSObject selectedObject = null;

		protected virtual void setupInput()
		{
			this.MouseDown += mouseDownHandler;
			this.MouseUp += mouseUpHandler;
			this.MouseMove += mouseMoveHandler;
			this.MouseWheel += mouseWheelHandler;
			this.KeyUp += keyUpHandler;
		}

		protected virtual void mouseDownHandler(object sender, MouseButtonEventArgs e)
		{
			if (!base.Focused) return;

			this.mouseButtonDown = true;

			// cast ray for mouse click
			var clientRect = new System.Drawing.Size(ClientRectangle.Width, ClientRectangle.Height);
			Vector2 mouseLoc = new Vector2(e.X,e.Y);

			SSRay ray = OpenTKHelper.MouseToWorldRay(
                this.mainScene.renderConfig.projectionMatrix, this.mainScene.renderConfig.invCameraViewMatrix, clientRect, mouseLoc);

			// Console.WriteLine("mouse ({0},{1}) unproject to ray ({2})",e.X,e.Y,ray);
			// scene.addObject(new SSObjectRay(ray));

			selectedObject = mainScene.Intersect(ref ray);
			updateTextDisplay ();
		}

		protected virtual void mouseUpHandler(object sender, MouseButtonEventArgs e)
		{ 
			this.mouseButtonDown = false;
		}

		protected virtual void mouseMoveHandler(object sender, MouseMoveEventArgs e)
		{
			if (this.mouseButtonDown) {

				// Console.WriteLine("mouse dragged: {0},{1}",e.XDelta,e.YDelta);
				this.mainScene.ActiveCamera.MouseDeltaOrient(e.XDelta,e.YDelta);
				// this.activeModel.MouseDeltaOrient(e.XDelta,e.YDelta);
			}
		}

		protected virtual void mouseWheelHandler(object sender, MouseWheelEventArgs e)
		{
			if (!base.Focused) return;

			// Console.WriteLine("mousewheel {0} {1}",e.Delta,e.DeltaPrecise);
			SSCameraThirdPerson ctp = mainScene.ActiveCamera as SSCameraThirdPerson;
			if (ctp != null) {
				ctp.followDistance += -e.DeltaPrecise;
			} 
		}

		protected virtual void keyUpHandler(object sender, KeyboardKeyEventArgs e)
		{
			if (!base.Focused) return;

			switch (e.Key) {
			case Key.Number1:
				System.Console.WriteLine ("Toggling wireframe");
				if (autoWireframeMode == true) {
					autoWireframeMode = false;
				} else {
                    mainScene.renderConfig.drawWireframeMode = SSRenderConfig.NextWireFrameMode(mainScene.renderConfig.drawWireframeMode);
                    if (mainScene.renderConfig.drawWireframeMode == WireframeMode.None) {
						autoWireframeMode = true; // rollover completes toggling modes
					}
				}
				updateTextDisplay ();
				break;
			case Key.Escape:
				Exit ();
				break;
			}
		}
	}
}

