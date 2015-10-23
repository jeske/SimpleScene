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
        protected enum MissileLaunchers : int 
            {AttackerDrone, VandalShip, Camera, End }
        protected enum MissileTargets : int 
            { TargetDrone1, VandalShip, Camera, Selected, AttackerDrone, End}
        protected enum HitTimeMode : int 
            { Disabled, Auto, Fixed5s, Fixed10s, Fixed15s, Fixed20s, End }

        protected SSScene particlesScene;
        protected SExplosionRenderManager explosionManager;
        protected SSpaceMissilesRenderManager missileManager;

        protected SSObjectMesh vandalShip;
        protected SSObjectMesh attackerDrone;
        protected SSObjectMesh targetDrone;

        protected SSpaceMissileParameters attackerDroneMissileParams;
        protected SSpaceMissileParameters vandalShipMissileParams;
        protected SSpaceMissileParameters cameraMissileParams;

        protected MissileLaunchers missileLauncher = MissileLaunchers.AttackerDrone;
        protected MissileTargets missileTarget = MissileTargets.TargetDrone1;
        protected HitTimeMode hitTimeMode = HitTimeMode.Auto;

        protected Dictionary<SSObject, ISSpaceMissileTarget> targets 
            = new Dictionary<SSObject, ISSpaceMissileTarget> ();
        protected int clusterSize = 1;

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

            targetDrone = new SSObjectMesh (droneMesh);
            targetDrone.Pos = new OpenTK.Vector3(200f, 0f, 0f);
            targetDrone.Orient(-Vector3.UnitX, Vector3.UnitY);
            targetDrone.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
            targetDrone.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
            targetDrone.Name = "target drone";
            targetDrone.MainColor = new Color4(1f, 0f, 0.7f, 1f);
            scene.AddObject (targetDrone);

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
            vandalShip.Orient((targetDrone.Pos-vandalShip.Pos).Normalized(), Vector3.UnitY);
            scene.AddObject (vandalShip);

            // shows explosions
            explosionManager = new SExplosionRenderManager ();
            explosionManager.particleSystem.doShockwave = false;
            explosionManager.particleSystem.doDebris = false;
            explosionManager.particleSystem.timeScale = 3f;
            //explosionManager.renderState.visible = false;
            particlesScene.AddObject(explosionManager);

            // attacker drone missile parameters
            attackerDroneMissileParams = new SSpaceMissileParameters();
            attackerDroneMissileParams.targetHitHandlers += targetHitHandler;

            // vandal missile params
            vandalShipMissileParams = new SSpaceMissileParameters();
            vandalShipMissileParams.spawnGenerator = null;
            vandalShipMissileParams.spawnTxfm = vandalMissileSpawnTxfm;
            vandalShipMissileParams.ejectionMaxRotationVel = 0.05f;
            vandalShipMissileParams.ejectionVelocity = 10f;

            vandalShipMissileParams.targetHitHandlers += targetHitHandler;
            vandalShipMissileParams.activationTime = 0.1f;

            cameraMissileParams = new SSpaceMissileParameters();
            cameraMissileParams.targetHitHandlers += targetHitHandler;
            cameraMissileParams.spawnGenerator = null;
            cameraMissileParams.spawnTxfm = cameraMissileSpawnTxfm;
            cameraMissileParams.ejectionMaxRotationVel = 0.05f;
            cameraMissileParams.ejectionVelocity = 10f;
            cameraMissileParams.activationTime = 0.1f;

            // missile manager
            missileManager = new SSpaceMissilesRenderManager(scene, particlesScene, hudScene);
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
                        camera.FollowTarget = targetDrone;
                    } else {
                        camera.FollowTarget = null;
                    }
                    updateTextDisplay();
                }
                break;
            case Key.T:
                do { switchTarget(); } while (getTargetObject() == getLauncherObject());
                updateTextDisplay();
                break;
            case Key.L: 
                switchLauncher();
                while (missileTarget != MissileTargets.Selected
                  && getTargetObject() == getLauncherObject()) { 
                    switchTarget();
                }
                updateTextDisplay();
                break;
            case Key.H:
                switchHitTime();
                updateTextDisplay();
                break;
            case Key.Minus:
                clusterSize = Math.Max(1, clusterSize - 1);
                updateTextDisplay();
                break;
            case Key.Plus:
                clusterSize++;
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
            moveShips(timeElapsed);
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
            if (missileTarget == MissileTargets.Selected) {
                text += "selected: ";
            }
            var targetObj = getTargetObject();
            text += '[' + (targetObj == null ? "none" : targetObj.Name) + ']';

            // hit time
            text += "\n[H] toggle hit time: [" + hitTimeMode.ToString() + ']';

            // num missiles
            text += "\n[+/-] cluster size: [" + clusterSize + ']';

            // debugging
            text += "\n[V] visual debugigng aid: [";
            text += (attackerDroneMissileParams.debuggingAid ? "ON" : "OFF")  + ']';

            textDisplay.Label += text;
        }

        // going deeper into demo logic...

        protected void _launchMissiles()
        {
            if (targets.Count == 0) {
                // initialize targets
                targets.Add(attackerDrone, new SSpaceMissileObjectTarget(attackerDrone));
                targets.Add(targetDrone, new SSpaceMissileObjectTarget(targetDrone));
                targets.Add(vandalShip, new SSpaceMissileObjectTarget(vandalShip));
                targets.Add(scene.ActiveCamera, new SSpaceMissileObjectTarget(scene.ActiveCamera));
            }

            var target = targets[getTargetObject()];
            var launcher = getLauncherObject();
            var hitTime = getHitTime();
            var mParams = getLauncherParams();
            mParams.pursuitHitTimeCorrection = (hitTimeMode != HitTimeMode.Disabled);
            if (target != null) {
                missileManager.launchCluster(
                    launcher.Pos, 
                    Vector3.Zero,
                    clusterSize,
                    target,
                    hitTime,
                    mParams
                );
            }
        }

        protected void moveShips(float timeElapsed)
        {
            // make the target drone move from side to side
            localTime += timeElapsed;
            Vector3 pos = targetDrone.Pos;
            pos.Z = 30f * (float)Math.Sin(localTime);
            targetDrone.Pos = pos;

            // make the vandal ship orbit missile target
            Vector3 desiredPos;
            Vector3 desiredDir;
            float angle = localTime * 0.5f;
            float desiredXOffset = 100f * (float)Math.Cos(angle);
            float desiredYOffset = 20f * (float)Math.Sin(angle * 0.77f);
            float desiredZOffset = 80f * (float)Math.Sin(angle * 0.88f);
            Vector3 desiredOffset = new Vector3 (desiredXOffset, desiredYOffset, desiredZOffset);

            var target = getTargetObject();
            if (missileLauncher != MissileLaunchers.VandalShip || target == null || target == vandalShip) {
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

        protected void targetHitHandler(Vector3 position, SSpaceMissileParameters mParams)
        {
            explosionManager.showExplosion(position, 2.5f);
        }

        protected Matrix4 vandalMissileSpawnTxfm(ISSpaceMissileTarget target, 
                                                 Vector3 launcherPos, Vector3 launcherVel)
        {
            Vector3 targetDir = (target.position - launcherPos).Normalized();
            return vandalShip.worldMat * Matrix4.CreateTranslation(targetDir * 7f);
        }

        protected Matrix4 cameraMissileSpawnTxfm(ISSpaceMissileTarget target, Vector3 
                                                 launcherPos, Vector3 launcherVel)
        {
            Vector3 targetDir = (target.position - launcherPos).Normalized();
            return scene.ActiveCamera.worldMat * Matrix4.CreateTranslation(targetDir * 7f);
        }

        protected void switchTarget()
        {
            int a = (int)missileTarget;
            a = (a + 1) % (int)MissileTargets.End;
            missileTarget = (MissileTargets)a;
        }

        protected void switchLauncher()
        {
            int l = (int)missileLauncher;
            l = (l + 1) % (int)MissileLaunchers.End;
            missileLauncher = (MissileLaunchers)l;
        }

        protected void switchHitTime()
        {
            int h = (int)hitTimeMode;
            h = (h + 1) % (int)HitTimeMode.End;
            hitTimeMode = (HitTimeMode)h;
        }

        protected SSObject getLauncherObject()
        {
            switch (missileLauncher) {
            case MissileLaunchers.AttackerDrone: return attackerDrone;
            case MissileLaunchers.VandalShip: return vandalShip;
            case MissileLaunchers.Camera: return scene.ActiveCamera;
            }
            throw new Exception ("unhandled enum");
        }

        protected SSpaceMissileParameters getLauncherParams()
        {
            switch (missileLauncher) {
            case MissileLaunchers.AttackerDrone: return attackerDroneMissileParams;
            case MissileLaunchers.VandalShip: return vandalShipMissileParams;
            case MissileLaunchers.Camera: return cameraMissileParams;
            }
            throw new Exception ("unhandled enum");
        }

        protected SSObject getTargetObject()
        {
            switch (missileTarget) {
            case MissileTargets.TargetDrone1: return targetDrone;
            case MissileTargets.AttackerDrone: return attackerDrone;
            case MissileTargets.VandalShip: return vandalShip;
            case MissileTargets.Camera: return scene.ActiveCamera;
            case MissileTargets.Selected: return selectedObject ?? scene.ActiveCamera;
            }
            throw new Exception ("unhandled enum");
        }

        protected float getHitTime()
        {
            switch (hitTimeMode) {
            case HitTimeMode.Disabled: return 0f;
            case HitTimeMode.Fixed5s: return 5f;
            case HitTimeMode.Fixed10s: return 10f;
            case HitTimeMode.Fixed15s: return 15f;
            case HitTimeMode.Fixed20s: return 20f;
            case HitTimeMode.Auto:
                // guess based on distance
                var launcher = getLauncherObject();
                var target = getTargetObject();
                float dist = (target.Pos - launcher.Pos).LengthFast;
                return dist / 30f;
            }
            throw new Exception ("unhandled enum");
        }
    }
}

