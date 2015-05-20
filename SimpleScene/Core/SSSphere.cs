using System;
using OpenTK;

namespace SimpleScene
{
	public struct SSSphere : IEquatable<SSSphere>
	{
		public Vector3 center;
		public float radius;

		public SSSphere (Vector3 _center, float _radius)
		{
			center = _center;
			radius = _radius;
		}

		public bool Equals(SSSphere other)
		{
			return this.center == other.center
				&& this.radius == other.radius;
		}

		public bool IntersectsSphere(SSSphere other)
		{
			float addedR = this.radius + other.radius;
			float addedRSq = addedR * addedR;
			float distSq = (other.center - this.center).LengthSquared;
			return addedRSq >= distSq;
		}

		public bool IntersectsRay (ref SSRay worldSpaceRay, out float distanceAlongRay)
		{
			float distanceToSphereOrigin = OpenTKHelper.DistanceToLine(
				worldSpaceRay, this.center, out distanceAlongRay);
			return distanceToSphereOrigin <= this.radius;
		}

		public bool IntersectsAABB(SSAABB aabb)
		{
			return aabb.IntersectsSphere (this);
		}

		public SSAABB ToAABB()
		{
			Vector3 rvec = new Vector3 (radius);
			return new SSAABB (center - rvec, center + rvec);
		}
	}
}

