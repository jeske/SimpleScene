using System;

namespace SimpleScene
{
	public class SSBindPoseSkeletalController : SSSkeletalChannelController
	{
		public SSBindPoseSkeletalController ()
		{
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			return joint.BaseInfo.BaseLocation;
		}
	}
}

