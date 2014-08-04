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
			this.Mouse.ButtonDown += (object sender, MouseButtonEventArgs e) => {
				this.mouseButtonDown = true;

				// cast ray for mouse click
				var clientRect = new System.Drawing.Size(ClientRectangle.Width, ClientRectangle.Height);
				Vector2 mouseLoc = new Vector2(e.X,e.Y);

				SSRay ray = OpenTKHelper.MouseToWorldRay(
					this.scene.renderConfig.projectionMatrix,
					this.scene.renderConfig.invCameraViewMat, clientRect, mouseLoc);

				// Console.WriteLine("mouse ({0},{1}) unproject to ray ({2})",e.X,e.Y,ray);
				// scene.addObject(new SSObjectRay(ray));

				selectedObject = scene.Intersect(ref ray);

			};
			this.Mouse.ButtonUp += (object sender, MouseButtonEventArgs e) => { 
				this.mouseButtonDown = false;
			};
			this.Mouse.Move += (object sender, MouseMoveEventArgs e) => {
				if (this.mouseButtonDown) {

					// Console.WriteLine("mouse dragged: {0},{1}",e.XDelta,e.YDelta);
					this.scene.activeCamera.MouseDeltaOrient(e.XDelta,e.YDelta);
					// this.activeModel.MouseDeltaOrient(e.XDelta,e.YDelta);
				}
			};
			this.Mouse.WheelChanged += (object sender, MouseWheelEventArgs e) => { 
				// Console.WriteLine("mousewheel {0} {1}",e.Delta,e.DeltaPrecise);
				SSCameraThirdPerson ctp = scene.activeCamera as SSCameraThirdPerson;
				if (ctp != null) {
					ctp.followDistance += -e.DeltaPrecise;
				} 
			};

			this.KeyPress += (object sender, KeyPressEventArgs e) => {
				switch (e.KeyChar) {
				case 'w':
					SSRenderConfig.toggle(ref scene.renderConfig.drawWireframeMode);
					updateWireframeDisplayText (scene.renderConfig);

					// if we need single-pass wireframes, set the GLSL uniform variable
					GL.UseProgram (this.shaderPgm.ProgramID);
					GL.Uniform1 (GL.GetUniformLocation (this.shaderPgm.ProgramID, "showWireframes"), (int) (scene.renderConfig.drawWireframeMode == WireframeMode.GLSL_SinglePass ? 1 : 0));
					break;
				}
			};
		}
	}
}

