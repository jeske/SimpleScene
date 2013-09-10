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

			// scene.addObject (new SSObjectCube ());
			
			SSObject droneObj;
			scene.addObject (new SSObjectTriangle () );
			SSAssetManagerContext ctx = SSAssetManager.mgr.getContext("./drone2/");
			scene.addObject (this.activeModel = droneObj = new SSObjectMesh(new SSMesh_wfOBJ(ctx, "drone2.obj", true, shaderPgm)));
			
			// 2. Add Camera

			scene.addObject (scene.activeCamera = new SSCameraThirdPerson (droneObj));
		}
	}
}