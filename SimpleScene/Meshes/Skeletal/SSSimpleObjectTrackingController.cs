using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSimpleObjectTrackingController : SSSkeletalChannelController
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
				_neutralViewDirectionDirty = true;
			}
		}

		/// <summary>
		/// Target object to be viewed
		/// </summary>
		public SSObject targetObject = null;

		protected Vector3 _neutralViewDirectionBindPose = Vector3.UnitX;
		protected Vector3 _neutralViewDirectionLocal;
		protected bool _neutralViewDirectionDirty = true;

		protected readonly SSObjectMesh _hostObject;
		protected readonly int _jointIdx;

		public int JointIndex { get { return _jointIdx; } }

		public SSSimpleObjectTrackingController (int jointIdx, SSObjectMesh hostObject)
		{
			_jointIdx = jointIdx;
			_hostObject = hostObject;
		}

		public override bool isActive (SSSkeletalJointRuntime joint)
		{
			return joint.BaseInfo.JointIndex == _jointIdx;
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			if (_neutralViewDirectionDirty) {
				Quaternion precedingBindPoseOrient = Quaternion.Identity;
				for (var j = joint.Parent; j != null; j = j.Parent) {
					precedingBindPoseOrient = Quaternion.Multiply (
						joint.BaseInfo.BindPoseLocation.Orientation, precedingBindPoseOrient);
				}
				_neutralViewDirectionLocal = Vector3.Transform (
					_neutralViewDirectionBindPose, precedingBindPoseOrient.Inverted());
				_neutralViewDirectionDirty = false;
			}

			SSSkeletalJointLocation ret = new SSSkeletalJointLocation ();
			ret.Position = jointPositionLocal;

			if (targetObject != null) {
				Vector3 targetPosInMesh 
					= Vector3.Transform (targetObject.Pos, _hostObject.worldMat.Inverted());
				Vector3 targetPosInLocal = targetPosInMesh;
				if (joint.Parent != null) {
					targetPosInLocal = joint.Parent.CurrentLocation.UndoTransformTo (targetPosInLocal);
				}
				Vector3 targetDirLocal = targetPosInLocal - jointPositionLocal;

				Quaternion neededRotation = OpenTKHelper.getRotationTo (
					_neutralViewDirectionLocal, 
					targetDirLocal, Vector3.UnitX);
				ret.Orientation = Quaternion.Multiply(neutralViewOrientationLocal, neededRotation);
				//Vector4 test = neededRotation.ToAxisAngle ();
			} else {
				ret.Orientation = neutralViewOrientationLocal;
			}
			return ret;
		}
	}
}

