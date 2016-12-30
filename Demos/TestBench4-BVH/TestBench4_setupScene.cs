// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

using SimpleScene;
using SimpleScene.Demos;

namespace TestBench4
{
	partial class TestBench4 : TestBenchBootstrap
	{
		protected override void setupHUD()
		{
			base.setupHUD();

			// HUD text....
			var testDisplay = new SSObject2DSurface_AGGText();
			testDisplay.backgroundColor = Color.Transparent;
			testDisplay.alphaBlendingEnabled = true;
			testDisplay.Label = "TEST AGG";
			hud2dScene.AddObject(testDisplay);
			testDisplay.Pos = new Vector3(50f, 100f, 0f);
			testDisplay.Scale = new Vector3(1.0f);
		}

		protected override void setupScene()
		{
			base.setupScene();

			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./shuttle_pod/pod-multiobj.obj");

			// add test object
			SSObject testObj = new SSObjectMesh(mesh);
			main3dScene.AddObject(testObj);
			testObj.renderState.lighted = true;
#if false
			testObj.colorMaterial = new SSColorMaterial(
                new Color4(0.3f,0.3f,0.3f,0.3f), // diffuse
			    new Color4(0.1f,0.1f,0.1f,0.1f), // ambient
			    new Color4(0.3f,0.3f,0.3f,0.3f), // specular
			    new Color4(0.3f,0.3f,0.3f,0.3f), // emission
                10f); // shininess
#endif
			//droneObj.EulerDegAngleOrient(-40.0f,0.0f);
			testObj.Pos = new OpenTK.Vector3(0f, 0f, -15f);
            testObj.Size = 100.0f;
			testObj.Name = "test object";
			
		}
	}
}