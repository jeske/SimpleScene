// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using System.Threading;
using System.Globalization;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

using SimpleScene;

// http://www.opentk.com/book/export/html/1039

namespace WavefrontOBJViewer
{

	// here we setup a basic "game window" and hook some input functions


	partial class WavefrontOBJViewer : OpenTK.GameWindow
	{

		Matrix4 projection;
		Matrix4 invCameraViewMatrix;

		SSScene scene;
		SSScene hudScene;
		SSScene environmentScene;

		bool mouseButtonDown = false;
		SSObject activeModel;
		
		// FPS counter
		int FPS_frames = 0;
		double FPS_time = 0.0;


		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public WavefrontOBJViewer()
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
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{

            // this is a hack to work around some i18n bugs related to culture and number parsing.
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (WavefrontOBJViewer game = new WavefrontOBJViewer())
			{
				Console.WriteLine("GL Version = {0}",GL.GetString(StringName.Version));
				Console.WriteLine("GL Shader Version = {0}", GL.GetString(StringName.ShadingLanguageVersion));
				
				game.setupShaders();  // before scene
				
				game.setupInput ();

				game.setupScene ();
				game.setupEnvironment ();
				game.setupHUD ();

				// game.VSync = VSyncMode.Off;
				game.Run(30.0);
			}
		}
	}
}