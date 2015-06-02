using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSimpleObjectTrackingController : SSSkeletalChannelController
	{
		/// <summary>
		/// For computing the final position of the joint; in joint-local coordinates
		/// </summary>
		public Vector3 jointPositionLocal = Vector3.Zero;

		/// <summary>
		/// Orientation, in joint-local coordinates, needed to make the joint "look" in the neutral direction
		/// </summary>
		public Quaternion neutralViewOrientationLocal = Quaternion.Identity;

		/// <summary>
		/// Direction, in joint-local coordinates, where the joint should be "looking" when neutral (nothing to see)
		/// </summary>
		public Vector3 neutralViewDirectionMesh = Vector3.UnitX;

		/// <summary>
		/// Target object to be viewed
		/// </summary>
		public SSObject targetObject = null;

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
			SSSkeletalJointLocation ret = new SSSkeletalJointLocation ();
			ret.Position = jointPositionLocal;

			if (targetObject != null) {
				Vector3 targetPosInMesh 
					= Vector3.Transform (targetObject.Pos, _hostObject.worldMat.Inverted());
				Vector3 targetPosInLocal = targetPosInMesh;
				Vector3 neutralDirLocal = neutralViewDirectionMesh;
				if (joint.Parent != null) {
					targetPosInLocal = joint.Parent.CurrentLocation.UndoTransformTo (targetPosInLocal);
					neutralDirLocal = Vector3.Transform (neutralDirLocal, joint.Parent.CurrentLocation.Orientation.Inverted ());
				}

				Vector3 targetDirLocal = targetPosInLocal - jointPositionLocal;
				Quaternion neededRotation = OpenTKHelper.getRotationTo (
					neutralDirLocal, 
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

