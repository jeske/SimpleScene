// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;

using OpenTK;
using SimpleScene;

namespace SimpleScene.Util3d
{
    // NOTE: this class is not working properly yet.... something wrong with the math...

	// http://www.crownandcutlass.com/features/technicaldetails/frustum.html
	// http://www.lighthouse3d.com/tutorials/view-frustum-culling/
	// http://www.lighthouse3d.com/tutorials/view-frustum-culling/clip-space-approach-extracting-the-planes/
	
	// classic opengl index order, helpful for translating code...
    //    http://www.learnopengles.com/understanding-opengls-matrices/
	//  0   4   8  12
    //  1   5   9  13
    //  2   6  10  14
    //  3   7  11  15

	public struct SSPlane3d {
        public const float epsilon = 0.0001f;

		public float A,B,C,D;

        public Vector3 normal {
            get {
                return new Vector3 (A, B, C);
            }
        }

		public void Normalize() {
			float t = (float)Math.Sqrt(A*A + B*B + C*C);
			A /= t;
			B /= t;
			C /= t;
			D /= t;
		}	

		public float DistanceToPoint(Vector3 point) {
			return (A*point.X + B*point.Y + C*point.Z + D);
		}

		public override string ToString() {
			return String.Format("Plane3d({0}x+{1}y+{2}z+{3}=0)",A,B,C,D);
		}

        /// <summary>
        /// Whether the plane and a ray intersect and where
        /// http://en.wikipedia.org/wiki/Line%E2%80%93plane_intersection
        /// </summary>
        public bool intersects (ref SSRay ray, out Vector3 intersectPt) 
        {
            Vector3 n = this.normal;
            var rayDirDotPlaneN = Vector3.Dot(ray.dir, n);
            if (Math.Abs(rayDirDotPlaneN) < epsilon) {
                // rayDirDotPlaneN == 0; ray and the plane are parallel
                intersectPt = new Vector3 (float.NaN);
                return false;
            } else {
                // plug parametric equation of a line into the plane normal equation
                // solve (rayPos + rayDir * t - planeP0) dot planeN == 0 
                // rayDir dot planeN + (rayPos - planeP0) dot planeN == 0
                Vector3 p0 = this.pickASurfacePoint();
                float t = Vector3.Dot(p0 - ray.pos, n) / rayDirDotPlaneN;
                if (t < -epsilon) {
                    // this means that the line-plane intersection is behind the ray origin (in the wrong
                    // direction). in the context of a ray this means no intersection
                    intersectPt = new Vector3 (float.NaN);
                    return false;
                }
                intersectPt = ray.pos + ray.dir * t;
                return true;
            }
        }

        /// <summary>
        /// Picks a point p0 (x0, y0, z0) that lies on a plane.
        /// </summary>
        public Vector3 pickASurfacePoint()
        {
            // TODO epsilonize
            if (Math.Abs(A) > epsilon) { 
                // if A != 0
                // let y0 == 0 and z0 == 0; then -A * x0 == D
                float x0 = D / -A;
                return new Vector3 (x0, 0f, 0f);
            } else if (Math.Abs(B) > epsilon) {
                // else if B != 0
                // let x0 == 0 and z0 == 0; then -B * y0 == D
                float y0 = D / -B;
                return new Vector3 (0f, y0, 0f);
            } else if (Math.Abs(C) > epsilon) { 
                // else if C != 0
                // let x0 == 0 and y0 == 0; then -C * z0 == D
                float z0 = D / -C;
                return new Vector3 (0f, 0f, z0);
            } else {
                throw new Exception ("invalid plane normal: " + this.normal);
            }
        }
	}

	public class SSFrustumCuller
	{
		public static int RIGHT = 0;		
        public static int LEFT = 1;
        public static int BOTTOM = 2;
        public static int TOP = 3;
        public static int FAR = 4;
        public static int NEAR = 5;
        public static int NUM_PLANES = 6;
		//static string[] PlaneNames = { "RIGHT", "LEFT", "BOTTOM", "TOP", "FAR", "NEAR" };
		
        public SSPlane3d[] frumstumPlanes { get { return this.FrustumPlane; } }

        protected readonly SSPlane3d[] FrustumPlane = new SSPlane3d[NUM_PLANES];

		public SSFrustumCuller (ref Matrix4 m) {
			// extract the six culling planes

			FrustumPlane[RIGHT].A = m.M14 - m.M11;
			FrustumPlane[RIGHT].B = m.M24 - m.M21;
			FrustumPlane[RIGHT].C = m.M34 - m.M31;
			FrustumPlane[RIGHT].D = m.M44 - m.M41;
			FrustumPlane[RIGHT].Normalize();

			FrustumPlane[LEFT].A = m.M14 + m.M11;
			FrustumPlane[LEFT].B = m.M24 + m.M21;
			FrustumPlane[LEFT].C = m.M34 + m.M31;
			FrustumPlane[LEFT].D = m.M44 + m.M41;
			FrustumPlane[LEFT].Normalize();

			FrustumPlane[BOTTOM].A = m.M14 + m.M12;
			FrustumPlane[BOTTOM].B = m.M24 + m.M22;
			FrustumPlane[BOTTOM].C = m.M34 + m.M32;
			FrustumPlane[BOTTOM].D = m.M44 + m.M42;
			FrustumPlane[BOTTOM].Normalize();

			FrustumPlane[TOP].A = m.M14 - m.M12;
			FrustumPlane[TOP].B = m.M24 - m.M22;
			FrustumPlane[TOP].C = m.M34 - m.M32;
			FrustumPlane[TOP].D = m.M44 - m.M42;
			FrustumPlane[TOP].Normalize();

			FrustumPlane[NEAR].A = m.M14 + m.M13;
			FrustumPlane[NEAR].B = m.M24 + m.M23;
			FrustumPlane[NEAR].C = m.M34 + m.M33;
			FrustumPlane[NEAR].D = m.M44 + m.M43;
			FrustumPlane[NEAR].Normalize();

			FrustumPlane[FAR].A = m.M14 - m.M13;
			FrustumPlane[FAR].B = m.M24 - m.M23;
			FrustumPlane[FAR].C = m.M34 - m.M33;
			FrustumPlane[FAR].D = m.M44 - m.M43;
			FrustumPlane[FAR].Normalize();

		}

		public bool isPointInsideFrustum (Vector3 pt)
		{
			//  NOTE: For a point to be inside the frustrum, it must be inside
            //  all six frustum planes. 

			for (int i = 0; i < NUM_PLANES; i++) {	
				float distance = FrustumPlane[i].DistanceToPoint (pt);
				if (distance < 0.0) {
					// Console.WriteLine("point {0} is {1} units outside frustrum plane {2}:{3}", pt, distance, PlaneNames[i], FrustumPlane[i]);
					return false;
				} 
			}
			return true;
		}

		public bool isSphereInsideFrustum (Vector3 pt, float radius)
		{
			
			for (int i = 0; i < NUM_PLANES; i++) {	
				float dist = FrustumPlane [i].DistanceToPoint (pt);

				if (-dist > radius) {
					return false;
				}
			}
			return true;
		}

		public bool isSphereInsideFrustum (SSSphere sphere)
		{
			return isSphereInsideFrustum (sphere.center, sphere.radius);
		}

	}
}


