using System;

namespace SimpleScene
{
	public abstract class SSSkeletalChannelController
	{
		protected bool interChannelFade = true;

		public abstract SSSkeletalJointLocation computeJointLocation(int jointIdx, SSSkeletalJointRuntime joint);

		public virtual void update(float timeElapsed) { }
		public virtual bool isActive(int jointIdx, SSSkeletalJointRuntime joint) { return true; }
		public virtual bool isFadingOut(int jointIdx, SSSkeletalJointRuntime joint) { return false; }
		public virtual float interChannelFadeIndentisy() { return 1f; }
	}
}

