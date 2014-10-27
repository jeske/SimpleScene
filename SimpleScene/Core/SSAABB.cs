// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;
using System.Collections.Generic;

using OpenTK;

namespace SimpleScene
{
    public struct SSAABB : IEquatable<SSAABB> {
        public Vector3 min,max;

        public bool intersectsSphere(Vector3 origin, float radius) {
            if ( 
                (origin.X + radius < min.X) ||
                (origin.Y + radius < min.Y) ||
                (origin.Z + radius < min.Z) ||
                (origin.X - radius > max.X) ||
                (origin.Y - radius > max.Y) ||
                (origin.Z - radius > max.Z)
               ) {
               return false;
            } else {
               return true;
            }
        }

        public bool intersectsAABB(SSAABB box) {
            return ( (max.X > box.min.X) && (min.X < box.max.X) &&
                     (max.Y > box.min.Y) && (min.Y < box.max.Y) &&
                     (max.Z > box.min.Z) && (min.Z < box.max.Z));
        }

        public bool Equals(SSAABB other) {
            return 
                (min.X == other.min.X) &&
                (min.Y == other.min.Y) &&
                (min.Z == other.min.Z) &&
                (max.X == other.max.X) &&
                (max.Y == other.max.Y) &&
                (max.Z == other.max.Z);
        }

        internal void expandToFit(SSAABB b) {
            if (b.min.X < this.min.X) { this.min.X = b.min.X; }            
            if (b.min.Y < this.min.Y) { this.min.Y = b.min.Y; }            
            if (b.min.Z < this.min.Z) { this.min.Z = b.min.Z; }
            
            if (b.max.X > this.max.X) { this.max.X = b.max.X; }
            if (b.max.Y > this.max.Y) { this.max.Y = b.max.Y; }
            if (b.max.Z > this.max.Z) { this.max.Z = b.max.Z; }                        
        }

        internal SSAABB expandedBy(SSAABB b) {
            SSAABB newbox = this;
            if (b.min.X < newbox.min.X) { newbox.min.X = b.min.X; }            
            if (b.min.Y < newbox.min.Y) { newbox.min.Y = b.min.Y; }            
            if (b.min.Z < newbox.min.Z) { newbox.min.Z = b.min.Z; }
            
            if (b.max.X > newbox.max.X) { newbox.max.X = b.max.X; }
            if (b.max.Y > newbox.max.Y) { newbox.max.Y = b.max.Y; }
            if (b.max.Z > newbox.max.Z) { newbox.max.Z = b.max.Z; }   

            return newbox;
        }

        public static SSAABB fromSphere(Vector3 pos, float radius) {
            SSAABB box;
            box.min.X = pos.X - radius;
            box.max.X = pos.X + radius;
            box.min.Y = pos.Y - radius;
            box.max.Y = pos.Y + radius;
            box.min.Z = pos.Z - radius;
            box.max.Z = pos.Z + radius;

            return box;
        }

    }
}
