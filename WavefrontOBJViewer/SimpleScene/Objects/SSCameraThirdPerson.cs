// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


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

