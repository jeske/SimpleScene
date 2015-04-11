using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalJoint
	{
		#region from MD5
		public int ParentIndex;
		public string Name;
		public Vector3 Position;
		public Quaternion Orientation;
		#endregion

		#region runtime
		SSSkeletalJoint Parent;
		SSSkeletalWeight[] Weights;
		#endregion
	}

	public struct SSSkeletalVertex
	{
		int VertexIndex;
		Vector2 TextureCoords;
	}

	public struct SSSkeletalWeight
	{
		int WeightIndex;
		int JointIndex;
		float Bias;
	}

	public class SSSkeletalMesh
	{
		protected SSSkeletalJoint[] m_joints = null;
		protected SSSkeletalVertex[] m_vertices = null;
		protected int[] m_triangleIndices = null;

		protected int NumJoints {
			get { return m_joints.Length; }
		}

		protected int NumVertices {
			get { return m_vertices.Length; }
		}

		protected int NumTriangles {
			get { return m_triangleIndices.Length / 3; }
		}

		public SSSkeletalMesh ()
		{
		}
	}
}

