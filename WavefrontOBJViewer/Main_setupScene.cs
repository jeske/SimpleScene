// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	partial class Game : OpenTK.GameWindow
	{
		public void setupScene() {
			scene = new SSScene ();

			SSAssetManager.mgr.addAssetArchive(new SSAssetArchiveHandler_FileSystem("./Assets"));

			// 0. Add Lights
			var light = new SSLight (LightName.Light0);
			light.Pos = new Vector3 (0.0f, 0.0f, 10.0f);
			scene.addLight(light);

			// 1. Add Objects
			//SSObject triObj;
			//scene.addObject (triObj = new SSObjectTriangle () );
						
			// add drone
			SSObject droneObj = new SSObjectMesh (new SSMesh_wfOBJ (SSAssetManager.mgr.getContext ("./drone2/"), "drone2.obj", true, shaderPgm));
			droneObj.renderState.lighted = true;
			scene.addObject (this.activeModel = droneObj);


			// add second drone
			
			SSObject drone2Obj = new SSObjectMesh(
				new SSMesh_wfOBJ(SSAssetManager.mgr.getContext("./drone2/"), "drone2.obj", true, shaderPgm)
				);
			scene.addObject (drone2Obj);
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