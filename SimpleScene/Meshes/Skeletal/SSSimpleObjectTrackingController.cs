using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSimpleObjectTrackingController : SSSkeletalChannelController
	{
		/// <summary>
		/// offset to add to the viewing position, in joint-local coordinates
		/// </summary>
		public Vector3 eyePositionOffset = Vector3.Zero;

		/// <summary>
		/// For computing the final position of the joint; joint-local coordinates
		/// </summary>
		public Vector3 jointPositionOffset = Vector3.Zero;

		/// <summary>
		/// Where the eyes are facing in the absense of a target; in joint-local coordinates
		/// </summary>
		public Vector3 eyeViewNeutralDirection = Vector3.UnitZ;

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
			return joint.BaseInfo.JointIndex == _jointIdx && targetObject != null;
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			Vector3 targetVecWorld = (targetObject.Pos - _hostObject.Pos);
			Vector3 targetVecMesh = Vector3.Transform (targetVecWorld, _hostObject.worldMat.Inverted()); 

			Vector3 targetVecJoint = targetVecMesh - eyePositionOffset;
			if (joint.Parent != null) {
				targetVecJoint -= joint.Parent.CurrentLocation.Position;
				targetVecJoint = Vector3.Transform (targetVecJoint, joint.Parent.CurrentLocation.Orientation.Inverted ());
			}
			targetVecJoint.Normalize ();

			Vector3 cross = Vector3.Cross (targetVecJoint, eyeViewNeutralDirection);
			float rotAngle 	= (float)Math.Asin (
				cross.LengthFast / targetVecJoint.LengthFast / eyeViewNeutralDirection.LengthFast);
			Quaternion rot = Quaternion.FromAxisAngle (cross.Normalized(), rotAngle);

			SSSkeletalJointLocation ret;
			ret.Position = jointPositionOffset;
			ret.Orientation = rot;
			if (joint.Parent != null) {
				ret.ApplyPrecedingTransform (joint.Parent.CurrentLocation);
			}
			return ret;
		}
	}
}

