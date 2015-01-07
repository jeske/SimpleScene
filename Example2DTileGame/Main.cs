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


namespace Example2DTileGame
{

	// here we setup a basic "game window" and hook some input functions


	partial class Example2DTileGame : OpenTK.GameWindow
	{

		SSScene scene = new SSScene();
		SSScene hudScene = new SSScene();		

		bool mouseButtonDown = false;				

		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public Example2DTileGame()
			: base(
				#if false
				800, 600, 
				GraphicsMode.Default, // color format
				"WavefrontOBJLoader",
				GameWindowFlags.Default,  // windowed mode 
				DisplayDevice.Default,    // primary monitor
				2, 2,  // opengl version
				GraphicsContextFlags.Debug
				#endif
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
			
			scene.Update ((float)e.Time);
			hudScene.Update ((float)e.Time);

			if (Keyboard[Key.Escape])
				Exit();
		}


		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{

            // this can be used to force other culture settings for testing..
            // System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-NL");

			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (Example2DTileGame game = new Example2DTileGame())
			{
				Console.WriteLine("GL Version = {0}",GL.GetString(StringName.Version));
				Console.WriteLine("GL Shader Version = {0}", GL.GetString(StringName.ShadingLanguageVersion));

				// setup asset manager contexts
				// these help the asset manager find the "Asset" directy up above the bin/obj/Debug
				// output directories... 
				SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("./Assets"));
				SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("../../Assets"));
				SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("../../../Assets"));
				SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("../../../../Assets"));
								
				game.setupInput ();

				game.setupScene ();
				game.setupHUD ();

				// game.VSync = VSyncMode.Off;
				game.Run(30.0);
			}
		}
	}
}