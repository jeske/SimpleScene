using System;

namespace WavefrontOBJViewer
{
	public class SSCameraThirdPerson : SSCamera
	{
		SSObject FollowTarget;

		public SSCameraThirdPerson (SSObject followTarget) {
			this.FollowTarget = followTarget;
		}
	}
}

