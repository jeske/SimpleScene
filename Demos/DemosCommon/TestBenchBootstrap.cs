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

namespace SimpleScene.Demos
{
	/// <summary>
	/// This class provides a bootstrap mechanism to setup a basic "game window", hook some input 
	/// functions, and add a few rendered objects to jump-start testing or development of SimpleScene
	/// features. When done carefully this shouldn't take more than a few minutes. Most of the bootstrap 
	/// functionality details are based on GL tutorial for OpenTK 
	/// (http://www.opentk.com/book/export/html/1039)
	/// 
	/// To start a new test bench project:
	///
	/// 1) Create a new empty project under SimpleDemos (lets say TestBenchX)
	/// 
	/// 2) Add a class derived from TestBenchBootstrap (lets say TestBenchX); 
	///    set as main class in the project settings
	///
	/// 3) Setup the static Main() function of the derived class like this:
	/// 
	/// <code>
	/// static void Main()
	/// {
	///	    // The 'using' idiom guarantees proper resource cleanup.
	///	    // We request 30 UpdateFrame events per second, and unlimited
	///	    // RenderFrame events (as fast as the computer can handle).
	///	    using (var game = new TestBenchX()) {
	///         game.Run(30.0);
	///	    }
	/// }
	/// </code>
	/// 
	/// 4) Provide a constructor that at least sets the window title:
	/// 
	/// <code>
	/// public TestBenchX()
	///	    : base("TestBenchX")
	/// {
	/// }
	/// 
	/// This should be enough to get the new game window project working. Next you can...
	/// 
	/// 5) Override one or more functions to change or extend the bootstrap functionality:
	/// - Override setupScene() or setupEnvironment() to change or extend scene setup.
	/// - Override renderScenes() (and renderShadowmaps(), if needed) if you have added another instance 
	///   of a SSScene and need to render it
	/// - Override setupInput() to change existing or add new key input handlers
	/// - etc; most functions are made virtual to encourage customizability and experimenting
	/// 
	/// See TestBench0, TestBench1, etc. for example uses of this base class.
	/// </code>
	/// </summary>
	public abstract partial class TestBenchBootstrap : OpenTK.GameWindow
	{
        protected SSScene main3dScene;
        protected SSScene alpha3dScene;
        protected SSScene sunDiskScene;
        protected SSScene sunFlareScene;
        protected SSScene hudScene;
        protected SSScene environmentScene;

		protected bool mouseButtonDown = false;

		protected SSMainShaderProgram mainShader;
		protected SSPssmShaderProgram pssmShader;
		protected SSInstanceShaderProgram instancingShader;
		protected SSInstancePssmShaderProgram instancingPssmShader;

		#if false
		/// <summary>
		/// How to declare a window in a derived test bench:
		/// </summary>
		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (var game = new TestBenchX()) {
				game.Run(30.0);
			}
		}
		#endif

		/// <summary>Creates a 800x600 window with the specified title.</summary>
		public TestBenchBootstrap(string windowName)
			: base(
				#if false
				800, 600, 
				GraphicsMode.Default, // color format
				windowName,
				GameWindowFlags.Default,  // windowed mode 
				DisplayDevice.Default,    // primary monitor
				2, 2,  // opengl version
				GraphicsContextFlags.Debug
				#endif
				)
		{
			// this can be used to force other culture settings for testing..
			// System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-NL");

			this.Title = windowName;
			VSync = VSyncMode.On;
			restoreClientWindowLocation();

			Console.WriteLine("GL Version = {0}",GL.GetString(StringName.Version));
			Console.WriteLine("GL Shader Version = {0}", GL.GetString(StringName.ShadingLanguageVersion));

			// setup asset manager contexts
			// these help the asset manager find the "Asset" directy up above the bin/obj/Debug
			// output directories... 
			SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("./Assets"));
			SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("../../Assets"));
			SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("../../../Assets"));
			SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("../../../../Assets"));
			SSAssetManager.AddAssetArchive(new SSAssetArchiveHandler_FileSystem("../../../DemosCommon/Assets"));

			mainShader = new SSMainShaderProgram(); // before mscene
			if (!mainShader.IsValid) {
				throw new Exception ("Failed to build the main shader");
			}
			pssmShader = new SSPssmShaderProgram ();
			if (!pssmShader.IsValid) {
				pssmShader = null;
			}
			instancingShader = new SSInstanceShaderProgram ();
			if (!instancingShader.IsValid) {
				instancingShader = null;
			} else {
				instancingShader.debugLocations ();
			}
			instancingPssmShader = new SSInstancePssmShaderProgram ();
			if (!instancingPssmShader.IsValid) {
				instancingPssmShader = null;
			}

			setupInput ();
			setupScene ();
			setupCamera ();
			setupEnvironment ();
			setupHUD ();
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

            float time = (float)e.Time;
			main3dScene.Update (time);
            environmentScene.Update(time);
            alpha3dScene.Update(time);
            sunDiskScene.Update(time);
			hudScene.Update (time);
            sunFlareScene.Update(time);

			driveCamera ((float)e.Time);
		}

		protected virtual void driveCamera(float deltaT)
		{
            // TODO this doesn't work well after turning upside down
			if (!base.Focused) return; // no window focus = no action

			SSCameraThirdPerson ctp = main3dScene.ActiveCamera as SSCameraThirdPerson;
			if (ctp == null) return;

			KeyboardState state = OpenTK.Input.Keyboard.GetState ();
			float cameraDisplacement = (ctp.followDistance + 5f) * deltaT * -0.33f;
			if (state.IsKeyDown(Key.W)) {
				ctp.basePos += ctp.Dir * cameraDisplacement;
			}
			if (state.IsKeyDown(Key.S)) {
				ctp.basePos -= ctp.Dir * cameraDisplacement;
			}

			if (state.IsKeyDown(Key.A)) {
				ctp.basePos += ctp.Right * cameraDisplacement;
			}
			if (state.IsKeyDown(Key.D)) {
				ctp.basePos -= ctp.Right * cameraDisplacement;
			}

			if (state.IsKeyDown(Key.Space)) {
				ctp.basePos -= ctp.Up * cameraDisplacement;
			}
			if (state.IsKeyDown(Key.C) || state.IsKeyDown(Key.ControlLeft)) {
				ctp.basePos += ctp.Up * cameraDisplacement;
			}
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