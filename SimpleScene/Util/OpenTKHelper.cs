// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;
using System.Drawing; // for RectangleF

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public static class OpenTKHelper
	{
		/// <summary>
		/// Randomizer to be used for debugging purposes only.
		/// </summary>
		public static readonly Random s_debugRandom = new Random();

	    // MouseToWorldRay
	    //
	    // This takes the view matricies, and a window-local mouse coordinate, and returns a ray in world space.

		public static SSRay MouseToWorldRay(
			Matrix4 projection, 
			Matrix4 view, 
			System.Drawing.Size viewport, 
			Vector2 mouse) 
		{
			// these mouse.Z values are NOT scientific. 
			// Near plane needs to be < -1.5f or we have trouble selecting objects right in front of the camera. (why?)
		    Vector3 pos1 = UnProject(ref projection, view, viewport, new Vector3(mouse.X,mouse.Y,-1.5f)); // near
			Vector3 pos2 = UnProject(ref projection, view, viewport, new Vector3(mouse.X,mouse.Y,1.0f));  // far
			return SSRay.FromTwoPoints(pos1, pos2);
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
		 
			if (vec.W > float.Epsilon || vec.W < -float.Epsilon)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}
		 
			return new Vector3(vec.X,vec.Y,vec.Z);
		}

        /// <summary>
        /// Converts quaternion representation to Euler angles
        /// http://math.stackexchange.com/questions/687964/getting-euler-tait-bryan-angles-from-quaternion-representation
        /// </summary>
		public static Vector3 QuaternionToEuler(ref Quaternion q)
        {
			float phi = (float)Math.Atan2(q.Z*q.W + q.X*q.Y, 0.5f - q.Y*q.Y - q.Z*q.Z);
			float theta = (float)Math.Asin(2f * (q.X*q.Z - q.Y*q.W));
			float gamma = (float)Math.Atan2(q.Y*q.Z + q.X*q.W, 0.5f - q.Z * q.Z - q.W * q.W);
			//return new Vector3 (phi, theta, gamma);
			return new Vector3 (gamma - (float)Math.PI, theta, phi);
        }

		public static void neededRotationAxisAngle(Vector3 dir1, Vector3 dir2, out Vector3 axis, out float angle, 
			bool normalizeInput = true)
        {
			if (normalizeInput) {
				dir1.Normalize ();
				dir2.Normalize ();
			}
            Vector3 cross = Vector3.Cross(dir1, dir2);
			float crossLength = cross.Length;
			if (crossLength <= float.Epsilon) {
				// 180 deg rotation special case
				Vector3 dummyAxis;
				TwoPerpAxes (dir1, out axis, out dummyAxis);
				angle = (float)Math.PI;
			} else {
				axis = cross / crossLength;
				float dot = Vector3.Dot (dir1, dir2);
				if (dot >= 1f) {
					// do this because Acos function below returns NaN when input is 1
					angle = 0f;
				} else {
					angle = (float)Math.Acos (dot);
				}
			}

			#if false
			if (normalize) {
			dir1.Normalize();
			dir2.Normalize();
			}
			Vector3 crossNormalized = Vector3.Cross(dir1, dir2).Normalized();
			if (float.IsNaN(crossNormalized.X)) {
			// this means dir1 is parallel to dir2. we can do it less cryptically but why add processing?
			if (dir1 == -dir2) {
			Vector3 perpAxis1, perpAxis2;
			TwoPerpAxes(dir1, out perpAxis1, out perpAxis2);
			return Quaternion.FromAxisAngle(perpAxis1, (float)Math.PI);
			} else {
			return Quaternion.Identity;
			}
			}

			float dot = Vector3.Dot(dir1, dir2);
			float angle = (float)Math.Acos(dot);
			return Quaternion.FromAxisAngle(crossNormalized, angle);
			#endif
        }

		/// <summary>
		/// Quaterionon needed to transform direction dir1 into direction dir2
		/// </summary>
		public static Quaternion neededRotationQuat(Vector3 dir1, Vector3 dir2, bool normalize = true)
		{
			Vector3 axis;
			float angle;
			neededRotationAxisAngle (dir1, dir2, out axis, out angle, normalize);
			return Quaternion.FromAxisAngle (axis, angle);
		}

		public static Matrix4 neededRotationMat(Vector3 dir1, Vector3 dir2, bool normalize = true)
		{
			Vector3 axis;
			float angle;
			neededRotationAxisAngle (dir1, dir2, out axis, out angle, normalize);
			return Matrix4.CreateFromAxisAngle (axis, angle);
		}
			       
        /// <summary>
        /// Distance from a ray to a point at the closest spot. The ray is assumed to be infinite length.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="point"></param>
        /// <returns></returns>
		public static float DistanceToLine(SSRay ray, Vector3 point, out float distanceAlongRay) {

            // http://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line

		    Vector3 a = ray.pos;
		    Vector3 n = ray.dir;
		    Vector3 p = point;

            var t = Vector3.Dot((a-p),n);
            distanceAlongRay = -t;
		    return ((a-p) - t * n).Length;
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

		// http://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
		public static bool TriangleRayIntersectionTest(
            ref Vector3 V1, ref Vector3 V2, ref Vector3 V3, ref Vector3 rayStart, ref Vector3 rayDir, out float contact) {
			Vector3 e1, e2;  //Edge1, Edge2
            Vector3 P, Q, T;
            float det, inv_det, u, v;
            float t;

			contact = 0.0f;
 
            //Find vectors for two edges sharing V1
			e1 = V2 - V1;
			e2 = V3 - V1;
            //Begin calculating determinant - also used to calculate u parameter
			P = Vector3.Cross(rayDir, e2);
            //if determinant is near zero, ray lies in plane of triangle
            det = Vector3.Dot(e1,P);
			if (det < 0.0f) return false; // backfaced triangle
			if (det > -float.Epsilon && det < float.Epsilon) return false; // triangle parallel to ray
            inv_det = 1.0f / det;
 
            //calculate distance from V1 to ray origin
			T = rayStart - V1;
 
            //Calculate u parameter and test bound
            u = Vector3.Dot(T,P) * inv_det;
            //The intersection lies outside of the triangle
            if(u < 0.0f || u > 1.0f) return false;
 
            //Prepare to test v parameter
			Q = Vector3.Cross(T,e1);
 
            //Calculate V parameter and test bound
			v = Vector3.Dot(rayDir,Q) * inv_det;
            //The intersection lies outside of the triangle
            if(v < 0.0f || u + v  > 1.0f) return false;
 
			t = Vector3.Dot(e2,Q) * inv_det;
  
			if(t > float.Epsilon) { //ray intersection
                contact = t;
                return true;
            }
 
            // No hit, no win
            return false;
		} // fn


        public static bool intersectRayAABox1(SSRay ray, SSAABB box, ref float tnear, ref float tfar) {
            // r.dir is unit direction vector of ray
            Vector3 dirfrac = new Vector3();
            float t;
            dirfrac.X = 1.0f / ray.dir.X;
            dirfrac.Y = 1.0f / ray.dir.Y;
            dirfrac.Z = 1.0f / ray.dir.Z;
            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            float t1 = (box.Min.X - ray.pos.X)*dirfrac.X;
            float t2 = (box.Max.X - ray.pos.X)*dirfrac.X;
            float t3 = (box.Min.Y - ray.pos.Y)*dirfrac.Y;
            float t4 = (box.Max.Y - ray.pos.Y)*dirfrac.Y;
            float t5 = (box.Min.Z - ray.pos.Z)*dirfrac.Z;
            float t6 = (box.Max.Z - ray.pos.Z)*dirfrac.Z;

            float tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            float tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            // if tmax < 0, ray (line) is intersecting AABB, but whole AABB is behing us
            if (tmax < 0)
            {
                t = tmax;
                return false;
            }

            // if tmin > tmax, ray doesn't intersect AABB
            if (tmin > tmax)
            {
                t = tmax;
                return false;
            }

            t = tmin;
            return true;

        }

        // Ray to AABB (AxisAlignedBoundingBox)
        // http://gamedev.stackexchange.com/questions/18436/most-efficient-aabb-vs-ray-collision-algorithms
       
        public static bool intersectRayAABox2(SSRay ray, SSAABB box, ref float tnear, ref float tfar) {
            Vector3d T_1 = new Vector3d();
            Vector3d T_2 = new Vector3d(); // vectors to hold the T-values for every direction
            double t_near = double.MinValue; // maximums defined in float.h
            double t_far = double.MaxValue;

            for (int i = 0; i < 3; i++){ //we test slabs in every direction
                if (ray.dir[i] == 0){ // ray parallel to planes in this direction
                    if ((ray.pos[i] < box.Min[i]) || (ray.pos[i] > box.Max[i])) {
                        return false; // parallel AND outside box : no intersection possible
                    }
                } else { // ray not parallel to planes in this direction
                    T_1[i] = (box.Min[i] - ray.pos[i]) / ray.dir[i];
                    T_2[i] = (box.Max[i] - ray.pos[i]) / ray.dir[i];

                    if(T_1[i] > T_2[i]){ // we want T_1 to hold values for intersection with near plane
                        var swp = T_2; // swap
                        T_1 = swp; T_2 = T_1;   
                    }
                    if (T_1[i] > t_near){
                        t_near = T_1[i];
                    }
                    if (T_2[i] < t_far){
                        t_far = T_2[i];
                    }
                    if( (t_near > t_far) || (t_far < 0) ){
                        return false;
                    }
                }
            }
            tnear = (float)t_near; tfar = (float)t_far; // put return values in place
            return true; // if we made it here, there was an intersection - YAY
        }

        public static float DegreeToRadian(this float angleInDegrees) {
			return (float)Math.PI * angleInDegrees / 180.0f;
		}

        public static float RadianToDegree(this float angleInRadians) {
			return 180f * (angleInRadians / (float)Math.PI);            
		}

		public static float Clamp(float value, float min, float max) {
			value = (value < min ? min : value);
			value = (value > max ? max : value);
			return value;
		}

        public static void TwoPerpAxes(Vector3 zAxis, 
            out Vector3 xAxis, 
            out Vector3 yAxis,
            float delta = 0.01f) 
        {
            // pick two perpendicular axes to an axis
            zAxis.Normalize();
            if (Math.Abs(zAxis.X) < delta
                && Math.Abs(zAxis.Y) < delta) { // special case
                xAxis = Vector3.UnitX;
            } else {
                xAxis = new Vector3(zAxis.Y, -zAxis.X, 0.0f).Normalized();
            }
            yAxis = Vector3.Cross(zAxis, xAxis);
        }

        public static Vector3 randomDirection()
        {
            double theta = 2.0 * Math.PI * s_debugRandom.NextDouble();
            double phi = Math.PI * s_debugRandom.NextDouble() - Math.PI/2.0;
            float xy = (float)(Math.Cos(phi));
            return new Vector3(
                xy * (float)Math.Cos(theta),
                xy * (float)Math.Sin(theta),
                (float)Math.Sin(phi));
        }

		/// <summary>
		/// https://bitbucket.org/sinbad/ogre/src/9db75e3ba05c/OgreMain/include/OgreVector3.h#cl-651
		/// </summary>
		public static Quaternion getRotationTo(Vector3 src, Vector3 dst, Vector3 fallbackAxis)
		{
			// Based on Stan Melax's article in Game Programming Gems
			Quaternion q = new Quaternion();
			// Copy, since cannot modify local
			Vector3 v0 = src.Normalized();
			Vector3 v1 = dst.Normalized();

			float d = Vector3.Dot (v0, v1);
			// If dot == 1, vectors are the same
			if (d >= 1.0f)
			{
				return Quaternion.Identity;
			}
			if (d < (1e-6f - 1.0f))
			{
				if (fallbackAxis != Vector3.Zero)
				{
					// rotate 180 degrees about the fallback axis
					q = Quaternion.FromAxisAngle (fallbackAxis, (float)Math.PI);
				}
				else
				{
					// Generate an axis
					Vector3 axis = Vector3.Cross(Vector3.UnitX, v0);
					if (axis.Length <= 0.001f) // pick another if colinear
						axis = Vector3.Cross(Vector3.UnitY, v0);
					axis.Normalize();
					q = Quaternion.FromAxisAngle (axis, (float)Math.PI);
				}
			}
			else
			{
				float s = (float)Math.Sqrt( (1f+d)*2f );
				float invs = 1f / s;

				Vector3 c = Vector3.Cross (v0, v1);

				q.X = c.X * invs;
				q.Y = c.Y * invs;
				q.Z = c.Z * invs;
				q.W = s * 0.5f;
				q.Normalize();
			}
			return q;
		}

        public static Vector3 ProjectCoord(Vector3 pt, 
            Vector3 dirX, Vector3 dirY, Vector3 dirZ) 
        {
            // projects a point onto 3 axes
            // (assumes dir vectors are unit length)
            Vector3 ret;
            ret.X = Vector3.Dot(pt, dirX);
            ret.Y = Vector3.Dot(pt, dirY);
            ret.Z = Vector3.Dot(pt, dirZ);
            return ret;
        }

        public static bool RectsOverlap(Vector2 r1Min, Vector2 r1Max,
                                        Vector2 r2Min, Vector2 r2Max) 
        {
            // return true when two rectangles overlap in 2D
            return !(r1Max.X < r2Min.X || r2Max.X < r1Min.X
                     || r1Max.Y < r2Min.Y || r2Max.Y < r1Min.Y);
        }

        /// <summary>
        /// Strip away the rotation in the view matrix to make an object always face the camera
        /// http://stackoverflow.com/questions/5467007/inverting-rotation-in-3d-to-make-an-object-always-face-the-camera/5487981#5487981
        /// </summary>
        public static Matrix4 BillboardMatrix(ref Matrix4 modelViewMat)
        {
            Vector3 trans = modelViewMat.ExtractTranslation();
            Vector3 scale = modelViewMat.ExtractScale();
            return new Matrix4 (
                scale.X, 0f, 0f, 0f,
                0f, scale.Y, 0f, 0f,
                0f, 0f, scale.Z, 0f,
                trans.X, trans.Y, trans.Z, 1f);
        }

        public static Vector2 WorldToScreen(Vector3 worldPos, 
            ref Matrix4 viewProjMat, ref RectangleF clientRect)
        {
            Vector4 pos = Vector4.Transform(new Vector4(worldPos, 1f), viewProjMat);
            pos /= pos.W;
            pos.Y = -pos.Y;
            Vector2 screenSize = new Vector2(clientRect.Width, clientRect.Height);
            Vector2 screenCenter = new Vector2 (clientRect.X, clientRect.Y) + screenSize / 2f;
            return screenCenter + pos.Xy * screenSize / 2f;
        }

        /// <summary>
        /// View matrix that makes the object appear on the screen in pixel dimentions matching it's 
        /// scale value (after modelview and projection transforms)
        /// </summary>
        public static Matrix4 ScaleToScreenPxViewMat(Vector3 objPos, float objScaleX,
                                                     ref Matrix4 viewMat, ref Matrix4 projMat)
        {
			float factor = scaleMitigationFactor(objPos, objScaleX, ref viewMat, ref projMat);
            return Matrix4.CreateScale(factor);
            //return Matrix4.Identity;
        }

		public static float scaleMitigationFactor(Vector3 objPos, float objScaleX,
												   ref Matrix4 viewMat, ref Matrix4 projMat)
		{
			objScaleX = Math.Abs(objScaleX);

			// compute rightmost point in world coordinates
			Matrix4 viewRotInverted = Matrix4.CreateFromQuaternion(viewMat.ExtractRotation().Inverted());
			Vector3 viewRight = Vector3.Transform(Vector3.UnitX, viewRotInverted).Normalized();
			Vector3 rightMostInWorld = objPos + objScaleX * viewRight;

			// compute things in screen coordinates and find the required scale mitigation
			Matrix4 modelViewProjMat = viewMat * projMat;
			RectangleF clientRect = GetClientRect();
			Vector2 centerOnScreen = WorldToScreen(objPos, ref modelViewProjMat, ref clientRect);
			Vector2 rightMostOnScreen = WorldToScreen(rightMostInWorld, ref modelViewProjMat, ref clientRect);
			float distanceObserved = Math.Abs(rightMostOnScreen.X - centerOnScreen.X);
			float scaleMitigation = objScaleX / distanceObserved;
			//System.Console.WriteLine("rightmost x = " + rightMostOnScreen.X);
			return scaleMitigation;
			//return Matrix4.Identity;
		}

        public static RectangleF GetClientRectF()
        {
            Rectangle rect = GetClientRect();
            return new RectangleF (rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public static Rectangle GetClientRect()
        {
            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            return new Rectangle (viewport [0], viewport [1], viewport [2], viewport [3]);
        }

		public static bool areFramebuffersSupported() {
			string version_string = GL.GetString(StringName.Version);
			// TODO improve in time for OpenGL 10.0 backends
			int major = version_string [0] - '0';
			int minor = version_string [2] - '0';
			Version version = new Version(major, minor); // todo: improve
			if (version < new Version(3, 0)) {
				var str = GL.GetString(StringName.Extensions).ToLower();
				if (!str.Contains ("framebuffer_object")) {
					Console.WriteLine ("framebuffers not supported by the GL version ");
					return false;
				}
			} 
			return true;
		}
	}
}

