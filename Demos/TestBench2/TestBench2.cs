using System;
using SimpleScene;
using SimpleScene.Demos;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace TestBench2
{
	public class TestBench2 : TestBenchBaseWindow
	{
		protected SSScene laserScene = new SSScene ();

		public TestBench2 ()
			: base("TestBench2: Lasers")
		{
		}

		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (var game = new TestBench2()) {
				game.Run(30.0);
			}
		}

		protected override void setupScene ()
		{
			base.setupScene ();

			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");

			// add drones
			var droneObj1 = new SSObjectMesh (mesh);
			droneObj1.Pos = new OpenTK.Vector3(-20f, 0f, -15f);
			droneObj1.Orient(Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI/2f));
			droneObj1.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
			droneObj1.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj1.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj1.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj1.Name = "green drone";
			droneObj1.MainColor = Color4.Green;
			 scene.AddObject (droneObj1);

			var droneObj2 = new SSObjectMesh (mesh);
			droneObj2.Pos = new OpenTK.Vector3(20f, 0f, -15f);
			droneObj2.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
			droneObj2.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj2.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj2.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj2.Name = "red drone";
			droneObj2.MainColor = Color4.Red;
			scene.AddObject (droneObj2);

			// add lasers
			SSLaserParameters laserParams = new SSLaserParameters();
			laserParams.backgroundColor = Color4.Lime;
			laserParams.overlayColor = Color4.White;
			laserParams.backgroundWidth = 2f;
			laserParams.startPointScale = 1f;

			SSLaser laser = new SSLaser ();
			laser.start = new Vector3 (-17f, 1f, -15f);
			//laser.end = new Vector3 (19f, 0f, -15f);
			laser.end = new Vector3 (200f, 0f, -15f);
			laser.parameters = laserParams;

			SimpleLaserObject lo = new SimpleLaserObject (laser);
			lo.Name = "laser test";
			laserScene.AddObject (lo);

			#if false
			// debug start location of the laser
			droneObj1.Scale = new Vector3 (0.01f);
			droneObj1.Pos = laser.start;
			#endif
		}

		protected override void renderScenes (
			float fovy, float aspect, float nearPlane, float farPlane, 
			ref Matrix4 mainSceneView, ref Matrix4 mainSceneProj, 
			ref Matrix4 rotationOnlyView, ref Matrix4 screenProj)
		{
			base.renderScenes (
				fovy, aspect, nearPlane, farPlane, 
				ref mainSceneView, ref mainSceneProj, ref rotationOnlyView, ref screenProj);

			laserScene.renderConfig.invCameraViewMatrix = mainSceneView;
			laserScene.renderConfig.projectionMatrix = mainSceneProj;

			GL.Enable (EnableCap.CullFace);
			GL.CullFace (CullFaceMode.Back);
			GL.Enable(EnableCap.DepthTest);
			GL.Disable(EnableCap.DepthClamp);
			GL.DepthFunc(DepthFunction.Less);
			GL.DepthMask (false);

			//GL.ColorMask (false, false, false, true);
			//GL.ClearColor (0f, 0f, 0f, 1f);
			//GL.Clear (ClearBufferMask.ColorBufferBit);

			laserScene.Render ();
		}
	}
}

