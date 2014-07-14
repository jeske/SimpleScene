// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

// http://www.opentk.com/book/export/html/1039

namespace WavefrontOBJViewer
{

	// here we setup a basic "game window" and hook some input functions


	partial class Game : OpenTK.GameWindow
	{

		Matrix4 projection;
		Matrix4 invCameraViewMatrix;

		SSScene scene;
		SSScene hudScene;
		SSScene environmentScene;

		bool mouseButtonDown = false;
		SSObject activeModel;
		

		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public Game()
			: base(
				800, 600, 
				GraphicsMode.Default, // color format
				"WavefrontOBJLoader",
				GameWindowFlags.Default,  // windowed mode 
				DisplayDevice.Default,    // primary monitor
				2, 2,  // opengl version
				GraphicsContextFlags.Debug
				)
		{
			VSync = VSyncMode.On;
		}

		public void setupInput() {
			// hook mouse drag input...
			this.Mouse.ButtonDown += (object sender, MouseButtonEventArgs e) => {
				this.mouseButtonDown = true;

				// cast ray for mouse click
				var clientRect = new System.Drawing.Size(ClientRectangle.Width, ClientRectangle.Height);
				Vector2 mouseLoc = new Vector2(e.X,e.Y);

				SSRay ray = OpenTKHelper.MouseToWorldRay(ref this.projection,invCameraViewMatrix, clientRect, mouseLoc);

				// Console.WriteLine("mouse ({0},{1}) unproject to ray ({2})",e.X,e.Y,ray);
				// scene.addObject(new SSObjectRay(ray));

				scene.Intersect(ref ray);

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
					ctp.followDistance += e.DeltaPrecise;
				} 
			};

			this.KeyPress += (object sender, KeyPressEventArgs e) => {
				switch (e.KeyChar) {
				case 'w':
					SSRenderConfig.toggle(ref scene.renderConfig.drawWireframeMode);
					GL.UseProgram (this.shaderPgm.ProgramID);
					GL.Uniform1 (GL.GetUniformLocation (this.shaderPgm.ProgramID, "showWireframes"), (int) (scene.renderConfig.drawWireframeMode == WireframeMode.GLSL ? 1 : 0));
					break;
				}
			};
		}

		protected override void OnFocusedChanged (EventArgs e)
		{
			base.OnFocusedChanged (e);
			mouseButtonDown = false;
		}

		/// <summary>Load resources here.</summary>
		/// <param name="e">Not used.</param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);			
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
			GL.UseProgram(shaderPgm.ProgramID);
			GL.Uniform2(
				GL.GetUniformLocation(this.shaderPgm.ProgramID, "WIN_SCALE"),
				(float)ClientRectangle.Width, (float)ClientRectangle.Height);
		}
		
		/// <summary>
		/// Called when it is time to setup the next frame. Add you game logic here.
		/// </summary>
		/// <param name="e">Contains timing information for framerate independent logic.</param>
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			environmentScene.Update();
			scene.Update ();
			hudScene.Update ();

			if (Keyboard[Key.Escape])
				Exit();
		}
		
		/// <summary>
		/// Called when it is time to render the next frame. Add your rendering code here.
		/// </summary>
		/// <param name="e">Contains timing information.</param>
		protected override void OnRenderFrame(FrameEventArgs e)
		{
			base.OnRenderFrame(e);

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


		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (Game game = new Game())
			{
				Console.WriteLine("GL Version = {0}",GL.GetString(StringName.Version));
				Console.WriteLine("GL Shader Version = {0}", GL.GetString(StringName.ShadingLanguageVersion));
				
				game.setupShaders();  // before scene
				
				game.setupInput ();

				game.setupScene ();
				game.setupEnvironment ();
				game.setupHUD ();

				game.Run(30.0);
			}
		}
	}
}