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
        protected SSScene particlesScene;
        protected SSpaceMissilesRenderManager missileManager;
        protected SSpaceMissileVisualParameters missileParams;

        protected SSObjectMesh attackerDrone;
        protected SSObjectMesh targetDrone;


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

            particlesScene = new SSScene (mainShader, pssmShader, instancingShader, instancingPssmShader);

            var droneMesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");
            //var droneMesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("missiles", "missile.obj");

            // add drones
            attackerDrone = new SSObjectMesh (droneMesh);
            attackerDrone.Pos = new OpenTK.Vector3(-20f, 0f, -15f);
            attackerDrone.Orient(Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI/2f));
            //attackerDrone.Orient(Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI));
            attackerDrone.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            attackerDrone.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            attackerDrone.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            attackerDrone.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            //droneObj1.renderState.visible = false;
            attackerDrone.Name = "attacker drone";
            scene.AddObject (attackerDrone);

            targetDrone = new SSObjectMesh (droneMesh);
            targetDrone.Pos = new OpenTK.Vector3(20f, 0f, -15f);
            targetDrone.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            targetDrone.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone.Name = "target drone";
            targetDrone.MainColor = new Color4(1f, 0f, 0.7f, 1f);
            //droneObj2.renderState.visible = false;
            scene.AddObject (targetDrone);

            // shows explosions
            SExplosionRenderManager explosionRenderer = new SExplosionRenderManager ();
            explosionRenderer.particleSystem.doShockwave = false;
            explosionRenderer.particleSystem.doDebris = false;
            explosionRenderer.particleSystem.timeScale = 3f;
            particlesScene.AddObject(explosionRenderer);

            // missile parameters
            missileParams = new SSpaceMissileVisualParameters();
            missileParams.targetHitHandlers += (pos, mParams) => {
                explosionRenderer.showExplosion(pos, 2.5f);
            };

            // missile manager
            missileManager = new SSpaceMissilesRenderManager(scene, particlesScene, hudScene);

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
                        camera.FollowTarget = attackerDrone;
                    } else if (target == attackerDrone) {
                        camera.FollowTarget = targetDrone;
                    } else {
                        camera.FollowTarget = null;
                    }
                    updateTextDisplay();
                }
            }
        }

        protected void _launchMissiles()
        {
            missileManager.launchCluster(
                attackerDrone.Pos, 
                Vector3.Zero,
                1,
                new SSpaceMissileObjectTarget(targetDrone),
                10f,
                missileParams
            );
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
            var camera = new SSCameraThirdPerson (null);
            //var camera = new SSCameraThirdPerson (droneObj1);
            camera.Pos = Vector3.Zero;
            camera.followDistance = 80.0f;

            scene.ActiveCamera = camera;
            scene.AddObject (camera);
        }

        protected override void renderScenes (
            float fovy, float aspect, 
            float nearPlane, float farPlane, 
            ref Matrix4 mainSceneView, ref Matrix4 mainSceneProj, 
            ref Matrix4 rotationOnlyView, ref Matrix4 screenProj)
        {
            base.renderScenes(fovy, aspect, nearPlane, farPlane, ref mainSceneView, ref mainSceneProj, ref rotationOnlyView, ref screenProj);

            // laser middle sections and burn particles
            particlesScene.renderConfig.invCameraViewMatrix = mainSceneView;
            particlesScene.renderConfig.projectionMatrix = mainSceneProj;
            particlesScene.Render ();
        }

        protected override void OnUpdateFrame (FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            particlesScene.Update((float)e.Time);
        }
    }
}

