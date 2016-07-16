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
        protected SSObjectOcclusionQueuery sunDisk;

		protected virtual void setupScene() {
            hud2dScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader, otherShaders);
            environmentScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader, otherShaders);
            sunFlareScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader, otherShaders);
            sunDiskScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader, otherShaders);

            main3dScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader, otherShaders);
			main3dScene.renderConfig.frustumCulling = true;
            main3dScene.renderConfig.usePoissonSampling = true;
			main3dScene.BeforeRenderObject += this.beforeRenderObjectHandler;

            alpha3dScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader, otherShaders);
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
                hud2dScene.AddObject(shadowmapDebugQuad);
            }
			#endif
		}

		protected virtual void setupCamera()
		{
			var camera = new SSCameraThirdPerson (null);
			camera.followDistance = 50.0f;
            camera.Name = "camera";
			main3dScene.ActiveCamera = camera;
		}

		protected virtual void setupEnvironment() 
		{
			// add skybox cube
            skyboxCube = new SSkyboxRenderer () {
                Scale = new Vector3(10f)
            };

            environmentScene.AddObject(skyboxCube);

            // add stars
			skyboxStars = new SStarfieldObject(1600);
            skyboxStars.selectable = false;
			environmentScene.AddObject(skyboxStars);

            #if true
            // setup a sun billboard object and a sun flare spriter renderer
            {
                var sunDiskMesh = new SSMeshDisk ();
                sunDisk = new SSObjectOcclusionQueuery (sunDiskMesh);
                sunDisk.renderState.doBillboarding = true;
                sunDisk.MainColor = new Color4 (1f, 1f, 0.8f, 1f);
                sunDisk.Scale = new Vector3 (25f);
                sunDisk.renderState.matchScaleToScreenPixels = true;
                sunDisk.renderState.depthFunc = DepthFunction.Lequal;
                sunDisk.renderState.frustumCulling = false;
                sunDisk.renderState.lighted = false;
                sunDisk.renderState.castsShadow = false;
                sunDiskScene.AddObject(sunDisk);

                var sunFlare = new SSSunFlareRenderer(sunDiskScene, sunDisk);
                sunFlare.renderState.depthTest = false;
                sunFlare.renderState.depthWrite = false;
                sunFlare.Name = "sun flare renderer";
                sunFlareScene.AddObject (sunFlare);
            }
            #endif
		}

		protected virtual void setupHUD() 
		{
            hud2dScene.renderConfig.projectionMatrix = Matrix4.Identity;

			// HUD Triangle...
			//SSObject triObj = new SSObjectTriangle ();
			//hudScene.addObject (triObj);
			//triObj.Pos = new Vector3 (50, 50, 0);
			//triObj.Scale = new Vector3 (50.0f);

			// HUD text....
			fpsDisplay = new SSObjectGDISurface_Text ();
			fpsDisplay.Label = "FPS: ...";
			fpsDisplay.alphaBlendingEnabled = true;
			hud2dScene.AddObject (fpsDisplay);
			fpsDisplay.Pos = new Vector3 (10f, 10f, 0f);
			fpsDisplay.Scale = new Vector3 (1.0f);

			// wireframe mode text....
			textDisplay = new SSObjectGDISurface_Text ();
			textDisplay.alphaBlendingEnabled = true;
			hud2dScene.AddObject (textDisplay);
			textDisplay.Pos = new Vector3 (10f, 40f, 0f);
			textDisplay.Scale = new Vector3 (1.0f);
			updateTextDisplay ();
		}

		protected virtual void beforeRenderObjectHandler (Object obj, SSRenderConfig renderConfig)
		{
			if (autoWireframeMode) {
				if (obj == selectedObject) {
					renderConfig.drawWireframeMode = WireframeMode.GLSL_SinglePass;
				} else {
					renderConfig.drawWireframeMode = WireframeMode.None;
				}
			}
		}
	}
}