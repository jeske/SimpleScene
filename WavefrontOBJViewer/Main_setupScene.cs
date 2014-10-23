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
            scene.renderConfig.BaseShader = shaderPgm;
			scene.renderConfig.frustumCulling = true;  // TODO: fix the frustum math, since it seems to be broken.
			scene.BeforeRenderObject += (obj, renderConfig) => {
				GL.UseProgram(this.shaderPgm.ProgramID);
				if (obj == selectedObject) {
					renderConfig.drawWireframeMode = WireframeMode.GLSL_SinglePass;
					GL.Uniform1 (this.shaderPgm.u_showWireframes, (int) 1);			

				} else {
					renderConfig.drawWireframeMode = WireframeMode.None;
					GL.Uniform1 (this.shaderPgm.u_showWireframes, (int) 0);			

				}
			};


			var lightPos = new Vector3 (5.0f, 40.0f, 10.0f);
			// 0. Add Lights
			var light = new SSLight (LightName.Light0);
			light.Pos = lightPos;
			scene.addLight(light);

			// 1. Add Objects
			SSObject triObj;
			scene.addObject (triObj = new SSObjectTriangle () );
			triObj.Pos = lightPos;

			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "drone2.obj");
						
			// add drone
			SSObject droneObj = new SSObjectMesh (mesh);
			scene.addObject (this.activeModel = droneObj);
			droneObj.renderState.lighted = true;
			droneObj.ambientMatColor = new Color4(0.2f,0.2f,0.2f,0.2f);
			droneObj.diffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.specularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.emissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.shininessMatColor = 10.0f;
			droneObj.EulerDegAngleOrient(-40.0f,0.0f);
			droneObj.Pos = new OpenTK.Vector3(-5,0,0);
			droneObj.Name = "drone 1";

			// add second drone
			
			SSObject drone2Obj = new SSObjectMesh(
				SSAssetManager.GetInstance<SSMesh_wfOBJ>("./drone2/", "drone2.obj")
			);
			scene.addObject (drone2Obj);
			drone2Obj.renderState.lighted = true;
			drone2Obj.ambientMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.diffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.specularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.emissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.shininessMatColor = 10.0f;
			drone2Obj.Pos = new OpenTK.Vector3(20,0,0);
			drone2Obj.EulerDegAngleOrient(20.0f,0.0f);
			drone2Obj.Name = "drone 2";

			// last. Add Camera

			scene.addObject (scene.activeCamera = 
					new SSCameraThirdPerson (droneObj));
		}

		public void setupEnvironment() {

			// add skybox cube
			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./skybox/","skybox.obj");
			SSObject skyboxCube = new SSObjectMeshSky(mesh);
			environmentScene.addObject(skyboxCube);
			skyboxCube.Scale = new Vector3(0.7f);
			skyboxCube.renderState.lighted = false;

			// scene.addObject(skyboxCube);

			SSObject skyboxStars = new SSObjectMeshSky(new SSMesh_Starfield(1600));
			environmentScene.addObject(skyboxStars);
			skyboxStars.renderState.lighted = false;

		}


		SSObjectGDISurface_Text fpsDisplay;

		SSObjectGDISurface_Text wireframeDisplay;

		public void updateWireframeDisplayText(SSRenderConfig config) {
			wireframeDisplay.Label = String.Format ("press 'w' to toggle wireframe mode: [{0}]", config.drawWireframeMode);
		}

		public void setupHUD() {
			hudScene.setProjectionMatrix(Matrix4.Identity);	

			// HUD Triangle...
			//SSObject triObj = new SSObjectTriangle ();
			//hudScene.addObject (triObj);
			//triObj.Pos = new Vector3 (50, 50, 0);
			//triObj.Scale = new Vector3 (50.0f);

			// HUD text....
			fpsDisplay = new SSObjectGDISurface_Text ();
			fpsDisplay.Label = "FPS: ...";
			hudScene.addObject (fpsDisplay);
			fpsDisplay.Pos = new Vector3 (10f, 10f, 0f);
			fpsDisplay.Scale = new Vector3 (1.0f);

			// wireframe mode text....
			wireframeDisplay = new SSObjectGDISurface_Text ();
			hudScene.addObject (wireframeDisplay);
			wireframeDisplay.Pos = new Vector3 (10f, 40f, 0f);
			wireframeDisplay.Scale = new Vector3 (1.0f);
			updateWireframeDisplayText (scene.renderConfig);

			// HUD text....
			var testDisplay = new SSObject2DSurface_AGGText ();
			testDisplay.Label = "TEST AGG";
			hudScene.addObject (testDisplay);
			testDisplay.Pos = new Vector3 (50f, 100f, 0f);
			testDisplay.Scale = new Vector3 (1.0f);

		}
	}
}