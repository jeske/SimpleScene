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
	partial class TestBench1 : OpenTK.GameWindow
	{
		private bool autoWireframeMode = true;

		public void setupScene1() {
			scene.MainShader = mainShader;
			scene.PssmShader = pssmShader;
			scene.InstanceShader = instancingShader;
			scene.InstancePssmShader = instancingPssmShader;
			scene.FrustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
			scene.BeforeRenderObject += beforeRenderObjectHandler;

			// 0. Add Lights
			var light = new SSDirectionalLight (LightName.Light0);
			light.Direction = new Vector3(0f, 0f, -1f);
			#if true
			if (OpenTKHelper.areFramebuffersSupported ()) {
				if (scene.PssmShader != null && scene.InstancePssmShader != null) {
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

			// last for the main scene. Add Camera
			var camera = new SSCameraThirdPerson (null);
			camera.followDistance = 50.0f;
			camera.basePos = new Vector3 (0f, 10f, 0f);
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

			// checkerboard floor
			#if true
			{
				SSTexture tex = SSAssetManager.GetInstance<SSTexture> (".", "checkerboard.png");
				const float tileSz = 4f;
				const int gridSz = 10;
				var tileVertices = new SSVertex_PosNormTex[SSTexturedNormalQuad.c_doubleFaceVertices.Length];
				SSTexturedNormalQuad.c_doubleFaceVertices.CopyTo(tileVertices, 0);
				for (int v = 0; v < tileVertices.Length; ++v) {
					tileVertices[v].TexCoord *= (float)gridSz;
				}

				var quadMesh = new SSVertexMesh<SSVertex_PosNormTex>(tileVertices);
				quadMesh.textureMaterial = new SSTextureMaterial(tex);
				var tileObj = new SSObjectMesh(quadMesh);
				tileObj.Name = "Tiles";
				tileObj.Selectable = false;
				tileObj.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, (float)Math.PI/2f));
				tileObj.Scale = new Vector3(tileSz * gridSz);
				//tileObj.boundingSphere = new SSObjectSphere(0f);
				scene.AddObject(tileObj);
			}
			#endif

			// skeleton mesh test
			#if true
			{
				SSSkeletalAnimation animIdle
					= SSAssetManager.GetInstance<SSSkeletalAnimationMD5>("./boneman", "boneman_idle.md5anim");
				SSSkeletalAnimation animRunning
					= SSAssetManager.GetInstance<SSSkeletalAnimationMD5>("./boneman", "boneman_running.md5anim");
				SSSkeletalAnimation animAttack
				= SSAssetManager.GetInstance<SSSkeletalAnimationMD5>("./boneman", "boneman_attack.md5anim");

				SSSkeletalMesh[] meshes 
					= SSAssetManager.GetInstance<SSSkeletalMeshMD5[]>("./boneman", "boneman.md5mesh");
				var tex = SSAssetManager.GetInstance<SSTexture>("./boneman", "skin.png");
				foreach (var skeliMesh in meshes) {

					#if true
					var renderMesh1 = new SSSkeletalRenderMesh(skeliMesh);
					renderMesh1.AddChannel(0, "all");
					renderMesh1.PlayAnimation(0, animRunning, true, 0f);

					var renderMesh2 = new SSSkeletalRenderMesh(skeliMesh);
					renderMesh2.AddChannel(0, "all");
					renderMesh2.AddChannel(1, "LeftClavicle", "RightClavicle");
					renderMesh2.PlayAnimation(0, animIdle, true, 0f);
					renderMesh2.PlayAnimation(1, animRunning, true, 0f);

					var renderMesh3 = new SSSkeletalRenderMesh(skeliMesh);
					renderMesh3.AddChannel(0, "all");
					renderMesh3.PlayAnimation(0, animIdle, true, 0f);

					var obj1 = new SSObjectMesh(renderMesh1);
					obj1.MainColor = Color4.DarkRed;
					obj1.Name = "red bones";
					obj1.Pos = new Vector3(6f, 0f, -12f);
					obj1.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
					scene.AddObject(obj1);

					var obj2 = new SSObjectMesh(renderMesh2);
					obj2.MainColor = Color.Green;
					obj2.Name = "green bones";
					obj2.Pos = new Vector3(0f, 0f, -12f);
					obj2.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
					scene.AddObject(obj2);

					var obj3 = new SSObjectMesh(renderMesh3);
					obj3.MainColor = Color.DarkCyan;
					obj3.Name = "blue bones";
					obj3.Pos = new Vector3(-6f, 0f, -12f);
					obj3.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
					scene.AddObject(obj3);
					#endif

					// state machine test
					var renderMesh4 = new SSSkeletalRenderMesh(skeliMesh);
					//renderMesh4.AddChannel(0, "all");
					renderMesh4.TimeScale = 0.15f;

					var obj4 = new SSObjectMesh(renderMesh4);
					obj4.MainColor = Color.DarkMagenta;
					obj4.Name = "magenta bones";
					obj4.Pos = new Vector3(0f, 0f, 0f);
					obj4.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
					scene.AddObject(obj4);

					#if true
					renderMesh4.AddChannel(0, "all");
					skeletonWalkSm = renderMesh4.AddNewStateMachine();

					skeletonWalkSm.AddState("idle");
					skeletonWalkSm.AddStateAnimation("idle", 0, animRunning);

					skeletonWalkSm.AddState("running");
					skeletonWalkSm.AddStateAnimation("running", 0, animRunning);

					skeletonWalkSm.AddState("attack");
					skeletonWalkSm.AddStateAnimation("attack", 0, animAttack);

					skeletonWalkSm.AddAnimationEndsTransition("idle", "running", 0f, 0);
					skeletonWalkSm.AddAnimationEndsTransition("running", "idle", 0f, 0);
					#endif

					#if true
					renderMesh4.AddChannel(1, "LeftClavicle", "RightClavicle");
					skeletonAttackSm = renderMesh4.AddNewStateMachine();

					skeletonAttackSm.AddState("inactive");
					skeletonAttackSm.AddStateAnimation("inactive", 1, null, true);

					skeletonAttackSm.AddState("attack");
					skeletonAttackSm.AddStateAnimation("attack", 1, animAttack, true);

					skeletonAttackSm.AddStateTransition("inactive", "attack", 0.5f);
					skeletonAttackSm.AddAnimationEndsTransition("attack", "inactive", 0.5f, 1);
					#endif
				}
			}
			#endif

			#if true
			// bob mesh test
			{
				var bobMeshes = SSAssetManager.GetInstance<SSSkeletalMeshMD5[]>(
					"./bob_lamp/", "bob_lamp_update.md5mesh");
				var bobAnim = SSAssetManager.GetInstance<SSSkeletalAnimationMD5>(
					"./bob_lamp/", "bob_lamp_update.md5anim");
				var bobBodyTex = SSAssetManager.GetInstance<SSTexture>(
					"./bob_lamp/", "bob_body.png");
				var bobRender = new SSSkeletalRenderMesh(bobMeshes);
				bobRender.AddChannel(0, "all");
				bobRender.PlayAnimation(0, bobAnim, true, 0f);
				bobRender.textureMaterial = new SSTextureMaterial(bobBodyTex);
				bobRender.alphaBlendingEnabled = true;
				bobRender.TimeScale = 0.5f;
				var bobObj = new SSObjectMesh(bobRender);
				bobObj.Name = "Bob";
				bobObj.Pos = new Vector3(10f, 0f, 10f);
				bobObj.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				scene.AddObject(bobObj);
			}
			#endif

			// particle system test
			// particle systems should be drawn last (if it requires alpha blending)
			#if false			
			{
				// setup an emitter
				var box = new ParticlesSphereGenerator (new Vector3(0f, 0f, 0f), 10f);
				var emitter = new SSParticlesFieldEmitter (box);
				//emitter.EmissionDelay = 5f;
				emitter.particlesPerEmission = 1;
				emitter.emissionInterval = 0.5f;
				emitter.life = 1000f;
				emitter.colorOffsetComponentMin = new Color4 (0.5f, 0.5f, 0.5f, 1f);
				emitter.colorOffsetComponentMax = new Color4 (1f, 1f, 1f, 1f);
				emitter.velocityComponentMax = new Vector3 (.3f);
				emitter.velocityComponentMin = new Vector3 (-.3f);
				emitter.angularVelocityMin = new Vector3 (-0.5f);
				emitter.angularVelocityMax = new Vector3 (0.5f);
				emitter.dragMin = 0f;
				emitter.dragMax = .1f;
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
				emitter.spriteRectangles = uvRects;

				var periodicExplosiveForce = new SSPeriodicExplosiveForceEffector ();
				periodicExplosiveForce.effectInterval = 3f;
				periodicExplosiveForce.explosiveForceMin = 1000f;
				periodicExplosiveForce.explosiveForceMax = 5000f;
				periodicExplosiveForce.effectDelay = 5f;
				periodicExplosiveForce.centerMin = new Vector3 (-30f, -30f, -30f);
				periodicExplosiveForce.centerMax = new Vector3 (30f, 30f, 30f);
				//periodicExplosiveForce.Center = new Vector3 (10f);

				// make a particle system
				SSParticleSystem cubesPs = new SSParticleSystem (1000);
				cubesPs.addEmitter(emitter);
				cubesPs.addEffector (periodicExplosiveForce);
			}
			#endif
		}

		public void setupEnvironment() 
		{
			// add skybox cube
			#if true
			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./skybox","skybox.obj");
			SSObject skyboxCube = new SSObjectMesh(mesh);
			environmentScene.AddObject(skyboxCube);
			skyboxCube.Scale = new Vector3(0.7f);
			skyboxCube.renderState.lighted = false;

			// scene.addObject(skyboxCube);

			SSObject skyboxStars = new SSObjectMesh(new SSMesh_Starfield(1600));
			environmentScene.AddObject(skyboxStars);
			skyboxStars.renderState.lighted = false;
			#endif

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
            bool showWireFrames;
			if (autoWireframeMode) {
				if (obj == selectedObject) {
					renderConfig.drawWireframeMode = WireframeMode.GLSL_SinglePass;
                    showWireFrames = true;
				} else {
					renderConfig.drawWireframeMode = WireframeMode.None;
                    showWireFrames = false;
				}
			} else { // manual
                showWireFrames = (scene.DrawWireFrameMode == WireframeMode.GLSL_SinglePass);
			}
            mainShader.Activate();
            mainShader.UniShowWireframes = showWireFrames;
            instancingShader.Activate();
            instancingShader.UniShowWireframes = showWireFrames;

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