// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
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
	
			scene.addObject (new SSObjectTriangle () );
						
			// add drone
			SSObject droneObj;			
			scene.addObject (this.activeModel = droneObj = new SSObjectMesh(
				new SSMesh_wfOBJ(SSAssetManager.mgr.getContext("./drone2/"), "drone2.obj", true, shaderPgm)
				));

			// droneObj.MouseDeltaOrient(20,20);
			droneObj.Pos = new OpenTK.Vector3(10,0,0);

			// add second drone
			
			SSObject drone2Obj;
			scene.addObject (drone2Obj = new SSObjectMesh(
				new SSMesh_wfOBJ(SSAssetManager.mgr.getContext("./drone2/"), "drone2.obj", true, shaderPgm)
				));

			// 2. Add Camera

			scene.addObject (scene.activeCamera = new SSCameraThirdPerson (droneObj));
			}
	}
}