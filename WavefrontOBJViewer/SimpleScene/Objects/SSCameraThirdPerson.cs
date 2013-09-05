// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;

using OpenTK;

namespace WavefrontOBJViewer
{
	public class SSCameraThirdPerson : SSCamera
	{
		SSObject FollowTarget;
		public float followDistance = 10.0f;
		public Vector3 basePos;

		public SSCameraThirdPerson (SSObject followTarget) : base() {
			this.FollowTarget = followTarget;			
		}
		public override void Update() {
			Vector3 pos = basePos;
			// FPS follow the target
			if (this.FollowTarget != null) {
				pos = this.FollowTarget.Pos;
			} 
			
			this.Pos = pos + (this.Dir * -followDistance);
			this.updateMat ();

		}
	}
}

