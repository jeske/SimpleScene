// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK.Input;
using SimpleScene;
using System.Collections;

namespace Example2DTileGame
{
	partial class Example2DTileGame : OpenTK.GameWindow
	{

		SSObjectGDISurface_Text fpsDisplay;
		SSObjectGDISurface_Text wireframeDisplay;
        public static int mapWidth = 20;
        public static int mapHeight = 20;
        public static int mapDepth = 35;
        SSObject mapObject;
        SSObject player;

        public static SSObject[,] map = new SSObject[mapWidth, mapHeight];
        
        /// <summary>
        /// Setup the scene (to be rendered)
        /// </summary>
		public void setupScene() {

            scene.BaseShader = shaderPgm;
            shaderPgm.Activate();

            // make and position the camera
            camera = new SSCameraThirdPerson();
            camera.basePos = new Vector3(0,30,0);   // the ground plane is in (X,Z) so Y+30 is units "above" the ground                           
                   
            scene.AddObject(camera);
            scene.ActiveCamera = camera;

            var lightPos = new Vector3(50.0f, 70.0f, 10.0f);
            var light = new SSLight();

            light.Pos = lightPos;
            light.Direction = new Vector3(50.0f, 10.0f, 0.0f);
            light.Ambient = new Vector4(0.2f, 0.2f, 0.2f, 0.2f);
            light.Specular = new Vector4(0.2f, 0.2f, 0.2f, 0.2f);

            var triangle = new SSObjectTriangle();
            triangle.Pos = light.Pos;

            scene.AddLight(light);
            scene.AddObject(triangle);

            setupMap();
            //setupPlayer();
		}

        /// <summary>
        /// Load map
        /// </summary>
        public void setupMap()
        {


           for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapWidth; j++)
                {

                       var meshname = ((i % 2) == 1) ? "tile_brick.obj" : "tile_grass.obj";
                       var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./mapTileModels", meshname);

                       SSObject plane = new SSObjectMesh(mesh);

                        scene.AddObject(plane);
                        map[i,j] = plane;
                        map[i,j].Pos = new Vector3(i * 2, 0.0f, j * 2);

                        // Set matrix colors I think?
                        map[i,j].ambientMatColor = new Color4(0.3f, 0.3f, 0.3f, 0.3f);
                        map[i,j].diffuseMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                        map[i,j].specularMatColor = new Color4(0.3f, 0.3f, 0.3f, 0.3f);
                        map[i,j].renderState.lighted = true;
                    
                    
                }
            } 
        }

        /*public void setupPlayer()
        {
            SSObject player =
                new SSObjectMesh(
                    SSAssetManager.GetInstance<SSMesh_wfOBJ>("./", "untitled.obj"));
            player.Pos = new Vector3(1.0f, 1.0f, 1.0f); // Default location
            player.ambientMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            player.diffuseMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            player.specularMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

            player.renderState.lighted = true;

            scene.AddObject(player);
            
        }*/

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
            // Add the object to screen
			hudScene.AddObject (fpsDisplay);
            // Location of FPS HUD
			fpsDisplay.Pos = new Vector3 (10f, 10f, 0f);
            // Scale of text... (size)
			fpsDisplay.Scale = new Vector3 (1.0f);
			
		}
	}
}