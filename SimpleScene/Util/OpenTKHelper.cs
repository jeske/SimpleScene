using System;

using OpenTK;

namespace SimpleScene
{
	public static class OpenTKHelper
	{

	    // MouseToWorldRay
	    //
	    // This takes the view matricies, and a window-local mouse coordinate, and returns a ray in world space.

		public static SSRay MouseToWorldRay(
			Matrix4 projection, 
			Matrix4 view, 
			System.Drawing.Size viewport, 
			Vector2 mouse) 
		{
		    Vector3 pos1 = UnProject(ref projection, view, viewport, new Vector3(mouse.X,mouse.Y,0.0f));
			Vector3 pos2 = UnProject(ref projection, view, viewport, new Vector3(mouse.X,mouse.Y,0.8f));
			return SSRay.FromTwoPoints(pos1,pos2);
		}

		// UnProject takes a window-local mouse-coordinate, and a Z-coordinate depth [0,1] and 
		// unprojects it, returning the point in world space. To get a ray, UnProject the
		// mouse coordinates at two different z-values.
		//
		// http://www.opentk.com/node/1276#comment-13029

		public static Vector3 UnProject(
			ref Matrix4 projection, 
			Matrix4 view, 
			System.Drawing.Size viewport, 
			Vector3 mouse) 
		{
			Vector4 vec;
		 
			vec.X = 2.0f * mouse.X / (float)viewport.Width - 1;
			vec.Y = -(2.0f * mouse.Y / (float)viewport.Height - 1);
			vec.Z = mouse.Z;
			vec.W = 1.0f;
		 
			Matrix4 viewInv = Matrix4.Invert(view);
			Matrix4 projInv = Matrix4.Invert(projection);
		 
			Vector4.Transform(ref vec, ref projInv, out vec);
			Vector4.Transform(ref vec, ref viewInv, out vec);
		 
			if (vec.W > float.Epsilon || vec.W < float.Epsilon)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}
		 
			return new Vector3(vec.X,vec.Y,vec.Z);
		}

        
        /// <summary>
        /// Distance from a ray to a point at the closest spot. The ray is assumed to be infinite length.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="point"></param>
        /// <returns></returns>
		public static float DistanceToLine(SSRay ray, Vector3 point) {

            // http://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line

		    Vector3 a = ray.pos;
		    Vector3 n = ray.dir;
		    Vector3 p = point;

		    return ((a-p) - Vector3.Dot((a-p),n) * n).Length;
        }

#if false
        public static float DistanceToLine_2(SSRay ray, Vector3 point) {
            return Vector3.Cross(ray.dir, point - ray.pos).Length;
        }

		// http://www.geometrictools.com/Documentation/DistancePointLine.pdf
		public static float DistanceToLine_3(SSRay ray, Vector3 point) {
		    float t0 = Vector3.Dot(ray.dir, ( point - ray.pos) ) / Vector3.Dot(ray.dir,ray.dir);
            float distance = (point - (ray.pos + (t0 * ray.dir))).Length;
            return distance;
		}
#endif

		public static UInt16[] generateLineIndicies(UInt16[] indicies) {
			int line_count = indicies.Length / 3;
			UInt16[] line_indicies = new UInt16[line_count * 6];
			int v = 0;
			for (int i = 2; i < indicies.Length; i += 3) {
				var v1i = indicies [i - 2];
				var v2i = indicies [i - 1];
				var v3i = indicies [i];

				line_indicies [v++] = v1i;
				line_indicies [v++] = v2i;
				line_indicies [v++] = v1i;
				line_indicies [v++] = v3i;
				line_indicies [v++] = v2i;
				line_indicies [v++] = v3i;
			}
			return line_indicies;
		}

        /// <summary>
        /// Computes the distance between two quaternions.
        /// </summary>
        /// <param name="q1"></param>
        /// <param name="q2"></param>
        /// <returns>distance between quaternions in radians</returns>
        public static float RadialDistanceTo(this Quaternion q1, Quaternion q2) {
            // http://math.stackexchange.com/questions/90081/quaternion-distance

            double inner_product = q1.X * q2.X + q1.Y * q2.Y + q1.Z * q2.Z + q1.W * q2.W;
            return (float)Math.Acos(2.0 * Math.Pow(inner_product,2.0) - 1);
        }
	}
}

