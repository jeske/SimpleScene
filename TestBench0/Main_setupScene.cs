// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

using SimpleScene;

namespace TestBench0
{
	partial class TestBench0 : OpenTK.GameWindow
	{
		private bool autoWireframeMode = true;

		public void setupScene() {
			scene.MainShader = mainShader;
			scene.PssmShader = pssmShader;
			scene.InstanceShader = instancingShader;
			scene.FrustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
			scene.BeforeRenderObject += beforeRenderObjectHandler;

			// 0. Add Lights
			var light = new SSDirectionalLight (LightName.Light0);
			light.Direction = new Vector3(0f, 0f, -1f);
			#if true
			if (OpenTKHelper.areFramebuffersSupported ()) {
				if (scene.PssmShader != null && scene.PssmShader.IsValid) {
					light.ShadowMap = new SSParallelSplitShadowMap (TextureUnit.Texture7);
				} else {
					light.ShadowMap = new SSSimpleShadowMap (TextureUnit.Texture7);
				}
			}
			if (!light.ShadowMap.IsValid) {
				light.ShadowMap = null;
			}
			#endif
			scene.AddLight(light);

			#if true
			var smapDebug = new SSObjectHUDQuad (light.ShadowMap.TextureID);
			smapDebug.Scale = new Vector3(0.3f);
			smapDebug.Pos = new Vector3(50f, 200, 0f);
			hudScene.AddObject(smapDebug);
			#endif


			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");
						
			// add drone
			SSObject droneObj = new SSObjectMesh (mesh);
			scene.AddObject (this.activeModel = droneObj);
			droneObj.renderState.lighted = true;
			droneObj.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
			droneObj.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.ShininessMatColor = 10.0f;
			//droneObj.EulerDegAngleOrient(-40.0f,0.0f);
			droneObj.Pos = new OpenTK.Vector3(0,0,-15f);
			droneObj.Name = "drone 1";

			// add second drone
			
			SSObject drone2Obj = new SSObjectMesh(
				SSAssetManager.GetInstance<SSMesh_wfOBJ>("./drone2/", "Drone2.obj")
			);
			scene.AddObject (drone2Obj);
			drone2Obj.renderState.lighted = true;
			drone2Obj.AmbientMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.ShininessMatColor = 10.0f;
			drone2Obj.EulerDegAngleOrient(-40f, 0f);
			drone2Obj.Pos = new OpenTK.Vector3(0f, 0f, 0f);
			drone2Obj.Name = "drone 2";

			// last for the main scene. Add Camera

			var camera = new SSCameraThirdPerson (null);
			camera.followDistance = 50.0f;
			scene.ActiveCamera = camera;
			scene.AddObject (camera);


			// setup a sun billboard object and a sun flare spriter renderer
			{
				var sunDisk = new SSMeshDisk ();
				var sunBillboard = new SSObjectBillboard (sunDisk, true);
				sunBillboard.MainColor = new Color4 (1f, 1f, 0.8f, 1f);
				sunBillboard.Pos = new Vector3 (0f, 0f, 18000f);
				sunBillboard.Scale = new Vector3 (600f);
				sunBillboard.renderState.frustumCulling = false;
				sunBillboard.renderState.lighted = false;
				sunBillboard.renderState.castsShadow = false;
				sunDiskScene.AddObject(sunBillboard);

				SSTexture flareTex = SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "sun_flare.png");
				const float bigOffset = 0.8889f;
				const float smallOffset = 0.125f;
				RectangleF[] flareSpriteRects = {
					new RectangleF(0f, 0f, 1f, bigOffset),
					new RectangleF(0f, bigOffset, smallOffset, smallOffset),
					new RectangleF(smallOffset, bigOffset, smallOffset, smallOffset),
					new RectangleF(smallOffset*2f, bigOffset, smallOffset, smallOffset),
					new RectangleF(smallOffset*3f, bigOffset, smallOffset, smallOffset),
				};
				float[] spriteScales = { 20f, 1f, 2f, 1f, 1f };
				var sunFlare = new SSObjectSunFlare (sunDiskScene, sunBillboard, flareTex, 
													 flareSpriteRects, spriteScales);
				sunFlare.Scale = new Vector3 (2f);
				sunFlare.renderState.lighted = false;
				sunFlareScene.AddObject(sunFlare);
			}

