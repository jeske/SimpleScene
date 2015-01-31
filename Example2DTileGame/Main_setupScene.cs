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
        public static int mapWidth = 1; // for now
        public static int mapHeight = 0;
        public static int mapDepth = 1; // for now

        private static int maxHeight = 256;
        private static int baseHeight = 0;

        SSObject mapObject;

        SSObject player;

        // Positions for the camera
        float CameraX = 50.0f,
              CameraY = 10.0f,
              CameraZ = 10.0f;

        float PlayerX = 0.0f,
              PlayerY = 1.0f, // So we aren't stuck in the ground
              PlayerZ = 0.0f;

        public static SSObject[,] map = new SSObject[mapWidth, mapDepth];
        public SSObject[,] heightMap = new SSObject[baseHeight, maxHeight];
        
        /// <summary>
        /// Setup the scene (to be rendered)
        /// </summary>
		public void setupScene() {             

            scene.BaseShader = shaderPgm;
            shaderPgm.Activate();

            // make and position the camera
            camera = new SSCameraThirdPerson();
            camera.basePos = new Vector3(CameraX, CameraY, CameraZ);   // the ground plane is in (X,Z) so Y+30 is units "above" the ground                           
                   
            scene.AddObject(camera);
            scene.ActiveCamera = camera;

            var lightPos = new Vector3(50.0f, 50.0f, 10.0f);
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
                for (int j = 0; j < mapDepth; j++)
                {

                    var mapMesh = SSAssetManager.GetInstance<SSMesh_wfOBJ>("./mapTileModels", "map.obj");
                    map[i, j] = new SSObjectMesh(mapMesh);

                    map[i, j].Pos = new Vector3(i * 20, 0, j * 20);

                    map[i, j].ambientMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                    map[i, j].diffuseMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                    map[i, j].specularMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

                    map[i, j].renderState.lighted = true;


                    if (i == 5 && j == 5)
                    {
                        map[i, j].Pos.Y.Equals(5.0f);
                    }                   

                    scene.AddObject(map[i, j]);
                    
                }
            }


        }

        public void setupPlayer()
        {
            player = new SSObjectMesh(SSAssetManager.GetInstance<SSMesh_wfOBJ>
                ("./mapTileModels", "player.obj"));

            player.Pos = new Vector3(PlayerX, PlayerY, PlayerZ); // Default location
            player.ambientMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            player.diffuseMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            player.specularMatColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

            player.renderState.lighted = true;

            scene.AddObject(player);
            
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
            // Add the object to screen
			hudScene.AddObject (fpsDisplay);
            // Location of FPS HUD
			fpsDisplay.Pos = new Vector3 (10f, 10f, 0f);
            // Scale of text... (size)
			fpsDisplay.Scale = new Vector3 (1.0f);
			
		}

        /// <summary>
        /// Get player
        /// </summary>
        /// <returns></returns>
        public SSObject getPlayer()
        {
            return player;
        }

	}
}