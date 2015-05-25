using System;

namespace SimpleScene
{
	public abstract class SSSkeletalChannelController
	{
		public bool interChannelFade = true;

		public abstract SSSkeletalJointLocation computeJointLocation(SSSkeletalJointRuntime joint);

		public virtual void update(float timeElapsed) { }
		public virtual bool isActive(SSSkeletalJointRuntime joint) { return true; }
		public virtual bool isFadingOut(SSSkeletalJointRuntime joint) { return false; }
		public virtual float interChannelFadeIndentisy() { return 1f; }
	}
}

