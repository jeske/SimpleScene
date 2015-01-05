// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

using SimpleScene;

namespace WavefrontOBJViewer
{
	partial class WavefrontOBJViewer : OpenTK.GameWindow
	{
		public void setupScene() {
			scene.MainShader = mainShader;
			scene.PssmShader = pssmShader;
			scene.FrustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
			scene.BeforeRenderObject += (obj, renderConfig) => {
				mainShader.Activate();
				if (obj == selectedObject) {
					renderConfig.drawWireframeMode = WireframeMode.GLSL_SinglePass;
					mainShader.UniShowWireframes = true;			

				} else {
					renderConfig.drawWireframeMode = WireframeMode.None;
					mainShader.UniShowWireframes = false;

				}
			};


			var lightPos = new Vector3 (5.0f, 40.0f, 10.0f);
			// 0. Add Lights
			var light = new SSLight ();
			light.Pos = lightPos;
			scene.AddLight(light);

			// 1. Add Objects
			SSObject triObj;
			scene.AddObject (triObj = new SSObjectTriangle () );
			triObj.Pos = lightPos;

			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");
						
			// add drone
			SSObject droneObj = new SSObjectMesh (mesh);
			scene.AddObject (this.activeModel = droneObj);
			droneObj.renderState.lighted = true;
			droneObj.ambientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
			droneObj.diffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.specularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.emissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.shininessMatColor = 10.0f;
			droneObj.EulerDegAngleOrient(-40.0f,0.0f);
			droneObj.Pos = new OpenTK.Vector3(-5,0,0);
			droneObj.Name = "drone 1";

			// add second drone
			
			SSObject drone2Obj = new SSObjectMesh(
				SSAssetManager.GetInstance<SSMesh_wfOBJ>("./drone2/", "Drone2.obj")
			);
			scene.AddObject (drone2Obj);
			drone2Obj.renderState.lighted = true;
			drone2Obj.ambientMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.diffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.specularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.emissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.shininessMatColor = 10.0f;
			drone2Obj.Pos = new OpenTK.Vector3(20,0,0);
			drone2Obj.EulerDegAngleOrient(20.0f,0.0f);
			drone2Obj.Name = "drone 2";

			// last for the main scene. Add Camera

			scene.AddObject (scene.ActiveCamera = 
					new SSCameraThirdPerson (droneObj));

			// setup a sun billboard object and a sun flare spriter renderer
			{
				var sunDisk = new SSMeshDisk ();
				var sunBillboard = new SSObjectBillboard (sunDisk, true);
				sunBillboard.Color = new Vector4 (1f, 1f, 0.8f, 1f);
				sunBillboard.Pos = new Vector3 (0f, 0f, 18000f);
				sunBillboard.Scale = new Vector3 (600f);
				sunBillboard.renderState.frustumCulling = false;
				sunBillboard.renderState.lighted = false;
				sunBillboard.renderState.castsShadow = false;
				sunDiskScene.AddObject(sunBillboard);

				SSTexture flareTex = SSAssetManager.GetInstance<SSTextureWithAlpha>("./Planets/", "sun_flare.png");
				const float flareTexBigOffset = 0.8889f;
				const float flareTexSmallOffset = 0.125f;
				Vector2[] flareTexCoords = {
					new Vector2(0f, 0f),
					new Vector2(1f, 0f),
					new Vector2(1f, flareTexBigOffset),
					new Vector2(0f, flareTexBigOffset),

					new Vector2(0f, flareTexBigOffset),
					new Vector2(flareTexSmallOffset, flareTexBigOffset),
					new Vector2(flareTexSmallOffset, 1f),
					new Vector2(0f, 1f),

					new Vector2(flareTexSmallOffset, flareTexBigOffset),
					new Vector2(flareTexSmallOffset*2f, flareTexBigOffset),
					new Vector2(flareTexSmallOffset*2f, 1f),
					new Vector2(flareTexSmallOffset, 1f),

					new Vector2(flareTexSmallOffset*2f, flareTexBigOffset),
					new Vector2(flareTexSmallOffset*3f, flareTexBigOffset),
					new Vector2(flareTexSmallOffset*3f, 1f),
					new Vector2(flareTexSmallOffset*2f, 1f),

					new Vector2(flareTexSmallOffset*3f, flareTexBigOffset),
					new Vector2(flareTexSmallOffset*4f, flareTexBigOffset),
					new Vector2(flareTexSmallOffset*4f, 1f),
					new Vector2(flareTexSmallOffset*3f, 1f),
				};
				float[] spriteScales = { 20f, 1f, 2f, 1f, 1f };
				var sunFlare = new SSObjectSunFlare (sunDiskScene, sunBillboard, flareTex, flareTexCoords, spriteScales);
				sunFlare.Scale = new Vector3 (2f);
				sunFlare.renderState.lighted = false;
				sunFlareScene.AddObject(sunFlare);
			}
		}

		public void setupEnvironment() {

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

		public void updateWireframeDisplayText(WireframeMode mode) {
			wireframeDisplay.Label = String.Format ("press 'w' to toggle wireframe mode: [{0}]", 
				mode);
		}

		public void setupHUD() {
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
			updateWireframeDisplayText (scene.DrawWireFrameMode);

			// HUD text....
			var testDisplay = new SSObject2DSurface_AGGText ();
			testDisplay.Label = "TEST AGG";
			hudScene.AddObject (testDisplay);
			testDisplay.Pos = new Vector3 (50f, 100f, 0f);
			testDisplay.Scale = new Vector3 (1.0f);

		}
	}
}