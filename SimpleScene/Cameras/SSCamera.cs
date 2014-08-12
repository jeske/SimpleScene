// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;

namespace SimpleScene
{
	public class SSCamera : SSObject
	{
		public SSCamera () : base() {
		
		}
		public override void Update() {
			this.calcMatFromState ();
		}
		private float DegreeToRadian(float angleInDegrees) {
			return (float)Math.PI * angleInDegrees / 180.0f;
		}

		public void MouseDeltaOrient_alternate(float XDelta, float YDelta) {
			Quaternion yaw_Rotation = Quaternion.FromAxisAngle(Vector3.UnitY,DegreeToRadian(-XDelta));
    		Quaternion pitch_Rotation = Quaternion.FromAxisAngle(this.Right,DegreeToRadian(-YDelta));

			this.calcMatFromState(); // make sure our local matrix is current

			// openGL requires pre-multiplation of these matricies...
			Quaternion qResult = yaw_Rotation * pitch_Rotation * this.localMat.ExtractRotation();
						
			this.Orient(qResult);
		}

	}
}