			// instanced asteroid ring
			//if (false)
			{
				var roidmesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("simpleasteroid", "asteroid.obj");
				var ringGen = new BodiesRingGenerator (
					120f, 50f,
					Vector3.Zero, Vector3.UnitY, 250f,
					0f, (float)Math.PI*2f,
					1f, 3f, 1f
				);
				var ringEmitter = new SSBodiesFieldEmitter (ringGen);
				ringEmitter.ParticlesPerEmission = 10000;

				var ps = new SSParticleSystem(10000);
				ps.AddEmitter(ringEmitter);
				Console.WriteLine ("Packing 10k asteroids into a ring. This should take a few seconds...");
				ps.EmitAll();

				asteroidRingRenderer = new SSInstancedMeshRenderer (ps, null, 
																	roidmesh, BufferUsageHint.StaticDraw);
				asteroidRingRenderer.SimulateOnUpdate = false;
				asteroidRingRenderer.Billboarding = SSInstancedMeshRenderer.BillboardingType.None;
				asteroidRingRenderer.AlphaBlendingEnabled = false;
				asteroidRingRenderer.DepthRead = true;
				asteroidRingRenderer.DepthWrite = true;
				asteroidRingRenderer.Name = "instanced asteroid renderer";
				scene.AddObject (asteroidRingRenderer);
			}

			// particle system test
			// particle systems should be drawn last (if it requires alpha blending)
			//if (false)
			{
				// setup an emitter
				var box = new ParticlesSphereGenerator (new Vector3(0f, 0f, 0f), 10f);
				var emitter = new SSParticlesFieldEmitter (box);
				//emitter.EmissionDelay = 5f;
				emitter.ParticlesPerEmission = 1;
				emitter.EmissionInterval = 0.5f;
				emitter.Life = 1000f;
				emitter.ColorOffsetComponentMin = new Color4 (0.5f, 0.5f, 0.5f, 1f);
				emitter.ColorOffsetComponentMax = new Color4 (1f, 1f, 1f, 1f);
				emitter.VelocityComponentMax = new Vector3 (.3f);
				emitter.VelocityComponentMin = new Vector3 (-.3f);
				emitter.AngularVelocityMin = new Vector3 (-0.5f);
				emitter.AngularVelocityMax = new Vector3 (0.5f);
				emitter.DragMin = 0f;
				emitter.DragMax = .1f;
				RectangleF[] uvRects = new RectangleF[18*6];
				float tileWidth = 1f / 18f;
				float tileHeight = 1f / 6f;
				for (int r = 0; r < 6; ++r) {
					for (int c = 0; c < 18; ++c) {
						uvRects [r*18 + c] = new RectangleF (tileWidth * (float)r, 
							tileHeight * (float)c,
							tileWidth, 
							tileHeight);
					}
				}
				emitter.SpriteRectangles = uvRects;

				var periodicExplosiveForce = new SSPeriodicExplosiveForceEffector ();
				periodicExplosiveForce.EffectInterval = 3f;
				periodicExplosiveForce.ExplosiveForceMin = 1000f;
				periodicExplosiveForce.ExplosiveForceMax = 5000f;
				periodicExplosiveForce.EffectDelay = 5f;
				periodicExplosiveForce.CenterMin = new Vector3 (-30f, -30f, -30f);
				periodicExplosiveForce.CenterMax = new Vector3 (30f, 30f, 30f);
				//periodicExplosiveForce.Center = new Vector3 (10f);

				// make a particle system
				SSParticleSystem cubesPs = new SSParticleSystem (1000);
				cubesPs.AddEmitter(emitter);
				cubesPs.AddEffector (periodicExplosiveForce);

				// test a renderer
				var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "elements.png");
				var cubesRenderer = new SSInstancedMeshRenderer (cubesPs, tex, SSTexturedCube.Instance);
				cubesRenderer.Pos = new Vector3 (0f, 0f, -30f);
				cubesRenderer.AlphaBlendingEnabled = false;
				cubesRenderer.Billboarding = SSInstancedMeshRenderer.BillboardingType.None;
				cubesRenderer.DepthRead = true;
				cubesRenderer.DepthWrite = true;
				cubesRenderer.Name = "cube particle renderer";
				scene.AddObject(cubesRenderer);
				//cubesRenderer.renderState.visible = false;

