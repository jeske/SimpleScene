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

	partial class Game : OpenTK.GameWindow
	{

		float animateSecondsOffset;
		/// <summary>
		/// Called when it is time to render the next frame. Add your rendering code here.
		/// </summary>
		/// <param name="e">Contains timing information.</param>
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

			// NOTE: this is a workaround for the fact that the ThirdPersonCamera is not parented to the target...
			//   before we can remove this, we need to parent it properly, currently it's transform only follows
			//   the target during Update() and input event processing.
			scene.Update ();  

			FPS_frames++;
			FPS_time += e.Time;
			if (FPS_time > 2.0) {
				fpsDisplay.Label = String.Format ("FPS: {0:0.00}", ((double)FPS_frames / FPS_time));
				FPS_frames = 0;
				FPS_time = 0.0;
			}

			animateSecondsOffset += (float)e.Time;
			if (animateSecondsOffset > 1000.0f) {
				animateSecondsOffset -= 1000.0f;
			}
			GL.UseProgram (this.shaderPgm.ProgramID);
			GL.Uniform1 (GL.GetUniformLocation (this.shaderPgm.ProgramID, "animateSecondsOffset"), (float)animateSecondsOffset);			


			/////////////////////////////////////////
			// clear the render buffer....
			GL.Enable(EnableCap.DepthTest);
			GL.DepthMask (true);
			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f); // black
			// GL.ClearColor (System.Drawing.Color.White);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			/////////////////////////////////////////
			// render the "environment" scene
			// 
			// todo: should move this after the scene render, with a proper depth
			//  test, because it's more efficient when it doesn't have to write every pixel
			{
				GL.Disable(EnableCap.DepthTest);
				GL.Enable(EnableCap.CullFace);
				GL.CullFace (CullFaceMode.Front);
				GL.Disable(EnableCap.DepthClamp);

				GL.MatrixMode(MatrixMode.Projection);
				Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView (
					(float)Math.PI / 2, 
					1.0f, 
					0.1f, 2.0f);
				GL.LoadMatrix(ref projMatrix);

				// create a matrix of just the camera rotation only (it needs to stay at the origin)
				Matrix4 environmentCameraMatrix = Matrix4.CreateFromQuaternion(scene.activeCamera.worldMat.ExtractRotation()).Inverted();
				environmentScene.Render(environmentCameraMatrix);
			}
			/////////////////////////////////////////
			// rendering the "main" 3d scene....
			{
				GL.Enable (EnableCap.CullFace);
				GL.CullFace (CullFaceMode.Back);
				GL.Enable(EnableCap.DepthTest);
				GL.Enable(EnableCap.DepthClamp);

				GL.DepthMask (true);

				// GL.Enable(IndexedEnableCap.Blend,0);

				// setup the view projection, including the active camera matrix
				projection = Matrix4.CreatePerspectiveFieldOfView ((float)Math.PI / 4, ClientRectangle.Width / (float)ClientRectangle.Height, 1.0f, 500.0f);
				// scene.adjustProjectionMatrixForActiveCamera (ref projection);
				projection = Matrix4.CreateTranslation (0, 0, -5) * projection;
				GL.MatrixMode(MatrixMode.Projection);
				GL.LoadMatrix(ref projection);

				// compute the inverse matrix of the active camera...
				invCameraViewMatrix = scene.activeCamera.worldMat;
				invCameraViewMatrix.Invert();

				// render 3d content...
				scene.SetupLights (invCameraViewMatrix);
				scene.Render (invCameraViewMatrix);
			}
			////////////////////////////////////////
			//  render HUD scene

			GL.Disable (EnableCap.DepthTest);
			GL.Disable (EnableCap.CullFace);
			GL.DepthMask (false);
			GL.MatrixMode (MatrixMode.Projection);
			GL.LoadIdentity ();
			GL.Ortho (0, ClientRectangle.Width, ClientRectangle.Height, 0, -1, 1);
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadIdentity ();

			hudScene.Render (Matrix4.Identity);

			SwapBuffers();
		}


	}
		
}