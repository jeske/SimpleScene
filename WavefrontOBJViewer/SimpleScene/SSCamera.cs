// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;

namespace WavefrontOBJViewer
{
	public class SSCamera : SSObject
	{
		public SSCamera () : base() {
		
		}
		public override void Update() {
			this.updateMat ();
		}
		
		private float DegreeToRadian(float angleInDegrees) {
			return (float)Math.PI * angleInDegrees / 180.0f;
		}
		public void MouseDeltaOrient(float XDelta, float YDelta) {	
			Quaternion yaw_Rotation = Quaternion.FromAxisAngle(Vector3.UnitY,DegreeToRadian(XDelta));
    		Quaternion pitch_Rotation = Quaternion.FromAxisAngle(this.Right,DegreeToRadian(YDelta));

			Quaternion qObjectRotation = this.localMat.ExtractRotation();
			
			qObjectRotation *= pitch_Rotation;
			qObjectRotation *= yaw_Rotation;
						
			Matrix4 newOrientation = Matrix4.CreateFromQuaternion(qObjectRotation);
			this.Orient(newOrientation);
		}
	}
}

