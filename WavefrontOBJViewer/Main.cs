// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using System.Threading;
using System.Globalization;
using System.Drawing;

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

		SSScene scene = new SSScene();
		SSScene sunDiskScene = new SSScene ();
		SSScene sunFlareScene = new SSScene ();
		SSScene hudScene = new SSScene();
		SSScene environmentScene = new SSScene();

		bool mouseButtonDown = false;
		SSObject activeModel;
		
		SSMainShaderProgram mainShader;
		SSPssmShaderProgram pssmShader;
		SSInstanceShaderProgram instancingShader;

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
			using (WavefrontOBJViewer game = new WavefrontOBJViewer())
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

				game.mainShader = new SSMainShaderProgram(); // before scene
				game.pssmShader = new SSPssmShaderProgram ();
				game.instancingShader = new SSInstanceShaderProgram ();

				game.setupInput ();

				game.setupScene ();
				game.setupEnvironment ();
				game.setupHUD ();

				// game.VSync = VSyncMode.Off;
				game.Run(30.0);
			}
		}

		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public WavefrontOBJViewer()
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
			restoreClientWindowLocation();
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

			environmentScene.Update((float)e.Time);
			scene.Update ((float)e.Time);
			hudScene.Update ((float)e.Time);

			if (Keyboard[Key.Escape])
				Exit();
		}

		private void restoreClientWindowLocation() {
			int w = Prefs.getPref("WINDOW_SIZE_W", this.Size.Width);
			int h = Prefs.getPref("WINDOW_SIZE_H", this.Size.Height); 			
			this.ClientSize = new Size(w,h);

			int x = Prefs.getPref("WINDOW_POS_X", this.Location.X);
			int y = Prefs.getPref("WINDOW_POS_Y", this.Location.Y);
			// this.Location = new Point(x, y);

			this.Bounds = new Rectangle(x,y,w,h);

			// TODO: restore monitor for dual-monitor setups
		}

		private void saveClientWindowLocation() {
			Prefs.setPref("WINDOW_SIZE_W", this.Bounds.Width);
			Prefs.setPref("WINDOW_SIZE_H", this.Bounds.Height);
			Prefs.setPref("WINDOW_POS_X", this.Bounds.X);
			Prefs.setPref("WINDOW_POS_Y", this.Bounds.Y);
		}
	}
}