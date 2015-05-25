using System;

namespace SimpleScene
{
	public class SSBindPoseSkeletalController : SSSkeletalChannelController
	{
		public SSBindPoseSkeletalController ()
		{
		}

		public override SSSkeletalJointLocation computeJointLocation (int jointIdx, SSSkeletalJointRuntime joint)
		{
			return joint.BaseInfo.BaseLocation;
		}
	}
}

