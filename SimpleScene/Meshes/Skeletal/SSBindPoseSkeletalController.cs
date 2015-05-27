using System;
using System.Collections.Generic;

namespace SimpleScene
{
	public class SSBindPoseSkeletalController : SSSkeletalChannelController
	{
		protected readonly List<int> _topLevelActiveJoints = null;
		protected readonly Dictionary<int, bool> _jointIsControlledCache = new Dictionary<int, bool>();

		public SSBindPoseSkeletalController (params int[] topLevelJoints)
		{
			if (topLevelJoints != null && topLevelJoints.Length > 0) {
				_topLevelActiveJoints = new List<int> (topLevelJoints);
			}
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			return joint.BaseInfo.BaseLocation;
		}

		public override bool isActive (SSSkeletalJointRuntime joint)
		{
			if (_topLevelActiveJoints == null) {
				return true;
			} else {
				bool jointIsControlled;
				int jointIdx = joint.BaseInfo.JointIndex;
				if (_jointIsControlledCache.ContainsKey (jointIdx)) {
					jointIsControlled = _jointIsControlledCache [jointIdx] ;
				} else {
					if (_topLevelActiveJoints.Contains (jointIdx)) {
						jointIsControlled = true;
					} else if (joint.BaseInfo.ParentIndex == -1) {
						jointIsControlled = false;
					} else {
						jointIsControlled = isActive(joint.Parent);
					}
					_jointIsControlledCache [jointIdx] = jointIsControlled;
				}
				return jointIsControlled;
			}
		}
	}
}

