// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace WavefrontOBJViewer
{
	partial class Game : OpenTK.GameWindow
	{
		public void setupScene() {
			scene = new SSScene ();

			SSAssetManager.mgr.addAssetArchive(new SSAssetArchiveHandler_FileSystem("./Assets"));


			var lightPos = new Vector3 (5.0f, 40.0f, 10.0f);
			// 0. Add Lights
			var light = new SSLight (LightName.Light0);
			light.Pos = lightPos;
			scene.addLight(light);

			// 1. Add Objects
			SSObject triObj;
			scene.addObject (triObj = new SSObjectTriangle () );
			triObj.Pos = lightPos;
						
			// add drone
			SSObject droneObj = new SSObjectMesh (new SSMesh_wfOBJ (SSAssetManager.mgr.getContext ("./drone2/"), "drone2.obj", true, shaderPgm));
			scene.addObject (this.activeModel = droneObj);
			droneObj.renderState.lighted = true;
			droneObj.ambientMatColor = new Color4(0.2f,0.2f,0.2f,0.2f);
			droneObj.diffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.specularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj.shininessMatColor = 10.0f;
			droneObj.MouseDeltaOrient(-40.0f,0.0f);
			droneObj.Pos = new OpenTK.Vector3(-5,0,0);

			// add second drone
			
			SSObject drone2Obj = new SSObjectMesh(
				new SSMesh_wfOBJ(SSAssetManager.mgr.getContext("./muscle_as1/"), "body.obj", true, shaderPgm)
				);
			scene.addObject (drone2Obj);
			drone2Obj.renderState.lighted = true;
			drone2Obj.ambientMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.diffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			drone2Obj.shininessMatColor = 10.0f;
			drone2Obj.Pos = new OpenTK.Vector3(20,0,0);
			drone2Obj.MouseDeltaOrient(20.0f,0.0f);

			// last. Add Camera

			scene.addObject (scene.activeCamera = 
					new SSCameraThirdPerson (droneObj));
		}

		public void setupEnvironment() {
		    environmentScene = new SSScene();

			// add skybox cube
			SSObject skyboxCube = new SSObjectMeshSky(new SSMesh_wfOBJ(SSAssetManager.mgr.getContext("./skybox/"),"skybox.obj",true));
			environmentScene.addObject(skyboxCube);
			skyboxCube.Scale = new Vector3(0.7f);
			skyboxCube.renderState.lighted = false;

			// scene.addObject(skyboxCube);

			SSObject skyboxStars = new SSObjectMeshSky(new SSMesh_Starfield(800));
			environmentScene.addObject(skyboxStars);
			skyboxStars.renderState.lighted = false;

		}

		public void setupHUD() {
			hudScene = new SSScene ();

			// HUD Triangle...
			//SSObject triObj = new SSObjectTriangle ();
			//hudScene.addObject (triObj);
			//triObj.Pos = new Vector3 (50, 50, 0);
			//triObj.Scale = new Vector3 (50.0f);

			// HUD text....
			SSObjectGDIText textObj = new SSObjectGDIText ();
			textObj.Label = "text test 1 2 3 4";
			hudScene.addObject (textObj);
			textObj.Pos = new Vector3 (100.5f, 100f, 0f);
			textObj.Scale = new Vector3 (2.0f);
		}
	}
}