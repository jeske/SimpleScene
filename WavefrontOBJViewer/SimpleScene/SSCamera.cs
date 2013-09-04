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
		private float DegreeToRadian(float angleInDegrees) {
			return (float)Math.PI * angleInDegrees / 180.0f;
		}
		public void MouseDeltaOrient(float XDelta, float YDelta) {	
			Quaternion pitch_Rotation = Quaternion.FromAxisAngle(this._right,DegreeToRadian(YDelta));
			Quaternion yaw_Rotation = Quaternion.FromAxisAngle(Vector3.UnitY,DegreeToRadian(XDelta));
			
			Quaternion qObjectRotation = this.localMat.ExtractRotation();
			
			qObjectRotation = Quaternion.Multiply(qObjectRotation,yaw_Rotation);
			qObjectRotation = Quaternion.Multiply(qObjectRotation,pitch_Rotation);
						
			Matrix4 newOrientation = Matrix4.CreateFromQuaternion(qObjectRotation);
			this.Orient(newOrientation);
		}
	}
}

