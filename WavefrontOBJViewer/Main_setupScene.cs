// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

namespace WavefrontOBJViewer
{
	partial class Game : OpenTK.GameWindow
	{
		public void setupScene() {
			SSAssetManager.mgr.addAssetArchive(new SSAssetArchiveHandler_FileSystem("./"));

			// scene.addObject (new SSObjectCube ());
			
			SSObject droneObj;
			scene.addObject (new SSObjectTriangle () );
			SSAssetManagerContext ctx = SSAssetManager.mgr.getContext("./drone2/");
			scene.addObject (droneObj = new SSObjectMesh(new SSMesh_wfOBJ(ctx, "drone2.obj", true)));
			
			
			// add camera
			scene.addObject (scene.activeCamera = new SSCameraThirdPerson (droneObj));
		}
	}
}