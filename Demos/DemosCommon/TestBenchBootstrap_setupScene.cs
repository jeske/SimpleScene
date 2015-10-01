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
		protected SSObjectGDISurface_Text textDisplay;

		protected virtual void setupScene() {
			sunDiskScene = new SSScene ();
            sunFlareScene = new SSScene (mainShader, null, instancingShader, null);
			hudScene = new SSScene ();
			environmentScene = new SSScene ();

            scene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);
			scene.renderConfig.frustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
            scene.renderConfig.usePoissonSampling = true;
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

            #if true
			// setup a sun billboard object and a sun flare spriter renderer
			{
				var sunDisk = new SSMeshDisk ();
				var sunBillboard = new SSObjectOcclusionQueuery (sunDisk);
				sunBillboard.renderState.doBillboarding = true;
				sunBillboard.MainColor = new Color4 (1f, 1f, 0.8f, 1f);
				sunBillboard.Pos = new Vector3 (0f, 0f, 18000f);
				sunBillboard.Scale = new Vector3 (600f);
                sunBillboard.renderState.depthFunc = DepthFunction.Lequal;
				sunBillboard.renderState.frustumCulling = false;
				sunBillboard.renderState.lighted = false;
				sunBillboard.renderState.castsShadow = false;
				sunDiskScene.AddObject(sunBillboard);

                var sunFlare = new SSSunFlareRenderer(sunDiskScene, sunBillboard);
                sunFlare.Name = "sun flare renderer";
				sunFlareScene.AddObject (sunFlare);
			}
            #endif
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
            skyboxCube.renderState.depthTest = false;
            skyboxCube.renderState.depthWrite = false;
            skyboxCube.renderState.lighted = false;
			environmentScene.AddObject(skyboxCube);
			skyboxCube.Scale = new Vector3(0.7f);

			// scene.addObject(skyboxCube);

			SSObject skyboxStars = new SStarfieldObject(1600);
			environmentScene.AddObject(skyboxStars);

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
			textDisplay = new SSObjectGDISurface_Text ();
			textDisplay.alphaBlendingEnabled = true;
			hudScene.AddObject (textDisplay);
			textDisplay.Pos = new Vector3 (10f, 40f, 0f);
			textDisplay.Scale = new Vector3 (1.0f);
			updateTextDisplay ();
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