using System;

using OpenTK;

namespace WavefrontOBJViewer
{
	public struct SSRay
	{
	    public Vector3 pos;
	    public Vector3 dir;

		public SSRay (Vector3 pos, Vector3 dir) {
			this.pos = pos;
			this.dir = dir;
		}

		public static SSRay FromTwoPoints(Vector3 p1, Vector3 p2) {
		    
		    Vector3 pos = p1;
		    Vector3 dir = (p2 - p1).Normalized();


		    return new SSRay(pos,dir);
		}

		public override string ToString() {
		    return String.Format("({0}) -> v({1})",pos,dir);
		}
	}
}

