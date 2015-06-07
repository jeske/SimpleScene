using System;
using System.Collections.Generic;
using OpenTK;

namespace SimpleScene
{
	/// <summary>
	/// Define only a method by which a joint creates a transform
	/// </summary>
	public class SSCustomizedJointsController : SSBindPoseSkeletalController
	{
		protected Dictionary<int, SSCustomizedJoint> _joints = new Dictionary<int, SSCustomizedJoint>();

		public SSCustomizedJointsController()
		{
		}

		public void addJoint(int jointIdx, SSCustomizedJoint joint)
		{
			_joints [jointIdx] = joint;
		}

		public void removeJoint(int jointIdx)
		{
			_joints.Remove (jointIdx);
		}

		public override bool isActive (SSSkeletalJointRuntime joint)
		{
			int jidx = joint.baseInfo.jointIndex;
			return _joints.ContainsKey (jidx) && _joints [jidx].isActive (joint);
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			return _joints [joint.baseInfo.jointIndex].computeJointLocation (joint);
		}
	}

	// ----------------------------------------------------------

	public abstract class SSCustomizedJoint 
	{
		/// <summary>
		/// In joint-local coordinates, as defined by individual parametric joints
		/// </summary>
		public abstract SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint);

		public virtual bool isActive (SSSkeletalJointRuntime joint) { return true; }
	}
}

