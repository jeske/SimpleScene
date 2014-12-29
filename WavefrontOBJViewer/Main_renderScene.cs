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

	partial class WavefrontOBJViewer : OpenTK.GameWindow
	{

		FPSCalculator fpsCalc = new FPSCalculator();
		float animateSecondsOffset;
		/// <summary>
		/// Called when it is time to render the next frame. Add your rendering code here.
		/// </summary>
		/// <param name="e">Contains timing information.</param>
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			base.OnRenderFrame (e);

			// NOTE: this is a workaround for the fact that the ThirdPersonCamera is not parented to the target...
			//   before we can remove this, we need to parent it properly, currently it's transform only follows
			//   the target during Update() and input event processing.
			scene.Update ((float)e.Time);  
			
			fpsCalc.newFrame (e.Time);
			fpsDisplay.Label = String.Format ("FPS: {0:0.00}", fpsCalc.AvgFramesPerSecond);


			// setup the GLSL uniform for shader animation
			animateSecondsOffset += (float)e.Time;
			if (animateSecondsOffset > 1000.0f) {
				animateSecondsOffset -= 1000.0f;
			}
			shaderPgm.Activate();
			shaderPgm.UniAnimateSecondsOffset = (float)animateSecondsOffset;


			/////////////////////////////////////////
			// clear the render buffer....
			GL.DepthMask (true);
			GL.ClearColor (0.0f, 0.0f, 0.0f, 0.0f); // black
			GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			float fovy = (float)Math.PI / 4;
			float aspect = ClientRectangle.Width / (float)ClientRectangle.Height;

			/////////////////////////////////////////
			// render the "environment" scene
			// 
			// todo: should move this after the scene render, with a proper depth
			//  test, because it's more efficient when it doesn't have to write every pixel
			{
				// setup infinite projection for cubemap
				Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView (fovy, aspect, 0.1f, 2.0f);
				environmentScene.ProjectionMatrix = projMatrix;

				// create a matrix of just the camera rotation only (it needs to stay at the origin)
				var rotOnly = Matrix4.CreateFromQuaternion(scene.ActiveCamera.worldMat.ExtractRotation()).Inverted ();
				environmentScene.InvCameraViewMatrix = rotOnly;

				GL.Enable (EnableCap.CullFace);
				GL.CullFace (CullFaceMode.Back);
				GL.Disable (EnableCap.DepthTest);
				GL.Disable (EnableCap.DepthClamp);
				GL.DepthMask (false);

				environmentScene.Render ();
			}
			/////////////////////////////////////////
			// rendering the "main" 3d scene....
			{
				// setup the inverse matrix of the active camera...
				scene.InvCameraViewMatrix = scene.ActiveCamera.worldMat.Inverted ();

				// setup the view projection. technically only need to do this on window resize..
				scene.ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView (fovy, aspect, 1.0f, 500.0f);

				GL.Enable (EnableCap.CullFace);
				GL.CullFace (CullFaceMode.Back);
				GL.Enable (EnableCap.DepthTest);
				GL.Enable (EnableCap.DepthClamp);
				GL.DepthMask (true);
				
				// render 3d content...
				// scene.renderConfig.renderBoundingSpheres = true;
				scene.Render ();
			}
			////////////////////////////////////////
			//  render HUD scene
			{
				// Note that a default identity view matrix is used and doesn't need to be changed

				// setup an orthographic projection looking down the +Z axis, same as:
				// GL.Ortho (0, ClientRectangle.Width, ClientRectangle.Height, 0, -1, 1);			
				hudScene.ProjectionMatrix 
				= Matrix4.CreateOrthographicOffCenter(0, ClientRectangle.Width, ClientRectangle.Height, 0, -1, 1);

				GL.Enable (EnableCap.CullFace);
				GL.CullFace (CullFaceMode.Back);
				GL.Disable(EnableCap.DepthTest);
				GL.Disable(EnableCap.DepthClamp);
				GL.DepthMask(false);

				hudScene.Render ();

			}
			SwapBuffers();
		}


		/// <summary>
		/// Called when your window is resized. Set your viewport here. It is also
		/// a good place to set up your projection matrix (which probably changes
		/// along when the aspect ratio of your window).
		/// </summary>
		/// <param name="e">Not used.</param>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			this.mouseButtonDown = false; // hack to fix resize mouse issue..

			// setup the viewport projection

			GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

			// setup WIN_SCALE for our shader...
			shaderPgm.Activate();
			shaderPgm.UniWinScale = ClientRectangle;
		}

	}
		
}