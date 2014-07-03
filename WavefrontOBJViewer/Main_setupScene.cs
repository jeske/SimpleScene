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
			scene.addLight(new SSLight(LightName.Light0));

			// 1. Add Objects
			SSObject triObj;
			scene.addObject (triObj = new SSObjectTriangle () );
						
			// add drone
			SSObject droneObj;			
			scene.addObject (this.activeModel = droneObj = new SSObjectMesh(
				new SSMesh_wfOBJ(SSAssetManager.mgr.getContext("./drone2/"), "drone2.obj", true, shaderPgm)
				));

			// droneObj.MouseDeltaOrient(20,20);
			droneObj.Pos = new OpenTK.Vector3(20,0,0);
			droneObj.MouseDeltaOrient(20.0f,0.0f);

			// add second drone
			
			SSObject drone2Obj;
			scene.addObject (drone2Obj = new SSObjectMesh(
				new SSMesh_wfOBJ(SSAssetManager.mgr.getContext("./drone2/"), "drone2.obj", true, shaderPgm)
				));

			// 2. Add Camera

			scene.addObject (scene.activeCamera = 
					new SSCameraThirdPerson (triObj));
		}

		public void setupHUD() {
			hudScene = new SSScene ();

			// HUD Triangle...
			SSObject triObj = new SSObjectTriangle ();
			hudScene.addObject (triObj);
			triObj.Pos = new Vector3 (50, 50, 0);
			triObj.Scale = new Vector3 (50.0f);

			// HUD text....
			SSObjectGDIText textObj = new SSObjectGDIText ();
			textObj.Label = "test Text";
			hudScene.addObject (textObj);
			textObj.Pos = new Vector3 (100, 100, 0);
			textObj.Scale = new Vector3 (2.0f);
		}
	}
}