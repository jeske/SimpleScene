// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;

namespace SimpleScene
{
	public class SSCameraThirdPerson : SSCamera
	{
		public SSObject FollowTarget;
		public float FollowDistance = 10.0f;
		public float minFollowDistance = 0.5f;
		public float maxFollowDistance = 500.0f;
		public Vector3 BasePos;

		public SSCameraThirdPerson (SSObject followTarget) : base() {
			this.FollowTarget = followTarget;			
		}
		public SSCameraThirdPerson() : base() {
		}

		public SSCameraThirdPerson (Vector3 origin) : base() {
			this.BasePos = origin;
		}
		public override void Update(float fElapsedMS) {
			Vector3 targetPos = BasePos;
			// FPS follow the target
			if (this.FollowTarget != null) {
				targetPos = this.FollowTarget.Pos;
			} 
			
			FollowDistance = OpenTKHelper.Clamp(FollowDistance,minFollowDistance,maxFollowDistance);
			
			// one way to have a third person camera, is to position ourselves
			// relative to our target object, and our current camera-direction
			
			// TODO: why are positive follow distances producing the correct orientation? 
			//    it feels like something is inverted

			this.Pos = targetPos + (this.Dir * FollowDistance);
			// Console.WriteLine("Camera Up {0} / Dir {1} / Right {2}",this.Up,this.Dir,this.Right);
			// Console.WriteLine("Camera Pos = {0}",this.Pos);
			base.Update(fElapsedMS);
		}
	}
}

