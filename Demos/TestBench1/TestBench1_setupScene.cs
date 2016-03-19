// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

using SimpleScene;
using SimpleScene.Demos;

namespace TestBench1
{
	partial class TestBench1 : TestBenchBootstrap
	{
		protected override void setupScene()
		{
			base.setupScene ();

			// checkerboard floor
			#if true
			{
				SSTexture tex = SSAssetManager.GetInstance<SSTexture> ("checkerboard.png");
				const float tileSz = 4f;
				const int gridSz = 10;
				var tileVertices = new SSVertex_PosNormTex[SSTexturedNormalQuad.c_doubleFaceVertices.Length];
				SSTexturedNormalQuad.c_doubleFaceVertices.CopyTo(tileVertices, 0);
				for (int v = 0; v < tileVertices.Length; ++v) {
					tileVertices[v].TexCoord *= (float)gridSz;
				}

				var quadMesh = new SSVertexMesh<SSVertex_PosNormTex>(tileVertices);
				quadMesh.textureMaterial = new SSTextureMaterial(tex);
				var tileObj = new SSObjectMesh(quadMesh);
				tileObj.Name = "Tiles";
				tileObj.selectable = false;
				tileObj.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, (float)Math.PI/2f));
				tileObj.Scale = new Vector3(tileSz * gridSz);
				//tileObj.boundingSphere = new SSObjectSphere(0f);
				main3dScene.AddObject(tileObj);
			}
			#endif

			// skeleton mesh test
			#if true
			{
				SSSkeletalAnimation animIdle
					= SSAssetManager.GetInstance<SSSkeletalAnimationMD5>("./boneman/boneman_idle.md5anim");
				SSSkeletalAnimation animRunning
					= SSAssetManager.GetInstance<SSSkeletalAnimationMD5>("./boneman/boneman_running.md5anim");
				SSSkeletalAnimation animAttack
				= SSAssetManager.GetInstance<SSSkeletalAnimationMD5>("./boneman/boneman_attack.md5anim");

				SSSkeletalMesh[] meshes 
					= SSAssetManager.GetInstance<SSSkeletalMeshMD5[]>("./boneman/boneman.md5mesh");
				var tex = SSAssetManager.GetInstance<SSTexture>("./boneman/skin.png");
				foreach (var skeliMesh in meshes) {

				#if true
				var renderMesh0 = new SSSkeletalRenderMesh(skeliMesh);
				var obj0 = new SSObjectMesh(renderMesh0);
				obj0.MainColor = Color4.DarkGray;
				obj0.Name = "grey bones (bind pose)";
				obj0.Pos = new Vector3(-18f, 0f, -18f);
				obj0.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				main3dScene.AddObject(obj0);

				tracker0 = new SSSimpleObjectTrackingJoint(obj0);
				tracker0.jointPositionLocal = animIdle.computeJointFrame(11, 0).position;
				tracker0.neutralViewOrientationLocal = animIdle.computeJointFrame(11, 0).orientation;
				tracker0.neutralViewDirectionBindPose = Vector3.UnitY;
				tracker0.neutralViewUpBindPose = Vector3.UnitZ;
				renderMesh0.addCustomizedJoint(11, tracker0);
				#endif

				#if true
				var renderMesh1 = new SSSkeletalRenderMesh(skeliMesh);
				var obj1 = new SSObjectMesh(renderMesh1);
				obj1.MainColor = Color4.DarkRed;
				obj1.Name = "red bones (running loop)";
				obj1.Pos = new Vector3(6f, 0f, -12f);
				obj1.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				main3dScene.AddObject(obj1);

				renderMesh1.playAnimationLoop(animRunning, 0f);
				#endif

				#if true
				var renderMesh2 = new SSSkeletalRenderMesh(skeliMesh);
				renderMesh2.playAnimationLoop(animIdle, 0f, "all");
				renderMesh2.playAnimationLoop(animRunning, 0f, "LeftClavicle", "RightClavicle");
				var obj2 = new SSObjectMesh(renderMesh2);
				obj2.MainColor = Color.Green;
				obj2.Name = "green bones (idle + running loop mixed)";
				obj2.Pos = new Vector3(0f, 0f, -12f);
				obj2.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				main3dScene.AddObject(obj2);
				#endif

				#if true
				var renderMesh3 = new SSSkeletalRenderMesh(skeliMesh);
				renderMesh3.playAnimationLoop(animIdle, 0f, "all");
				var obj3 = new SSObjectMesh(renderMesh3);
				obj3.MainColor = Color.DarkCyan;
				obj3.Name = "blue bones (idle loop)";
				obj3.Pos = new Vector3(-6f, 0f, -12f);
				obj3.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				main3dScene.AddObject(obj3);
				#endif
				
				// state machines setup for skeletal render mesh 4 and 5
				var skeletonWalkDescr = new SSAnimationStateMachine();
				skeletonWalkDescr.addState("idle", animIdle, true);		
				skeletonWalkDescr.addState("running1", animRunning);
				skeletonWalkDescr.addState("running2", animRunning);
				skeletonWalkDescr.addAnimationEndsTransition("idle", "running1", 0.3f);
				skeletonWalkDescr.addAnimationEndsTransition("running1", "running2", 0f);
				skeletonWalkDescr.addAnimationEndsTransition("running2", "idle", 0.3f);

				var skeletonAttackDescr = new SSAnimationStateMachine();
				skeletonAttackDescr.addState("inactive", null, true);
				skeletonAttackDescr.addState("attack", animAttack);
				skeletonAttackDescr.addStateTransition(null, "attack", 0.5f);
				skeletonAttackDescr.addAnimationEndsTransition("attack", "inactive", 0.5f);

				#if true
				// state machine test (in slow motion)
				var renderMesh4 = new SSSkeletalRenderMesh(skeliMesh);
				renderMesh4.timeScale = 0.25f;

				var obj4 = new SSObjectMesh(renderMesh4);
				obj4.MainColor = Color.DarkMagenta;
				obj4.Name = "magenta bones (looping idle/walk; interactive attack; slowmo)";
				obj4.Pos = new Vector3(-12f, 0f, 0f);
				obj4.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				main3dScene.AddObject(obj4);

				var renderMesh4WallSm = renderMesh4.addStateMachine(skeletonWalkDescr, "all");
				renderMesh4AttackSm = renderMesh4.addStateMachine(skeletonAttackDescr, "LeftClavicle", "RightClavicle");

				tracker4 = new SSSimpleObjectTrackingJoint(obj4);
				tracker4.jointPositionLocal = animRunning.computeJointFrame(11, 0).position;
				tracker4.neutralViewOrientationLocal = animRunning.computeJointFrame(11, 0).orientation;
				tracker4.neutralViewDirectionBindPose = Vector3.UnitY;
				renderMesh4.addCustomizedJoint(11, tracker4);
				#endif

				#if true
				// another mesh, using the same state machine but running at normal speed
				var renderMesh5 = new SSSkeletalRenderMesh(skeliMesh);
				var renderMesh5WalkSm = renderMesh5.addStateMachine(skeletonWalkDescr, "all");
				renderMesh5AttackSm = renderMesh5.addStateMachine(skeletonAttackDescr, "LeftClavicle", "RightClavicle");
				var obj5 = new SSObjectMesh(renderMesh5);
				obj5.Name = "orange bones (looping idle/walk, interactive attack + parametric neck rotation)";
				obj5.Pos = new Vector3(12f, 0f, 0f);
				obj5.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
				obj5.MainColor = Color4.DarkOrange;
				main3dScene.AddObject(obj5);

				renderMesh5NeckJoint = new SSPolarJoint();
				renderMesh5NeckJoint.positionOffset = new Vector3(0f, 0.75f, 0f);
				renderMesh5.addCustomizedJoint("UpperNek", renderMesh5NeckJoint);
				#endif
				}
			}
			#endif

