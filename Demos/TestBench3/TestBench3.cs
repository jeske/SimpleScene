using System;
using System.Collections.Generic;
using SimpleScene;
using SimpleScene.Demos;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;

namespace TestBench3
{
    public class TestBench3 : TestBenchBootstrap
    {
        protected enum AttackLaunchers : int { AttackerDrone, Vandal, Camera, End }
        protected enum AttackTargets : int { TargetDrone1, Vandal, Camera, Selected, AttackerDrone, End, TargetDrone2 }

        protected SSScene particlesScene;
        protected SSpaceMissilesRenderManager missileManager;
        protected SSpaceMissileVisualParameters attackerDroneMissileParams;
        protected SSpaceMissileVisualParameters vandalShipMissileParams;
        protected SSpaceMissileVisualParameters cameraMissileParams;

        protected SSObjectMesh vandalShip;
        protected SSObjectMesh attackerDrone;
        protected SSObjectMesh targetDrone1;
        protected SSObjectMesh targetDrone2;

        protected AttackLaunchers attackLauncher = AttackLaunchers.AttackerDrone;
        protected AttackTargets attackTargetMode = AttackTargets.TargetDrone1;

        protected float localTime = 0f;

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
            var vandalMesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("missiles", "vandal_assembled.obj");

            // add drones
            attackerDrone = new SSObjectMesh (droneMesh);
            attackerDrone.Pos = new OpenTK.Vector3(-20f, 0f, 0f);
            attackerDrone.Orient(Vector3.UnitX, Vector3.UnitY);
            attackerDrone.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            attackerDrone.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            attackerDrone.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            attackerDrone.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            attackerDrone.Name = "attacker drone";
            scene.AddObject (attackerDrone);

            targetDrone1 = new SSObjectMesh (droneMesh);
            targetDrone1.Pos = new OpenTK.Vector3(200f, 0f, 0f);
            targetDrone1.Orient(-Vector3.UnitX, Vector3.UnitY);
            targetDrone1.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            targetDrone1.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone1.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone1.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone1.Name = "target drone";
            targetDrone1.MainColor = new Color4(1f, 0f, 0.7f, 1f);
            scene.AddObject (targetDrone1);

            vandalShip = new SSObjectMesh (vandalMesh);
            vandalShip.Pos = new OpenTK.Vector3(100f, 0f, 0f);
            vandalShip.Scale = new Vector3 (0.05f);
            vandalShip.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            vandalShip.DiffuseMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            vandalShip.SpecularMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            vandalShip.EmissionMatColor = new Color4(0.0f,0.0f,0.0f,0.0f);
            vandalShip.Name = "Vandal ship";
            vandalShip.MainColor = new Color4 (0.6f, 0.6f, 0.6f, 1f);
            //vandalShip.MainColor = new Color4(1f, 0f, 0.7f, 1f);
            //droneObj2.renderState.visible = false;
            vandalShip.Orient((targetDrone1.Pos-vandalShip.Pos).Normalized(), Vector3.UnitY);
            scene.AddObject (vandalShip);


            // shows explosions
            SExplosionRenderManager explosionRenderer = new SExplosionRenderManager ();
            explosionRenderer.particleSystem.doShockwave = false;
            explosionRenderer.particleSystem.doDebris = false;
            explosionRenderer.particleSystem.timeScale = 3f;
            particlesScene.AddObject(explosionRenderer);

            // missile parameters
            attackerDroneMissileParams = new SSpaceMissileVisualParameters();
            attackerDroneMissileParams.targetHitHandlers += (pos, mParams) => {
                explosionRenderer.showExplosion(pos, 2.5f);
            };
            vandalShipMissileParams = new SSpaceMissileVisualParameters();
            vandalShipMissileParams.targetHitHandlers += (pos, mParams) => {
                explosionRenderer.showExplosion(pos, 2.5f);
            };
            cameraMissileParams = new SSpaceMissileVisualParameters();
            cameraMissileParams.targetHitHandlers += (pos, mParams) => {
                explosionRenderer.showExplosion(pos, 2.5f);
            };

