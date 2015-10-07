using System;
using SimpleScene;
using SimpleScene.Demos;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;

namespace TestBench3
{
    public class TestBench3 : TestBenchBootstrap
    {
        protected SSScene missileParticlesScene;
        protected SSpaceMissilesVisualSimulationManager missileManager;

        protected SSObjectMesh droneObj1;
        protected SSObjectMesh droneObj2;


        public TestBench3 ()
            : base("TestBench3: Missiles")
        {
            shadowmapDebugQuad.renderState.visible = false;
        }

        static void Main()
        {
            // The 'using' idiom guarantees proper resource cleanup.
            // We request 30 UpdateFrame events per second, and unlimited
            // RenderFrame events (as fast as the computer can handle).
            using (var game = new TestBench3()) {
                game.Run(30.0);
            }
        }

        protected override void setupScene ()
        {
            base.setupScene();

            missileParticlesScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);

            //var droneMesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");
            var droneMesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("missiles", "missile.obj");

            // add drones
            droneObj1 = new SSObjectMesh (droneMesh);
            droneObj1.Pos = new OpenTK.Vector3(-20f, 0f, -15f);
            droneObj1.Orient(Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI/2f));
            droneObj1.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            droneObj1.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            droneObj1.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            droneObj1.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            //droneObj1.renderState.visible = false;
            droneObj1.Name = "attacker drone";
            scene.AddObject (droneObj1);

            droneObj2 = new SSObjectMesh (droneMesh);
            droneObj2.Pos = new OpenTK.Vector3(20f, 0f, -15f);
            droneObj2.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            droneObj2.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            droneObj2.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            droneObj2.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            droneObj2.Name = "target drone";
            droneObj2.MainColor = new Color4(1f, 0f, 0.7f, 1f);
            //droneObj2.renderState.visible = false;
            scene.AddObject (droneObj2);

            // manages missiles
            missileManager = new SSpaceMissilesVisualSimulationManager(scene, missileParticlesScene);
        }

        protected void missileKeyUpHandler(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Q) {
                _launchMissiles();
            } else if (e.Key == Key.M) {
                var camera = (scene.ActiveCamera as SSCameraThirdPerson);
                if (camera != null) {
                    var target = camera.FollowTarget;
                    if (target == null) {
                        camera.FollowTarget = droneObj1;
                    } else if (target == droneObj1) {
                        camera.FollowTarget = droneObj2;
                    } else {
                        camera.FollowTarget = null;
                    }
                    updateTextDisplay();
                }
            }
        }

        protected void _launchMissiles()
        {
            // TODO
        }

        protected override void updateTextDisplay ()
        {
            base.updateTextDisplay ();
            textDisplay.Label += "\n\nPress Q to fire missiles";

            var camera = scene.ActiveCamera as SSCameraThirdPerson;
            if (camera != null) {
                var target = camera.FollowTarget;
                textDisplay.Label += 
                    "\n\nPress M to toggle camera target: ["
                    + (target == null ? "none" : target.Name) + ']';
            }

        }

        protected override void setupInput()
        {
            base.setupInput();
            this.KeyUp += missileKeyUpHandler;
        }

        protected override void setupCamera()
        {
            var camera = new SSCameraThirdPerson (droneObj2);
            //var camera = new SSCameraThirdPerson (droneObj1);
            camera.Pos = Vector3.Zero;
            camera.followDistance = 80.0f;

            scene.ActiveCamera = camera;
            scene.AddObject (camera);
        } 
    }
}

