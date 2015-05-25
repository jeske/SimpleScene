using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalMesh
	{
		public SSSkeletalVertex[] Vertices = null;
		public SSSkeletalWeight[] Weights = null;
		public UInt16[] TriangleIndices = null;
		public SSSkeletalJoint[] Joints = null;
		public string MaterialShaderString = null;

		public SSAssetManager.Context AssetContext = null;
	}

	public struct SSSkeletalVertex
	{
		public Vector2 TextureCoords;
		public int WeightStartIndex;
		public int WeightCount;
	}

	public struct SSSkeletalWeight
	{
		public int JointIndex;
		public float Bias;
		public Vector3 Position;
	}

	public class SSSkeletalJoint
	{
		public string Name;
		public int JointIndex;
		public int ParentIndex;
		public SSSkeletalJointLocation BaseLocation;
	}

	public struct SSSkeletalJointLocation
	{
		public Vector3 Position;
		public Quaternion Orientation;

		public static SSSkeletalJointLocation Interpolate(
			SSSkeletalJointLocation left, 
			SSSkeletalJointLocation right, 
			float blend)
		{
			SSSkeletalJointLocation ret;
			ret.Position = Vector3.Lerp(left.Position, right.Position, blend);
			ret.Orientation = Quaternion.Slerp (left.Orientation, right.Orientation, blend);
			return ret;
		}

		public void ComputeQuatW()
		{
			float t = 1f - Orientation.X * Orientation.X 
				- Orientation.Y * Orientation.Y 
				- Orientation.Z * Orientation.Z;
			Orientation.W = t < 0f ? 0f : -(float)Math.Sqrt(t);
		}

		public void ApplyPrecedingTransform(SSSkeletalJointLocation parentLoc)
		{
			Position = parentLoc.Position 
				+ Vector3.Transform (Position, parentLoc.Orientation);
			Orientation = Quaternion.Multiply (parentLoc.Orientation, Orientation);
			Orientation.Normalize ();
		}

		public void UndoPrecedingTransform(SSSkeletalJointLocation parentLoc)
		{
			var parOrientInverse = parentLoc.Orientation.Inverted ();
			Orientation = Quaternion.Multiply (parOrientInverse, Orientation);
			Orientation.Normalize ();
			Position = Vector3.Transform (Position - parentLoc.Position, parOrientInverse);
		}
	}
}

