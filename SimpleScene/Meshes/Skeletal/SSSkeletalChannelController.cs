using System;

namespace SimpleScene
{
	public abstract class SSSkeletalChannelController
	{
		/// <summary>
		/// Set to false to disable inter-channel fade, even when implemented by a controller
		/// </summary>
		public bool interChannelFade = true;

		/// <summary>
		/// Returns joint location as defined by controller in mesh coordinates
		/// </summary>
		public abstract SSSkeletalJointLocation computeJointLocation(SSSkeletalJointRuntime joint);

		public virtual void update(float timeElapsed) { }
		public virtual bool isActive(SSSkeletalJointRuntime joint) { return true; }
		public virtual float interChannelFadeIndentisy() { return 1f; }
	}
}

