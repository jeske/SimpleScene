// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

using SimpleScene;

namespace SimpleScene.Demos
{
	partial class TestBenchBootstrap : OpenTK.GameWindow
	{
		protected bool autoWireframeMode = true;
		protected SSObjectGDISurface_Text fpsDisplay;
		protected SSObjectGDISurface_Text wireframeDisplay;

		protected virtual void setupScene() {
			scene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);
			sunDiskScene = new SSScene ();
			sunFlareScene = new SSScene ();
			hudScene = new SSScene ();
			environmentScene = new SSScene ();

			scene.renderConfig.frustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
			scene.BeforeRenderObject += beforeRenderObjectHandler;

			// 0. Add Lights
			var light = new SSDirectionalLight (LightName.Light0);
			light.Direction = new Vector3(0f, 0f, -1f);
			#if true
			if (OpenTKHelper.areFramebuffersSupported ()) {
                if (scene.renderConfig.pssmShader != null && scene.renderConfig.instancePssmShader != null) {
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
				var sunFlare = new SimpleSunFlareMesh (sunDiskScene, sunBillboard, flareTex, 
													 flareSpriteRects, spriteScales);
				sunFlare.Scale = new Vector3 (2f);
				sunFlare.renderState.lighted = false;
				sunFlareScene.AddObject(sunFlare);
			}

		}

		protected virtual void setupCamera()
		{
			var camera = new SSCameraThirdPerson (null);
			camera.followDistance = 50.0f;
			scene.ActiveCamera = camera;
			scene.AddObject (camera);
		}

		protected virtual void setupEnvironment() 
		{
			// add skybox cube
			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./skybox/","skybox.obj");
			SSObject skyboxCube = new SSObjectMesh(mesh);
			environmentScene.AddObject(skyboxCube);
			skyboxCube.Scale = new Vector3(0.7f);
			skyboxCube.renderState.lighted = false;

			// scene.addObject(skyboxCube);

			SSObject skyboxStars = new SSObjectMesh(new SimpleStarfieldMesh(1600));
			environmentScene.AddObject(skyboxStars);
			skyboxStars.renderState.lighted = false;

		}

		protected virtual void setupHUD() 
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
		}

		protected virtual void beforeRenderObjectHandler (Object obj, SSRenderConfig renderConfig)
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
	}
}