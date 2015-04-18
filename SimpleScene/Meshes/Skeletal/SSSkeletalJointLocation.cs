using System;
using OpenTK;

namespace SimpleScene
{
	public struct SSSkeletalJointLocation
	{
		public Vector3 Position;
		public Quaternion Orientation;

		public void ComputeQuatW()
		{
			float t = 1f - Orientation.X * Orientation.X 
				- Orientation.Y * Orientation.Y 
				- Orientation.Z * Orientation.Z;
			Orientation.W = t < 0f ? 0f : -(float)Math.Sqrt(t);
		}
	}
}

