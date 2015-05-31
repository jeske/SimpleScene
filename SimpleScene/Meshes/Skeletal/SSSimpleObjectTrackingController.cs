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
		/// Direction, in mesh coordinates, where the joint should be "looking" when neutral (nothing to see)
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
			Vector3 jointPosMesh = jointPositionLocal;
			if (joint.Parent != null) {
				jointPosMesh = joint.Parent.CurrentLocation.ApplyTransformTo (jointPosMesh);
			}
			ret.Position = jointPosMesh;

			if (targetObject != null) {
				Vector3 eyesPosWorld = Vector3.Transform (jointPosMesh, _hostObject.worldMat);
				Vector3 targetVecWorld = (targetObject.Pos - eyesPosWorld).Normalized ();

				Quaternion rotOnly = _hostObject.worldMat.ExtractRotation ();
				Vector3 targetVecMesh = Vector3.Transform (targetVecWorld, rotOnly.Inverted ());
				Quaternion neededRotation = OpenTKHelper.getRotationTo (neutralViewDirectionMesh, targetVecMesh, Vector3.UnitX);
				Vector4 test = neededRotation.ToAxisAngle ();

				ret.Orientation = Quaternion.Multiply (neededRotation,
					Quaternion.Multiply (joint.Parent.CurrentLocation.Orientation, neutralViewOrientationLocal)
					);
			} else {
				ret.Orientation = neutralViewOrientationLocal;
				if (joint.Parent != null) {
					ret.Orientation = Quaternion.Multiply (joint.Parent.CurrentLocation.Orientation, ret.Orientation);
				}
			}
			return ret;
		}
	}
}

