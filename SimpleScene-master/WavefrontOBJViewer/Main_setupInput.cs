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

namespace WavefrontOBJViewer
{
	partial class WavefrontOBJViewer : OpenTK.GameWindow {

		SSObject selectedObject = null;

		public void setupInput() {
			// hook mouse drag input...
			this.MouseDown += (object sender, MouseButtonEventArgs e) => {
				this.mouseButtonDown = true;

				// cast ray for mouse click
				var clientRect = new System.Drawing.Size(ClientRectangle.Width, ClientRectangle.Height);
				Vector2 mouseLoc = new Vector2(e.X,e.Y);

				SSRay ray = OpenTKHelper.MouseToWorldRay(
					this.scene.ProjectionMatrix,this.scene.InvCameraViewMatrix, clientRect, mouseLoc);

				// Console.WriteLine("mouse ({0},{1}) unproject to ray ({2})",e.X,e.Y,ray);
				// scene.addObject(new SSObjectRay(ray));

				selectedObject = scene.Intersect(ref ray);

			};
			this.MouseUp += (object sender, MouseButtonEventArgs e) => { 
				this.mouseButtonDown = false;
			};
			this.MouseMove += (object sender, MouseMoveEventArgs e) => {
				if (this.mouseButtonDown) {

					// Console.WriteLine("mouse dragged: {0},{1}",e.XDelta,e.YDelta);
					this.scene.ActiveCamera.MouseDeltaOrient(e.XDelta,e.YDelta);
					// this.activeModel.MouseDeltaOrient(e.XDelta,e.YDelta);
				}
			};
			this.MouseWheel += (object sender, MouseWheelEventArgs e) => { 
				// Console.WriteLine("mousewheel {0} {1}",e.Delta,e.DeltaPrecise);
				SSCameraThirdPerson ctp = scene.ActiveCamera as SSCameraThirdPerson;
				if (ctp != null) {
					ctp.followDistance += -e.DeltaPrecise;
				} 
			};

			this.KeyPress += (object sender, KeyPressEventArgs e) => {
				switch (e.KeyChar) {
				case 'w':
					scene.DrawWireFrameMode = SSRenderConfig.NextWireFrameMode(scene.DrawWireFrameMode);
					updateWireframeDisplayText (scene.DrawWireFrameMode);

					// if we need single-pass wireframes, set the GLSL uniform variable
					shaderPgm.Activate();
					shaderPgm.UniShowWireframes = (scene.DrawWireFrameMode == WireframeMode.GLSL_SinglePass);
					break;
				}
			};
		}
	}
}

