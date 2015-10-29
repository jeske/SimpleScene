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
        protected SSObjectHUDQuad shadowmapDebugQuad;

        protected SSObject skyboxCube;
        protected SSObject skyboxStars;
        protected SSObjectOcclusionQueuery sunBillboard;

		protected virtual void setupScene() {
            hudScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);
            environmentScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);
            sunFlareScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);
            sunDiskScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);

            main3dScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);
			main3dScene.renderConfig.frustumCulling = true;
            main3dScene.renderConfig.usePoissonSampling = true;
			main3dScene.BeforeRenderObject += this.beforeRenderObjectHandler;

            alpha3dScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);
            alpha3dScene.renderConfig.frustumCulling = true;
            alpha3dScene.renderConfig.usePoissonSampling = false;
            alpha3dScene.BeforeRenderObject += this.beforeRenderObjectHandler;

			// 0. Add Lights
			var light = new SSDirectionalLight (LightName.Light0);
			light.Direction = new Vector3(0f, 0f, -1f);
			#if true
			if (OpenTKHelper.areFramebuffersSupported ()) {
                if (main3dScene.renderConfig.pssmShader != null && main3dScene.renderConfig.instancePssmShader != null) {
                //if (false) {
					light.ShadowMap = new SSParallelSplitShadowMap (TextureUnit.Texture7);
				} else {
					light.ShadowMap = new SSSimpleShadowMap (TextureUnit.Texture7);
				}
			}
			if (!light.ShadowMap.IsValid) {
				light.ShadowMap = null;
			}
			#endif
			main3dScene.AddLight(light);

			#if true
            if (light.ShadowMap != null) {
                shadowmapDebugQuad = new SSObjectHUDQuad (light.ShadowMap.TextureID);
                shadowmapDebugQuad.Scale = new Vector3(0.3f);
                shadowmapDebugQuad.Pos = new Vector3(50f, 200, 0f);
                hudScene.AddObject(shadowmapDebugQuad);
            }
			#endif

            #if true
			// setup a sun billboard object and a sun flare spriter renderer
			{
				var sunDisk = new SSMeshDisk ();
				sunBillboard = new SSObjectOcclusionQueuery (sunDisk);
				sunBillboard.renderState.doBillboarding = true;
				sunBillboard.MainColor = new Color4 (1f, 1f, 0.8f, 1f);
				sunBillboard.Scale = new Vector3 (25f);
                sunBillboard.renderState.matchScaleToScreenPixels = true;
                sunBillboard.renderState.depthFunc = DepthFunction.Lequal;
				sunBillboard.renderState.frustumCulling = false;
				sunBillboard.renderState.lighted = false;
				sunBillboard.renderState.castsShadow = false;
                sunDiskScene.AddObject(sunBillboard);

                var sunFlare = new SSSunFlareRenderer(sunDiskScene, sunBillboard);
                sunFlare.renderState.depthTest = false;
                sunFlare.renderState.depthWrite = false;
                sunFlare.Name = "sun flare renderer";
				sunFlareScene.AddObject (sunFlare);
			}
            #endif
		}

		protected virtual void setupCamera()
		{
			var camera = new SSCameraThirdPerson (null);
			camera.followDistance = 50.0f;
            camera.Name = "camera";
			main3dScene.ActiveCamera = camera;
			main3dScene.AddObject (camera);
		}

		protected virtual void setupEnvironment() 
		{
			// add skybox cube
			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./skybox/","skybox.obj");
			skyboxCube = new SSObjectMesh(mesh);
            skyboxCube.renderState.depthTest = true;
            skyboxCube.renderState.depthWrite = true;
            skyboxCube.renderState.lighted = false;
			//skyboxCube.Scale = new Vector3(0.7f);
            //skyboxCube.Scale = new Vector3(farPlane);
            //skyboxCube.renderState.matchScaleToScreenPixels = true;
            environmentScene.AddObject(skyboxCube);


			skyboxStars = new SStarfieldObject(1600);
			//environmentScene.AddObject(skyboxStars);

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
                showWireFrames = (main3dScene.renderConfig.drawWireframeMode == WireframeMode.GLSL_SinglePass);
			}
            mainShader.Activate();
            mainShader.UniShowWireframes = showWireFrames;
            instancingShader.Activate();
            instancingShader.UniShowWireframes = showWireFrames;

		}
	}
}