				// test explositons
				//if (false)
				{
					AcmeExplosionRenderer aer = new AcmeExplosionRenderer (100);
					aer.Pos = cubesRenderer.Pos;
					scene.AddObject (aer);

					periodicExplosiveForce.ExplosionEventHandlers += (pos, force) => { 
						aer.ShowExplosion(pos, force/periodicExplosiveForce.ExplosiveForceMin*1.5f); 
					};
				}
			}
		}

		public void setupEnvironment() 
		{
			// add skybox cube
			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./skybox/","skybox.obj");
			SSObject skyboxCube = new SSObjectMesh(mesh);
			environmentScene.AddObject(skyboxCube);
			skyboxCube.Scale = new Vector3(0.7f);
			skyboxCube.renderState.lighted = false;

			// scene.addObject(skyboxCube);

			SSObject skyboxStars = new SSObjectMesh(new SSMesh_Starfield(1600));
			environmentScene.AddObject(skyboxStars);
			skyboxStars.renderState.lighted = false;

		}

		SSObjectGDISurface_Text fpsDisplay;

		SSObjectGDISurface_Text wireframeDisplay;

		public void setupHUD() 
		{
			hudScene.ProjectionMatrix = Matrix4.Identity;

			// HUD Triangle...
			//SSObject triObj = new SSObjectTriangle ();
			//hudScene.addObject (triObj);
			//triObj.Pos = new Vector3 (50, 50, 0);
			//triObj.Scale = new Vector3 (50.0f);

			// HUD text....
			fpsDisplay = new SSObjectGDISurface_Text ();
			fpsDisplay.Label = "FPS: ...";
			hudScene.AddObject (fpsDisplay);
			fpsDisplay.Pos = new Vector3 (10f, 10f, 0f);
			fpsDisplay.Scale = new Vector3 (1.0f);

			// wireframe mode text....
			wireframeDisplay = new SSObjectGDISurface_Text ();
			hudScene.AddObject (wireframeDisplay);
			wireframeDisplay.Pos = new Vector3 (10f, 40f, 0f);
			wireframeDisplay.Scale = new Vector3 (1.0f);
			updateWireframeDisplayText ();

			// HUD text....
			var testDisplay = new SSObject2DSurface_AGGText ();
			testDisplay.Label = "TEST AGG";
			hudScene.AddObject (testDisplay);
			testDisplay.Pos = new Vector3 (50f, 100f, 0f);
			testDisplay.Scale = new Vector3 (1.0f);
		}

		private void beforeRenderObjectHandler (Object obj, SSRenderConfig renderConfig)
		{
			// TODO not activate 
			mainShader.Activate ();
			if (autoWireframeMode) {
				if (obj == selectedObject) {
					renderConfig.drawWireframeMode = WireframeMode.GLSL_SinglePass;
					mainShader.UniShowWireframes = true;

				} else {
					renderConfig.drawWireframeMode = WireframeMode.None;
					mainShader.UniShowWireframes = false;
				}
			} else {
				mainShader.UniShowWireframes = (scene.DrawWireFrameMode == WireframeMode.GLSL_SinglePass);
			}
		}

		private void updateWireframeDisplayText() 
		{
			string selectionInfo;
			WireframeMode techinqueInfo;
			if (autoWireframeMode) {
				selectionInfo = "selected";
				techinqueInfo = (selectedObject == null ? WireframeMode.None : WireframeMode.GLSL_SinglePass);
			} else {
				selectionInfo = "all";
				techinqueInfo = scene.DrawWireFrameMode;
			}
			wireframeDisplay.Label = String.Format (
				"press '1' to toggle wireframe mode: [{0}:{1}]",
				selectionInfo, techinqueInfo.ToString()
			);
		}
	}
}