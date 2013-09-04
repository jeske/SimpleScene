// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;

using OpenTK;

namespace WavefrontOBJViewer
{
	public class SSCameraThirdPerson : SSCamera
	{
		SSObject FollowTarget;
		float followDistance = 10.0f;

		public SSCameraThirdPerson (SSObject followTarget) {
			this.FollowTarget = followTarget;
		}
		public override void Update() {
			// FPS follow the target
			// this.Pos = this.FollowTarget.Pos + (this.Dir * -followDistance);
		}
	}
}

