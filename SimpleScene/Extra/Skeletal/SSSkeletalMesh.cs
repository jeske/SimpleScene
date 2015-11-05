using System;
using OpenTK;

namespace SimpleScene.Demos
{
	public class SSSkeletalMesh
	{
		public SSSkeletalVertex[] vertices = null;
		public SSSkeletalWeight[] weights = null;
		public UInt16[] triangleIndices = null;
		public SSSkeletalJoint[] joints = null;
		public string materialShaderString = null;

		public SSAssetManager.Context assetContext = null;
	}

	public struct SSSkeletalVertex
	{
		public Vector2 textureCoords;
		public int weightStartIndex;
		public int weightCount;
	}

	public struct SSSkeletalWeight
	{
		public int jointIndex;
		public float bias;
		public Vector3 position;
	}

	public class SSSkeletalJoint
	{
		public string name;
		public int jointIndex;
		public int parentIndex;

		/// <summary>
		/// The bind pose location in global (mesh) coordinates.
		/// </summary>
		public SSSkeletalJointLocation bindPoseLocation;
	}

	public struct SSSkeletalJointLocation
	{
		public Vector3 position;
		public Quaternion orientation;

		public static SSSkeletalJointLocation identity {
			get {
				var ret = new SSSkeletalJointLocation();
				ret.position = Vector3.Zero;
				ret.orientation = Quaternion.Identity;
				return ret;
			}
		}

		public static SSSkeletalJointLocation interpolate(
			SSSkeletalJointLocation left, 
			SSSkeletalJointLocation right, 
			float blend)
		{
			SSSkeletalJointLocation ret;
			ret.position = Vector3.Lerp(left.position, right.position, blend);
			ret.orientation = Quaternion.Slerp (left.orientation, right.orientation, blend);
			return ret;
		}

		public void computeQuatW()
		{
			float t = 1f - orientation.X * orientation.X 
				- orientation.Y * orientation.Y 
				- orientation.Z * orientation.Z;
			orientation.W = t < 0f ? 0f : -(float)Math.Sqrt(t);
		}

		public void applyPrecedingTransform(SSSkeletalJointLocation parentLoc)
		{
			position = parentLoc.position 
				+ Vector3.Transform (position, parentLoc.orientation);
			orientation = Quaternion.Multiply (parentLoc.orientation, orientation);
			orientation.Normalize ();
		}

		public void undoPrecedingTransform(SSSkeletalJointLocation parentLoc)
		{
			var parOrientInverse = parentLoc.orientation.Inverted ();
			orientation = Quaternion.Multiply (parOrientInverse, orientation);
			orientation.Normalize ();
			position = Vector3.Transform (position - parentLoc.position, parOrientInverse);
		}

		public Vector3 applyTransformTo(Vector3 pos)
		{
			return position + Vector3.Transform (pos, orientation);
		}

		public Vector3 undoTransformTo(Vector3 pos)
		{
			return Vector3.Transform (pos - position, orientation.Inverted ());
		}
	}
}

