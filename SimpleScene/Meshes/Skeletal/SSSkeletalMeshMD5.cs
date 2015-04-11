using System;
using OpenTK;

namespace SimpleScene
{
	public struct SkeletalVertexMD5
	{
		#region from MD5 mesh
		public int VertexIndex;
		public Vector2 TextureCoords;
		public int WeightStartIndex;
		public int WeightCount;
		#endregion
	}

	public struct SkeletalWeightMD5
	{
		#region from MD5 mesh
		public int WeightIndex;
		public int JointIndex;
		public float Bias;
		public Vector3 Position;
		#endregion
	}

	public struct SkeletalJointMD5
	{
		#region from MD5 mesh
		public int ParentIndex;
		public string Name;
		public Vector3 BasePosition;
		public Vector3 BaseOrientation;
		#endregion
	}

	public struct SkeletalJointLocation
	{
		public Vector3 Position;
		public Quaternion Orientation;
	}

	public class SkeletalJoint
	{
		public SkeletalJointMD5 Md5;
		public SkeletalJointLocation Current;

		public SkeletalJointLocation[] Frames = null;
	}

	public class SSSkeletalMeshMD5
	{
		public static float ComputeQuatW(ref Vector3 quatXyz)
		{
			float t = 1f - quatXyz.X * quatXyz.X - quatXyz.Y * quatXyz.Y - quatXyz.Z * quatXyz.Z;
			return t < 0f ? 0f : -(float)Math.Sqrt(t);
		}

		#region from MD5 mesh
		protected string MaterialShaderString;

		protected SkeletalVertexMD5[] m_vertices = null;
		protected SkeletalWeightMD5[] m_weights = null;
		protected UInt16[] m_triangleIndices = null;
		protected SkeletalJoint[] m_joints = null;
		#endregion

		#region runtime only use
		protected Vector3[] m_baseNormals = null;
		#endregion

		protected int NumJoints {
			get { return m_joints.Length; }
		}

		protected int NumVertices {
			get { return m_vertices.Length; }
		}

		protected int NumIndices {
			get { return m_triangleIndices.Length; }
		}

		protected int NumTriangles {
			get { return m_triangleIndices.Length / 3; }
		}

		public ushort[] Indices {
			get { return m_triangleIndices; }
		}

		public void ComputeJointPosFromMD5Mesh(bool computeNormals=true)
		{
			for (int j = 0; j < NumJoints; ++j) {
				m_joints [j].Current.Position = m_joints [j].Md5.BasePosition;

				Vector3 orient = m_joints [j].Md5.BaseOrientation;
				m_joints [j].Current.Orientation.Xyz = orient;
				m_joints [j].Current.Orientation.W = ComputeQuatW (ref orient);
			}

			if (computeNormals) {
				// TODO compute normals?

			}
		}

		public Vector3 ComputeVertexPos(int vertexIndex)
		{
			Vector3 currentPos = Vector3.Zero;
			SkeletalVertexMD5 vertex = m_vertices [vertexIndex];

			for (int w = 0; w < vertex.WeightCount; ++w) {
				SkeletalWeightMD5 weight = m_weights [vertex.WeightStartIndex + w];
				SkeletalJoint joint = m_joints [weight.JointIndex];

				Vector3 currWeightPos = Vector3.Transform (weight.Position, joint.Current.Orientation); 
				currentPos += weight.Bias * (joint.Current.Position + currWeightPos);
			}
			return currentPos;
		}

		public SSSkeletalMeshMD5 (SSAssetManager.Context ctx, string filename)
		{

		}

	}
}

