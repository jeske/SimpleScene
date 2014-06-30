// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;

namespace WavefrontOBJViewer
{
	public class SSCameraThirdPerson : SSCamera
	{
		SSObject FollowTarget;
		public float followDistance = -10.0f;
		public Vector3 basePos;
		Vector3 xaxis = Vector3.UnitX;
		Vector3 yaxis = Vector3.UnitY;
		Vector3 zaxis = Vector3.UnitZ;

		public SSCameraThirdPerson (SSObject followTarget) : base() {
			this.FollowTarget = followTarget;			
		}
		public SSCameraThirdPerson (Vector3 origin) : base() {
			this.basePos = origin;
		}
		public override void Update() {
			Vector3 pos = basePos;
			// FPS follow the target
			if (this.FollowTarget != null) {
				pos = this.FollowTarget.Pos;
			} 
			
			// one way to have a third person camera, is to position ourselves
			// relative to our target object, and our current camera-direction
			
			this.Pos = pos + (this.Dir * -followDistance);
			Console.WriteLine("Camera Up {0} / Dir {1} / Right {2}",this.Up,this.Dir,this.Right);
			// Console.WriteLine("Camera Pos = {0}",this.Pos);
			base.Update();
		}
	}
}

