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
            scene.renderConfig.PssmShader = pssmShader;
            scene.renderConfig.InstanceShader = instancingShader;
            scene.renderConfig.InstancePssmShader = instancingPssmShader;
            scene.renderConfig.frustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
            scene.BeforeRenderObject += beforeRenderObjectHandler;

			// 0. Add Lights
			var light = new SSDirectionalLight (LightName.Light0);
			light.Direction = new Vector3(0f, 0f, -1f);
			#if true
			if (OpenTKHelper.areFramebuffersSupported ()) {
                if (scene.renderConfig.PssmShader != null && scene.renderConfig.InstancePssmShader != null) {
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
				var renderMesh0 = new SSSkeletalRenderMesh(skeliMesh);
				var obj0 = new SSObjectMesh(renderMesh0);
				obj0.MainColor = Color4.DarkGray;
				obj0.Name = "grey bones (bind pose)";
				obj0.Pos = new Vector3(-18f, 0f, -18f);
				obj0.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				scene.AddObject(obj0);
				#endif

				#if true
				var renderMesh1 = new SSSkeletalRenderMesh(skeliMesh);
				var obj1 = new SSObjectMesh(renderMesh1);
				obj1.MainColor = Color4.DarkRed;
				obj1.Name = "red bones (running loop)";
				obj1.Pos = new Vector3(6f, 0f, -12f);
				obj1.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				scene.AddObject(obj1);

				renderMesh1.PlayAnimationLoop(animRunning, 0f);
				#endif

				#if true
				var renderMesh2 = new SSSkeletalRenderMesh(skeliMesh);
				renderMesh2.PlayAnimationLoop(animIdle, 0f, "all");
				renderMesh2.PlayAnimationLoop(animRunning, 0f, "LeftClavicle", "RightClavicle");
				var obj2 = new SSObjectMesh(renderMesh2);
				obj2.MainColor = Color.Green;
				obj2.Name = "green bones (idle + running loop mixed)";
				obj2.Pos = new Vector3(0f, 0f, -12f);
				obj2.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				scene.AddObject(obj2);
				#endif

				#if true
				var renderMesh3 = new SSSkeletalRenderMesh(skeliMesh);
				renderMesh3.PlayAnimationLoop(animIdle, 0f, "all");
				var obj3 = new SSObjectMesh(renderMesh3);
				obj3.MainColor = Color.DarkCyan;
				obj3.Name = "blue bones (idle loop)";
				obj3.Pos = new Vector3(-6f, 0f, -12f);
				obj3.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				scene.AddObject(obj3);
				#endif

				#if true
				// state machine test (in slow motion)
				var renderMesh4 = new SSSkeletalRenderMesh(skeliMesh);
				renderMesh4.TimeScale = 0.25f;

				var obj4 = new SSObjectMesh(renderMesh4);
				obj4.MainColor = Color.DarkMagenta;
				obj4.Name = "magenta bones (looping idle/walk; interactive attack; slowmo)";
				obj4.Pos = new Vector3(-12f, 0f, 0f);
				obj4.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				scene.AddObject(obj4);

				var skeletonWalkDescr = new SSAnimationStateMachine();
				skeletonWalkDescr.AddState("idle", animIdle, true);		
				skeletonWalkDescr.AddState("running1", animRunning);
				skeletonWalkDescr.AddState("running2", animRunning);
				skeletonWalkDescr.AddAnimationEndsTransition("idle", "running1", 0.3f);
				skeletonWalkDescr.AddAnimationEndsTransition("running1", "running2", 0f);
				skeletonWalkDescr.AddAnimationEndsTransition("running2", "idle", 0.3f);
				var renderMesh4WallSm = renderMesh4.AddStateMachine(skeletonWalkDescr, "all");
	
				var skeletonAttackDescr = new SSAnimationStateMachine();
				skeletonAttackDescr.AddState("inactive", null, true);
				skeletonAttackDescr.AddState("attack", animAttack);
				skeletonAttackDescr.AddStateTransition(null, "attack", 0.5f);
				skeletonAttackDescr.AddAnimationEndsTransition("attack", "inactive", 0.5f);
				renderMesh4AttackSm = renderMesh4.AddStateMachine(skeletonAttackDescr, "LeftClavicle", "RightClavicle");
				#endif

				#if true
				// another mesh, using the same state machine but running at normal speed
				var renderMesh5 = new SSSkeletalRenderMesh(skeliMesh);
				var renderMesh5WalkSm = renderMesh5.AddStateMachine(skeletonWalkDescr, "all");
				renderMesh5AttackSm = renderMesh5.AddStateMachine(skeletonAttackDescr, "LeftClavicle", "RightClavicle");
				var obj5 = new SSObjectMesh(renderMesh5);
				obj5.Name = "orange bones (looping idle/walk, interactive attack + parametric neck rotation)";
				obj5.Pos = new Vector3(12f, 0f, 0f);
				obj5.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				obj5.MainColor = Color4.DarkOrange;
				scene.AddObject(obj5);

				//renderMesh5NeckController = new SSPolarJointController(10);
				renderMesh5NeckJoint = new SSPolarJoint();
				renderMesh5NeckJoint.baseOffset.Position = new Vector3(0f, 0.5f, 0f);
				renderMesh5.AddParametricJoint("UpperNek", renderMesh5NeckJoint);
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
				var bobRender = new SSSkeletalRenderMesh(bobMeshes);
				bobRender.PlayAnimationLoop(bobAnim, 0f);
				bobRender.alphaBlendingEnabled = true;
				bobRender.TimeScale = 0.5f;
				var bobObj = new SSObjectMesh(bobRender);
				bobObj.Name = "Bob";
				bobObj.Pos = new Vector3(10f, 0f, 10f);
				bobObj.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				scene.AddObject(bobObj);
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
            hudScene.renderConfig.projectionMatrix = Matrix4.Identity;

			// HUD Triangle...
			//SSObject triObj = new SSObjectTriangle ();
			//hudScene.addObject (triObj);
			//triObj.Pos = new Vector3 (50, 50, 0);
			//triObj.Scale = new Vector3 (50.0f);

			// HUD text....
			fpsDisplay = new SSObjectGDISurface_Text ();
			fpsDisplay.Label = "FPS: ...";
			fpsDisplay.alphaBlendingEnabled = true;
			hudScene.AddObject (fpsDisplay);
			fpsDisplay.Pos = new Vector3 (10f, 10f, 0f);
			fpsDisplay.Scale = new Vector3 (1.0f);

			// wireframe mode text....
			wireframeDisplay = new SSObjectGDISurface_Text ();
			wireframeDisplay.alphaBlendingEnabled = true;
			hudScene.AddObject (wireframeDisplay);
			wireframeDisplay.Pos = new Vector3 (10f, 40f, 0f);
			wireframeDisplay.Scale = new Vector3 (1.0f);
			updateWireframeDisplayText ();

			// HUD text....
			#if false
			var testDisplay = new SSObject2DSurface_AGGText ();
			testDisplay.backgroundColor = Color.Transparent;
			testDisplay.alphaBlendingEnabled = true;
			testDisplay.Label = "TEST AGG";
			hudScene.AddObject (testDisplay);
			testDisplay.Pos = new Vector3 (50f, 100f, 0f);
			testDisplay.Scale = new Vector3 (1.0f);
			#endif
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
                showWireFrames = (scene.renderConfig.drawWireframeMode == WireframeMode.GLSL_SinglePass);
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
                techinqueInfo = scene.renderConfig.drawWireframeMode;
			}
			wireframeDisplay.Label = String.Format (
				"press '1' to toggle wireframe mode: [{0}:{1}]\n\n" +
				"press 'Q' to \"attack\"",
				selectionInfo, techinqueInfo.ToString()
			);
		}
	}
}