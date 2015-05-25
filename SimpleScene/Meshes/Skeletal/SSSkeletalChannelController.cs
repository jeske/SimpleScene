using System;

namespace SimpleScene
{
	public abstract class SSSkeletalChannelController
	{
		protected bool interChannelFade = true;

		public SSSkeletalChannelController ()
		{
		}

		public abstract bool isActive(int jointIdx, SSSkeletalJointRuntime joint);
		public abstract bool isFadingOut(int jointIdx, SSSkeletalJointRuntime joint);
		public abstract void computeJointLocation(int jointIdx, SSSkeletalJointRuntime joint);
		public virtual void update(float timeElapsed) { }
		public virtual float interChannelFadeIndentisy() { return 1f; }
	}
}

