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

    }
}