            // missile manager
            missileManager = new SSpaceMissilesRenderManager(scene, particlesScene, hudScene);

            // initialize demo logic
            //targetObject = getTargetObject();
        }

        protected void missileKeyUpHandler(object sender, KeyboardKeyEventArgs e)
        {
            switch(e.Key) {
            case Key.Q:
                _launchMissiles();
                break;
            case Key.M:
                var camera = (scene.ActiveCamera as SSCameraThirdPerson);
                if (camera != null) {
                    var target = camera.FollowTarget;
                    if (target == null) {
                        camera.FollowTarget = attackerDrone;
                    } else if (target == attackerDrone) {
                        camera.FollowTarget = targetDrone1;
                    } else {
                        camera.FollowTarget = null;
                    }
                    updateTextDisplay();
                }
                break;
            case Key.T:
                int a = (int)attackTargetMode;
                a = (a + 1) % (int)AttackTargets.End;
                attackTargetMode = (AttackTargets)a;
                if (getTargetObject() == getLauncherObject()) {
                    a = (a + 1) % (int)AttackTargets.End;
                    attackTargetMode = (AttackTargets)a;
                }
                updateTextDisplay();
                break;
            case Key.L: 
                int l = (int)attackLauncher;
                l = (l + 1) % (int)AttackLaunchers.End;
                attackLauncher = (AttackLaunchers)l;
                if (getTargetObject() == getLauncherObject()) {
                    a = (int)attackTargetMode;
                    a = (a + 1) % (int)AttackTargets.End;
                    attackTargetMode = (AttackTargets)a;
                }
                updateTextDisplay();
                break;
            case Key.V:
                attackerDroneMissileParams.debuggingAid = !attackerDroneMissileParams.debuggingAid;
                vandalShipMissileParams.debuggingAid = !vandalShipMissileParams.debuggingAid;
                cameraMissileParams.debuggingAid = !cameraMissileParams.debuggingAid;
                updateTextDisplay();
                break;
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
            camera.Name = "camera";
            camera.basePos = new Vector3 (100f, 0f, 30f);
            camera.Pos = new Vector3 (170f, 20f, 245f);
            camera.followDistance = 225f;
            camera.localBoundingSphereRadius = 0.1f;

            scene.ActiveCamera = camera;
            scene.AddObject (camera);
        }

        protected override void renderScenes (
            float fovy, float aspect, 
            float nearPlane, float farPlane, 
            ref Matrix4 mainSceneView, ref Matrix4 mainSceneProj, 
            ref Matrix4 rotationOnlyView, ref Matrix4 screenProj)
        {
            base.renderScenes(fovy, aspect, nearPlane, farPlane, 
                ref mainSceneView, ref mainSceneProj, ref rotationOnlyView, ref screenProj);

            // laser middle sections and burn particles
            particlesScene.renderConfig.invCameraViewMatrix = mainSceneView;
            particlesScene.renderConfig.projectionMatrix = mainSceneProj;
            particlesScene.Render ();
        }

        protected override void OnUpdateFrame (FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            float timeElapsed = (float)e.Time;
            particlesScene.Update(timeElapsed);

            // make the target drone move from side to side
            localTime += timeElapsed;
            Vector3 pos = targetDrone1.Pos;
            pos.Z = 30f * (float)Math.Sin(localTime);
            targetDrone1.Pos = pos;

            // make the vandal ship orbit missile target
            Vector3 desiredPos;
            Vector3 desiredDir;
            float angle = localTime * 0.5f;
            float desiredXOffset = 100f * (float)Math.Cos(angle);
            float desiredYOffset = 20f * (float)Math.Sin(angle * 0.77f);
            float desiredZOffset = 80f * (float)Math.Sin(angle * 0.88f);
            Vector3 desiredOffset = new Vector3 (desiredXOffset, desiredYOffset, desiredZOffset);

            var target = getTargetObject();
            if (attackLauncher != AttackLaunchers.Vandal || target == null || target == vandalShip) {
                desiredPos = new Vector3 (100f, 0f, 0f);
                desiredDir = -Vector3.UnitX;
            }
            else if (target == scene.ActiveCamera) {
                desiredPos = scene.ActiveCamera.Pos + -scene.ActiveCamera.Dir * 300f;
                Quaternion cameraOrient = OpenTKHelper.neededRotation(Vector3.UnitZ, -scene.ActiveCamera.Up);
                desiredPos += Vector3.Transform(desiredOffset * 0.1f, cameraOrient); 
                desiredDir = (target.Pos - vandalShip.Pos).Normalized();

            } else {
                //float desiredZOffset = 5f * (float)Math.Sin(angle + 0.2f);
                desiredPos = target.Pos + desiredOffset;
                desiredDir = (target.Pos - vandalShip.Pos).Normalized();
            }

            Vector3 desiredMotion = desiredPos - vandalShip.Pos;
            const float vel = 100f;
            float displacement = vel * timeElapsed;
            if (displacement > desiredMotion.LengthFast) {
                vandalShip.Pos = desiredPos;
            } else {
                vandalShip.Pos = vandalShip.Pos + desiredMotion.Normalized() * displacement;
            }

            Quaternion vandalOrient = OpenTKHelper.neededRotation(Vector3.UnitZ, desiredDir);
            vandalShip.Orient(desiredDir, Vector3.Transform(Vector3.UnitY, vandalOrient));
        }

        // Going deeper into customization...

        protected void _launchMissiles()
        {
            var target = getTargetObject();
            if (target != null) {
                missileManager.launchCluster(
                    getLauncherObject().Pos, 
                    Vector3.Zero,
                    1,
                    new SSpaceMissileObjectTarget (target),
                    10f,
                    getLauncherParams()
                );
            }
        }

        protected SSObject getLauncherObject()
        {
            switch (attackLauncher) {
            case AttackLaunchers.AttackerDrone: return attackerDrone;
            case AttackLaunchers.Vandal: return vandalShip;
            case AttackLaunchers.Camera: return scene.ActiveCamera;
            }
            throw new Exception ("unhandled enum");
        }

        protected SSpaceMissileVisualParameters getLauncherParams()
        {
            switch(attackLauncher) {
            case AttackLaunchers.AttackerDrone: return attackerDroneMissileParams;
            case AttackLaunchers.Vandal: return vandalShipMissileParams;
            case AttackLaunchers.Camera: return cameraMissileParams;
            }
            throw new Exception ("unhandled enum");
        }

        protected SSObject getTargetObject()
        {
            switch (attackTargetMode) {
            case AttackTargets.TargetDrone1: return targetDrone1;
            case AttackTargets.TargetDrone2: return targetDrone1; // TODO
            case AttackTargets.AttackerDrone: return attackerDrone;
            case AttackTargets.Vandal: return vandalShip;
            case AttackTargets.Camera: return scene.ActiveCamera;
            case AttackTargets.Selected: return selectedObject ?? scene.ActiveCamera;
            }
            throw new Exception ("unhandled enum");
        }

        protected override void updateTextDisplay ()
        {
            base.updateTextDisplay ();
            var text =  "\n[Q] to fire missiles";

            // camera mode
            var camera = scene.ActiveCamera as SSCameraThirdPerson;
            if (camera != null) {
                var target = camera.FollowTarget;
                text += "\n[M] toggle camera target: [";
                text += (target == null ? "none" : target.Name) + ']';
            }

            // attacker
            text += "\n[L] toggle missile launcher: ";
            var launcherObj = getLauncherObject();
            text += '[' + (launcherObj == null ? "none" : launcherObj.Name) + ']';

            // target
            text += "\n[T] toggle missile target: ";
            if (attackTargetMode == AttackTargets.Selected) {
                text += "selected: ";
            }
            var targetObj = getTargetObject();
            text += '[' + (targetObj == null ? "none" : targetObj.Name) + ']';

            // debugging
            text += "\n[V] visual debugigng aid: [";
            text += (attackerDroneMissileParams.debuggingAid ? "ON" : "OFF")  + ']';

            textDisplay.Label += text;
        }
    }
}

