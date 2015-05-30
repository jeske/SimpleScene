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
		/// Where the eyes are facing in the absense of a target; in joint-local coordinates
		/// </summary>
		public Vector3 eyeViewNeutralDirection = Vector3.UnitY;

		/// <summary>
		/// Target object to be viewed
		/// </summary>
		public readonly SSObject targetObject = null;

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
			Quaternion targetOrientationWorld = targetObject.worldMat.ExtractRotation ();
			Vector3 targetVecMesh = Vector3.Transform (targetVecWorld, targetOrientationWorld.Inverted()); 

			Vector3 targetVecJoint = targetVecMesh - eyePositionOffset;
			if (joint.Parent != null) {
				targetVecJoint -= joint.Parent.CurrentLocation.Position;
			}

			Vector3 cross = Vector3.Cross (targetVecJoint, eyeViewNeutralDirection);
			float rotAngle 	= (float)Math.Asin (
				cross.LengthFast / targetVecJoint.LengthFast / eyeViewNeutralDirection.LengthFast);
			Quaternion rot = Quaternion.FromAxisAngle (cross, rotAngle);

			SSSkeletalJointLocation ret;
			ret.Position = eyePositionOffset;
			ret.Orientation = rot;
			return ret;
		}

	}
}

