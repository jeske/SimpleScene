// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;

namespace SimpleScene
{
	public class SSCamera : SSObject
	{
        protected float pitchAngle = 0f;
        protected float yawAngle = 0f;


        public SSCamera () : base() {
		
		}

        public virtual void preRenderUpdate(float timeElapsed)
        {
            this.calcMatFromState ();
        }

		protected float DegreeToRadian(float angleInDegrees) {
			return (float)Math.PI * angleInDegrees / 180.0f;
		}

        public void mouseDeltaOrient(float XDelta, float YDelta) 
        {
            if (pitchAngle > (float)Math.PI/2f && pitchAngle < 1.5f*(float)Math.PI) {
                // upside down
                XDelta *= -1f;
            }

            pitchAngle += DegreeToRadian(YDelta);
            yawAngle += DegreeToRadian(XDelta);

            const float twoPi = (float)(2.0 * Math.PI);
            if (pitchAngle < 0f) {
                pitchAngle += twoPi;
            } else if (pitchAngle > twoPi) {
                pitchAngle -= twoPi;
            }


            //var pitchOri = Quaternion.FromAxisAngle(this.Up, pitchAngle);
            var pitchOri = Quaternion.FromAxisAngle(Vector3.UnitY, -yawAngle);
            var yawOri = Quaternion.FromAxisAngle(Vector3.UnitX, -pitchAngle);
            this.Orient(pitchOri * yawOri);

            this.calcMatFromState(); // make sure our local matrix is current

            // openGL requires pre-multiplation of these matricies...
            //Quaternion qResult = yawDelta * pitch_Rotation * this.localMat.ExtractRotation();


        }
	}
}

