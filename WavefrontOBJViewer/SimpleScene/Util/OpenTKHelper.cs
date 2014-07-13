using System;

using OpenTK;

namespace WavefrontOBJViewer
{
	public static class OpenTKHelper
	{

	    // MouseToWorldRay
	    //
	    // This takes the view matricies, and a window-local mouse coordinate, and returns a ray in world space.

		public static SSRay MouseToWorldRay(
			ref Matrix4 projection, 
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

		public static float DistanceToLine_2(SSRay ray, Vector3 point) {
            return Vector3.Cross(ray.dir, point - ray.pos).Length;
		}

		// http://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
		public static float DistanceToLine(SSRay ray, Vector3 point) {
		    Vector3 a = ray.pos;
		    Vector3 n = ray.dir;
		    Vector3 p = point;

		    return ((a-p) - Vector3.Dot((a-p),n) * n).Length;
        }

		// http://www.geometrictools.com/Documentation/DistancePointLine.pdf
		public static float DistanceToLine3(SSRay ray, Vector3 point) {
		    float t0 = Vector3.Dot(ray.dir, ( point - ray.pos) ) / Vector3.Dot(ray.dir,ray.dir);
            float distance = (point - (ray.pos + (t0 * ray.dir))).Length;
            return distance;
		}
	}
}

