// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing; // RectangleF

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
        public float nearPlane = 1f;
        public float farPlane = 5000f;

		protected FPSCalculator fpsCalc = new FPSCalculator();
		protected float animateSecondsOffset;
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
            if (mainScene.ActiveCamera != null) {
                mainScene.ActiveCamera.preRenderUpdate((float)e.Time);
            }
			
			fpsCalc.newFrame (e.Time);
			fpsDisplay.Label = String.Format ("FPS: {0:0.00}", fpsCalc.AvgFramesPerSecond);


			// setup the GLSL uniform for shader animation
			animateSecondsOffset += (float)e.Time;
			if (animateSecondsOffset > 1000.0f) {
				animateSecondsOffset -= 1000.0f;
			}
            mainShader.Activate();
			mainShader.UniAnimateSecondsOffset = (float)animateSecondsOffset;
            instancingShader.Activate();
			instancingShader.UniAnimateSecondsOffset = (float)animateSecondsOffset;


			float fovy = (float)Math.PI / 4;
			float aspect = ClientRectangle.Width / (float)ClientRectangle.Height;

			// setup the inverse matrix of the active camera...
			Matrix4 mainSceneView = mainScene.ActiveCamera.worldMat.Inverted();
			// setup the view projection. technically only need to do this on window resize..
			Matrix4 mainSceneProj = Matrix4.CreatePerspectiveFieldOfView (fovy, aspect, nearPlane, farPlane);
			// create a matrix of just the camera rotation only (it needs to stay at the origin)
			Matrix4 rotationOnlyView = Matrix4.CreateFromQuaternion (mainSceneView.ExtractRotation ());
			// create an orthographic projection matrix looking down the +Z matrix; for hud scene and sun flare scene
			Matrix4 screenProj = Matrix4.CreateOrthographicOffCenter (0, ClientRectangle.Width, ClientRectangle.Height, 0, -1, 1);

            /////////////////////////////////////////
            // clear the render buffer....
            GL.DepthMask (true);
            GL.ColorMask (true, true, true, true);
            GL.ClearColor (0.0f, 0.0f, 0.0f, 0.0f); // black
            GL.Clear (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			renderShadowmaps (fovy, aspect, nearPlane, farPlane, 
					          ref mainSceneView, ref mainSceneProj, ref rotationOnlyView, ref screenProj);
			renderScenes (fovy, aspect, nearPlane, farPlane, 
						  ref mainSceneView, ref mainSceneProj, ref rotationOnlyView, ref screenProj);

			// setup the view-bounds.
			SwapBuffers();
		}

		protected virtual void renderShadowmaps(
			float fovy, float aspect, float nearPlane, float farPlane,
			ref Matrix4 mainSceneView, ref Matrix4 mainSceneProj,
			ref Matrix4 rotationOnlyView, ref Matrix4 screenProj
			)
		{
			#if true
			mainScene.renderConfig.projectionMatrix = mainSceneProj;
			mainScene.renderConfig.invCameraViewMatrix = mainSceneView;
			mainScene.RenderShadowMap(fovy, aspect, nearPlane, farPlane);
			#endif
		}

		protected virtual void renderScenes(
			float fovy, float aspect, float nearPlane, float farPlane,
			ref Matrix4 mainSceneView, ref Matrix4 mainSceneProj,
			ref Matrix4 rotationOnlyView, ref Matrix4 screenProj)
		{
			GL.Viewport(ClientRectangle.X, ClientRectangle.Y, 
					    ClientRectangle.Width, ClientRectangle.Height);

			/////////////////////////////////////////
			// rendering the "main" 3d scene....
			{
				mainScene.renderConfig.invCameraViewMatrix = mainSceneView;
				mainScene.renderConfig.projectionMatrix = mainSceneProj;
				mainScene.Render ();
			}
            /////////////////////////////////////////
            // render the "environment" scene
            // 
            // todo: should move this after the scene render, with a proper depth
            //  test, because it's more efficient when it doesn't have to write every pixel
            {
                //skyboxCube.Scale = new Vector3 (1000f);

                // setup infinite projection for cubemap
                environmentScene.renderConfig.projectionMatrix 
                = Matrix4.CreatePerspectiveFieldOfView (fovy, aspect, 0.1f, 2.0f);
                environmentScene.renderConfig.invCameraViewMatrix = rotationOnlyView;
                environmentScene.Render ();
            }

			/////////////////////////////////////////
			// rendering the sun dsk scene....
			{
                sunBillboard.Pos = new Vector3 (0f, 0f, farPlane-1f);
				sunDiskScene.renderConfig.invCameraViewMatrix = rotationOnlyView;
				sunDiskScene.renderConfig.projectionMatrix = mainSceneProj;
				sunDiskScene.Render();
			}

			////////////////////////////////////////
			//  render the sun flare scene
			{
				// Note that a default identity view matrix is used and doesn't need to be changed	
				sunFlareScene.renderConfig.projectionMatrix = screenProj;
				sunFlareScene.Render ();
			}

			////////////////////////////////////////
			//  render the HUD scene
			{
				// Note that a default identity view matrix is used and doesn't need to be changed
				hudScene.renderConfig.projectionMatrix = screenProj;
				hudScene.Render ();
			}
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
            Size sz = this.ClientSize;
			System.Console.WriteLine ("Resizing to width = " + sz.Width +
									 " and height = " + sz.Height);

			// setup WIN_SCALE for our shader...
            mainShader.Activate();
			mainShader.UniWinScale = sz;
            instancingShader.Activate();
			instancingShader.UniWinScale = sz;

			saveClientWindowLocation ();
		}

		protected virtual void updateTextDisplay() 
		{
			string selectionInfo;
			WireframeMode techinqueInfo;
			if (autoWireframeMode) {
				selectionInfo = "selected";
				techinqueInfo = (selectedObject == null ? WireframeMode.None : WireframeMode.GLSL_SinglePass);
			} else {
				selectionInfo = "all";
				techinqueInfo = mainScene.renderConfig.drawWireframeMode;
			}
			textDisplay.Label = String.Format (
				"press '1' to toggle wireframe mode: [{0}:{1}]",
				selectionInfo, techinqueInfo.ToString()
			);
		}

	}
		
}