// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;
using System.Collections.Generic;

using OpenTK;

namespace SimpleScene
{
    public struct SSAABB : IEquatable<SSAABB> {
        public Vector3 Min;
        public Vector3 Max;

        public SSAABB(float min = float.PositiveInfinity, float max = float.NegativeInfinity) {
            Min = new Vector3(min);
            Max = new Vector3(max);
        }

		public SSAABB(Vector3 min, Vector3 max)
		{
			Min = min;
			Max = max;
		}

        public void Combine(ref SSAABB other) {
            Min = Vector3.ComponentMin(Min, other.Min);
            Max = Vector3.ComponentMax(Max, other.Max);
        }

        public bool IntersectsSphere(Vector3 origin, float radius) {
            if ( 
                (origin.X + radius < Min.X) ||
                (origin.Y + radius < Min.Y) ||
                (origin.Z + radius < Min.Z) ||
                (origin.X - radius > Max.X) ||
                (origin.Y - radius > Max.Y) ||
                (origin.Z - radius > Max.Z)
               ) {
               return false;
            } else {
               return true;
            }
        }

		public bool IntersectsSphere(SSSphere sphere)
		{
			return IntersectsSphere (sphere.center, sphere.radius);
		}

        public bool IntersectsAABB(SSAABB box) {
            return ( (Max.X > box.Min.X) && (Min.X < box.Max.X) &&
                     (Max.Y > box.Min.Y) && (Min.Y < box.Max.Y) &&
                     (Max.Z > box.Min.Z) && (Min.Z < box.Max.Z));
        }

        public bool Equals(SSAABB other) {
            return 
                (Min.X == other.Min.X) &&
                (Min.Y == other.Min.Y) &&
                (Min.Z == other.Min.Z) &&
                (Max.X == other.Max.X) &&
                (Max.Y == other.Max.Y) &&
                (Max.Z == other.Max.Z);
        }

        public void UpdateMin(Vector3 localMin) {
            Min = Vector3.ComponentMin(Min, localMin);
        }

        public void UpdateMax(Vector3 localMax) {
            Max = Vector3.ComponentMax(Max, localMax);
        }

        public Vector3 Center() {
            return (Min + Max) / 2f;
        }

        public Vector3 Diff() {
            return Max - Min;
        }

		public SSSphere ToSphere()
		{
			float r = (Diff ().LengthFast + 0.001f)/2f;
			return new SSSphere (Center (), r);
		}

        internal void ExpandToFit(SSAABB b) {
            if (b.Min.X < this.Min.X) { this.Min.X = b.Min.X; }            
            if (b.Min.Y < this.Min.Y) { this.Min.Y = b.Min.Y; }            
            if (b.Min.Z < this.Min.Z) { this.Min.Z = b.Min.Z; }
            
            if (b.Max.X > this.Max.X) { this.Max.X = b.Max.X; }
            if (b.Max.Y > this.Max.Y) { this.Max.Y = b.Max.Y; }
            if (b.Max.Z > this.Max.Z) { this.Max.Z = b.Max.Z; }                        
        }

        public SSAABB ExpandedBy(SSAABB b) {
            SSAABB newbox = this;
            if (b.Min.X < newbox.Min.X) { newbox.Min.X = b.Min.X; }            
            if (b.Min.Y < newbox.Min.Y) { newbox.Min.Y = b.Min.Y; }            
            if (b.Min.Z < newbox.Min.Z) { newbox.Min.Z = b.Min.Z; }
            
            if (b.Max.X > newbox.Max.X) { newbox.Max.X = b.Max.X; }
            if (b.Max.Y > newbox.Max.Y) { newbox.Max.Y = b.Max.Y; }
            if (b.Max.Z > newbox.Max.Z) { newbox.Max.Z = b.Max.Z; }   

            return newbox;
        }

		public void ExpandBy(SSAABB b) 
		{
			this = this.ExpandedBy (b);
		}

		public static SSAABB FromSphere(Vector3 pos, float radius) {
            SSAABB box;
            box.Min.X = pos.X - radius;
            box.Max.X = pos.X + radius;
            box.Min.Y = pos.Y - radius;
            box.Max.Y = pos.Y + radius;
            box.Min.Z = pos.Z - radius;
            box.Max.Z = pos.Z + radius;

            return box;
        }

        private static readonly Vector4[] c_homogenousCorners = {
            new Vector4(-1f, -1f, -1f, 1f),
            new Vector4(-1f, 1f, -1f, 1f),
            new Vector4(1f, 1f, -1f, 1f),
            new Vector4(1f, -1f, -1f, 1f),

            new Vector4(-1f, -1f, 1f, 1f),
            new Vector4(-1f, 1f, 1f, 1f),
            new Vector4(1f, 1f, 1f, 1f),
            new Vector4(1f, -1f, 1f, 1f),
        };

        public static SSAABB FromFrustum(ref Matrix4 axisTransform, ref Matrix4 modelViewProj) {
            SSAABB ret = new SSAABB(float.PositiveInfinity, float.NegativeInfinity);
            Matrix4 inverse = modelViewProj;
            inverse.Invert();
            for (int i = 0; i < c_homogenousCorners.Length; ++i) {
                Vector4 corner = Vector4.Transform(c_homogenousCorners [i], inverse);
                Vector3 transfPt = Vector3.Transform(corner.Xyz / corner.W, axisTransform);
                ret.UpdateMin(transfPt);
                ret.UpdateMax(transfPt);
            }
            return ret;
        }

    }
}