			#if true
			// bob mesh test
			{
				var bobMeshes = SSAssetManager.GetInstance<SSSkeletalMeshMD5[]>(
					"./bob_lamp/bob_lamp_update.md5mesh");
				var bobAnim = SSAssetManager.GetInstance<SSSkeletalAnimationMD5>(
					"./bob_lamp/bob_lamp_update.md5anim");
				var bobRender = new SSSkeletalRenderMesh(bobMeshes);
				bobRender.playAnimationLoop(bobAnim, 0f);
				bobRender.alphaBlendingEnabled = true;
				bobRender.timeScale = 0.5f;
				var bobObj = new SSObjectMesh(bobRender);
				bobObj.Name = "Bob";
				bobObj.Pos = new Vector3(10f, 0f, 10f);
				bobObj.Orient(Quaternion.FromAxisAngle(Vector3.UnitX, -(float)Math.PI/2f));
                alpha3dScene.AddObject(bobObj);
			}
			#endif
		}

		protected override void setupCamera()
		{
			var camera = new SSCameraThirdPerson (null);
			camera.basePos = new Vector3 (0f, 10f, 0f);
			camera.followDistance = 50.0f;
			main3dScene.ActiveCamera = camera;

			if (tracker0 != null) {
				tracker0.targetObject = main3dScene.ActiveCamera;
			}
			if (tracker4 != null) {
				tracker4.targetObject = main3dScene.ActiveCamera;
			}
		}
	}
}