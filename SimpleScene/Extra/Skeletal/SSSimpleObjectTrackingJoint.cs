﻿using System;
using OpenTK;

namespace SimpleScene.Demos
{
	public class SSSimpleObjectTrackingJoint : SSCustomizedJoint
	{
		/// <summary>
		/// Fixed position of the joint in joint-local coordinates
		/// </summary>
		public Vector3 jointPositionLocal = Vector3.Zero;

		/// <summary>
		/// Orientation, in joint-local coordinates, that makes the joint "look" at nothing (neutral)
		/// </summary>
		public Quaternion neutralViewOrientationLocal = Quaternion.Identity;

		/// <summary>
		/// Direction, in mesh coordinates, where the joint should be "looking" while in bind pose with 
		/// nothing to see
		/// </summary>
		public Vector3 neutralViewDirectionBindPose {
			get { return _neutralViewDirectionBindPose; }
			set { 
				_neutralViewDirectionBindPose = value;
				_neutralViewDirty = true;
			}
		}

		public Vector3 neutralViewUpBindPose {
			get { return _neutralViewUpBindPose; }
			set {
				_neutralViewUpBindPose = value;
				_neutralViewDirty = true;
			}
		}

		/// <summary>
		/// Target object to be viewed
		/// </summary>
		public SSObject targetObject = null;

		protected Vector3 _neutralViewDirectionBindPose = Vector3.UnitY;
		protected Vector3 _neutralViewUpBindPose = Vector3.UnitZ;

		protected Vector3 _neutralViewDirectionLocal; // "X"
		protected Vector3 _neutralViewUpLocal;        // "Z"
		protected Vector3 _neutralViewYLocal;         // "Y"

		protected bool _neutralViewDirty = true;

		protected readonly SSObjectMesh _hostObject;

		public SSSimpleObjectTrackingJoint (SSObjectMesh hostObject, SSObject target = null)
		{
			_hostObject = hostObject;
			targetObject = target;
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			if (_neutralViewDirty) {
				Quaternion precedingBindPoseOrient = Quaternion.Identity;
				for (var j = joint.parent; j != null; j = j.parent) {
					precedingBindPoseOrient = Quaternion.Multiply (
						joint.baseInfo.bindPoseLocation.orientation, precedingBindPoseOrient);
				}
				Quaternion precedingInverted = precedingBindPoseOrient.Inverted ();
				_neutralViewDirectionLocal = Vector3.Transform (_neutralViewDirectionBindPose, precedingInverted)
					.Normalized();
				_neutralViewUpLocal = Vector3.Transform (_neutralViewUpBindPose, precedingInverted)
					.Normalized();
				_neutralViewYLocal = Vector3.Transform (
					Vector3.Cross (_neutralViewDirectionBindPose, _neutralViewUpBindPose), precedingInverted)
					.Normalized();
				_neutralViewDirty = false;
			}

			SSSkeletalJointLocation ret = new SSSkeletalJointLocation ();
			ret.position = jointPositionLocal;

			if (targetObject != null) {

                //Vector3 targetPosInMesh = Vector3.Transform (targetObject.Pos, _hostObject.worldMat.Inverted());
                // some_name code start 24112019
                Vector3 targetPosInMesh = (new Vector4(targetObject.Pos.X, targetObject.Pos.Y, targetObject.Pos.Z, 1) * _hostObject.worldMat.Inverted()).Xyz;
                // some_name code end
                Vector3 targetPosInLocal = targetPosInMesh;
				if (joint.parent != null) {
					targetPosInLocal = joint.parent.currentLocation.undoTransformTo (targetPosInLocal);
				}
				Vector3 targetDirLocal = targetPosInLocal - jointPositionLocal;
				Vector3 nvDir = new Vector3();
				nvDir.X = Vector3.Dot (targetDirLocal, _neutralViewDirectionLocal);
				nvDir.Y = Vector3.Dot (targetDirLocal, _neutralViewYLocal);
				nvDir.Z = Vector3.Dot (targetDirLocal, _neutralViewUpLocal);

				float theta = -(float)Math.Atan2 (nvDir.Y, nvDir.X);
				float phi = (float)Math.Atan2 (nvDir.Z, nvDir.Xy.LengthFast);

				Quaternion neededRotation = Quaternion.FromAxisAngle (_neutralViewUpLocal, theta) 
										  * Quaternion.FromAxisAngle (_neutralViewYLocal, phi);
				ret.orientation = neutralViewOrientationLocal * neededRotation;
			} else {
				ret.orientation = neutralViewOrientationLocal;
			}
			return ret;
		}
	}
}

