// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;


namespace WavefrontOBJViewer
{

	// here we setup a basic "game window" and hook some input functions


	partial class Game : OpenTK.GameWindow
	{

		SSScene scene;

		bool mouseButtonDown = false;

		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public Game()
			: base(800, 600, GraphicsMode.Default, "OpenTK Quick Start Sample")
		{
			VSync = VSyncMode.On;

			// setup the scene
			scene = new SSScene ();
			this.setupScene();
		
			// hook mouse drag input...
			this.Mouse.ButtonDown += (object sender, MouseButtonEventArgs e) => {
				this.mouseButtonDown = true;
			};
			this.Mouse.ButtonUp += (object sender, MouseButtonEventArgs e) => { 
				this.mouseButtonDown = false;
			};
			this.Mouse.Move += (object sender, MouseMoveEventArgs e) => {
				if (this.mouseButtonDown) {
					Console.WriteLine("mouse dragged: {0},{1}",e.XDelta,e.YDelta);
					scene.activeCamera.MouseDeltaOrient(e.XDelta,e.YDelta);
				}
			};
			this.Mouse.WheelChanged += (object sender, MouseWheelEventArgs e) => { 
				Console.WriteLine("mousewheel {0} {1}",e.Delta,e.DeltaPrecise);
				SSCameraThirdPerson ctp = scene.activeCamera as SSCameraThirdPerson;
				if (ctp != null) {
					ctp.followDistance -= e.DeltaPrecise;
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

			// setup the viewport projection

			GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

		}
		
		/// <summary>
		/// Called when it is time to setup the next frame. Add you game logic here.
		/// </summary>
		/// <param name="e">Contains timing information for framerate independent logic.</param>
		protected override void OnUpdateFrame(FrameEventArgs e)
		{
			base.OnUpdateFrame(e);

			scene.Update ();

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
			
			scene.Update();
			
			GL.Enable(EnableCap.DepthTest);
			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f); // black
			// GL.ClearColor (System.Drawing.Color.Blue);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// setup the view projection, including the active camera matrix
			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView ((float)Math.PI / 4, Width / (float)Height, 1.0f, 500.0f);
			scene.adjustProjectionMatrixForActiveCamera (ref projection);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix(ref projection);

			// render
			scene.Render ();

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
				game.Run(30.0);
			}
		}
	}
}