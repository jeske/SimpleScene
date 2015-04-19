using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalMeshMD5
	{
		#region from MD5 mesh
		protected string m_materialShaderString;

		protected SkeletalVertexMD5[] m_vertices = null;
		protected SkeletalWeightMD5[] m_weights = null;
		protected UInt16[] m_triangleIndices = null;
		protected SkeletalJoint[] m_joints = null;
		#endregion

		#region runtime only use
		protected Vector3[] m_baseNormals = null;
		#endregion

		public int NumJoints {
			get { return m_joints.Length; }
		}

		public int NumVertices {
			get { return m_vertices.Length; }
		}

		public int NumIndices {
			get { return m_triangleIndices.Length; }
		}

		public int NumTriangles {
			get { return m_triangleIndices.Length / 3; }
		}

		public ushort[] Indices {
			get { return m_triangleIndices; }
		}

		public static SSSkeletalMeshMD5[] ReadMeshes(SSAssetManager.Context ctx, string filename)
		{
			var parser = new SSMD5Parser(ctx, filename);
			Match[] matches;
			parser.seekEntry ("MD5Version", "10");
			parser.seekEntry ("commandline", SSMD5Parser.c_nameRegex);

			matches = parser.seekEntry ("numJoints", SSMD5Parser.c_uintRegex);
			var joints = new SkeletalJointMD5[Convert.ToUInt32(matches[1].Value)];

			matches = parser.seekEntry ( "numMeshes", SSMD5Parser.c_uintRegex);
			SSSkeletalMeshMD5[] meshes = new SSSkeletalMeshMD5[Convert.ToUInt32 (matches [1].Value)];

			parser.seekEntry ("joints", "{");
			for (int j = 0; j < joints.Length; ++j) {
				joints[j] = new SkeletalJointMD5(parser);
			}
			parser.seekEntry ("}");

			for (int m = 0; m < meshes.Length; ++m) {
				meshes [m] = new SSSkeletalMeshMD5 (joints, parser);
			}
			return meshes;
		}

		public SSSkeletalMeshMD5 (SkeletalJointMD5[] joints, SSMD5Parser parser)
		{
			parser.seekEntry("mesh", "{");

			Match[] matches;
			matches = parser.seekEntry("shader", SSMD5Parser.c_nameRegex);
			m_materialShaderString = matches[1].Value;

			matches = parser.seekEntry ("numverts", SSMD5Parser.c_uintRegex);
			m_vertices = new SkeletalVertexMD5[Convert.ToUInt32(matches[1].Value)];

			for (int v = 0; v < m_vertices.Length; ++v) {
				int vertexIndex;
				var vertex = new SkeletalVertexMD5 (parser, out vertexIndex);
				m_vertices [vertexIndex] = vertex;
			}

			matches = parser.seekEntry ("numtris", SSMD5Parser.c_uintRegex);
			int numTris = Convert.ToUInt16 (matches [1].Value);
			m_triangleIndices = new UInt16[numTris * 3];
			for (int t = 0; t < numTris; ++t) {
				readTriangle (parser);
			}

			matches = parser.seekEntry ("numweights", SSMD5Parser.c_uintRegex);
			int numWeights = Convert.ToInt32 (matches [1].Value);
			m_weights = new SkeletalWeightMD5[numWeights];
			for (int w = 0; w < m_weights.Length; ++w) {
				int weightIdx;
				SkeletalWeightMD5 weight = new SkeletalWeightMD5 (parser,  out weightIdx);
				m_weights [weightIdx] = weight;
			}

			m_joints = new SkeletalJoint[joints.Length];
			for (int j = 0; j < joints.Length; ++j) {
				m_joints[j] = new SkeletalJoint (joints [j]);
			}

			preComputeNormals ();
		}

		public Vector3 ComputeVertexPos(int vertexIndex)
		{
			Vector3 currentPos = Vector3.Zero;
			SkeletalVertexMD5 vertex = m_vertices [vertexIndex];

			for (int w = 0; w < vertex.WeightCount; ++w) {
				SkeletalWeightMD5 weight = m_weights [vertex.WeightStartIndex + w];
				SkeletalJoint joint = m_joints [weight.JointIndex];

				Vector3 currWeightPos = Vector3.Transform (weight.Position, joint.CurrentLocation.Orientation); 
				currentPos += weight.Bias * (joint.CurrentLocation.Position + currWeightPos);
			}
			return currentPos;
		}

		public Vector3 ComputeVertexNormal(int vertexIndex)
		{
			SkeletalVertexMD5 vertex = m_vertices [vertexIndex];
			Vector3 currentNormal = Vector3.Zero;

			for (int w = 0; w < vertex.WeightCount; ++w) {
				SkeletalWeightMD5 weight = m_weights [vertex.WeightStartIndex + w];
				SkeletalJoint joint = m_joints [weight.JointIndex];
				currentNormal += weight.Bias * Vector3.Transform (vertex.Normal, joint.CurrentLocation.Orientation);
			}
			return currentNormal;
		}

		public Vector2 TextureCoords(int vertexIndex)
		{
			return m_vertices [vertexIndex].TextureCoords;
		}

		public void VerifyAnimation(SSSkeletalAnimationMD5 animation)
		{
			if (this.NumJoints != animation.NumJoints) {
				string str = string.Format (
		             "Joint number mismatch: {0} in md5mesh, {1} in md5anim",
		             this.NumJoints, animation.NumJoints);
				Console.WriteLine (str);
				throw new Exception (str);
			}
			for (int j = 0; j < NumJoints; ++j) {
				string jointMeshName = m_joints [j].Md5.Name;
				string jointAnimName = animation.JointHierarchy [j].Name;
				if (jointMeshName != jointAnimName) {
					string str = string.Format (
						"Joint name mismatch: {0} in md5mesh, {1} in md5anim",
						jointMeshName, jointAnimName);
					Console.WriteLine (str);
					throw new Exception (str);
				}
			}
		}

		public void LoadAnimationFrame(SSSkeletalAnimationMD5 anim, float t)
		{
			for (int j = 0; j < NumJoints; ++j) {
				m_joints [j].CurrentLocation = anim.ComputeJointFrame (j, t);
			}
		}

		private void readTriangle(SSMD5Parser parser)
		{
			Match[] matches;
			matches = parser.seekEntry (
				"tri",
				SSMD5Parser.c_uintRegex, // triangle index
				SSMD5Parser.c_uintRegex, SSMD5Parser.c_uintRegex, SSMD5Parser.c_uintRegex // 3 vertex indices
			);
			UInt32 triIdx = Convert.ToUInt32 (matches [1].Value);
			UInt32 indexBaseIdx = triIdx * 3;
			m_triangleIndices [indexBaseIdx] = Convert.ToUInt16 (matches [2].Value);
			m_triangleIndices [indexBaseIdx+2] = Convert.ToUInt16 (matches [3].Value);
			m_triangleIndices [indexBaseIdx+1] = Convert.ToUInt16 (matches [4].Value);
		}

		/// <summary>
		/// Precompute normals in joint-local space
		/// http://3dgep.com/loading-and-animating-md5-models-with-opengl/#The_MD5Mesh::PrepareNormals_Method
		/// </summary>
		private void preComputeNormals()
		{
			// step 1: walk each triangle, and add the normal contribution to it's verticies.
			for (int i = 0; i < NumTriangles; ++i) {
				int baseIdx = i * 3;
				int v0 = m_triangleIndices [baseIdx];
				int v1 = m_triangleIndices [baseIdx + 2];
				int v2 = m_triangleIndices [baseIdx + 1];
				Vector3 p0 = ComputeVertexPos (v0);
				Vector3 p1 = ComputeVertexPos (v1);
				Vector3 p2 = ComputeVertexPos (v2);
				Vector3 normal = Vector3.Cross (p2 - p0, p1 - p0);
				m_vertices [v0].Normal += normal;
				m_vertices [v1].Normal += normal;
				m_vertices [v2].Normal += normal;
			}

			// step 2: walk each vertex, normalize the normal, and convert into joint-local space
			for (int v = 0; v < m_vertices.Length; ++v) {
				// Normalize
				Vector3 normal = m_vertices [v].Normal.Normalized ();

				// Reset for the next step
				m_vertices [v].Normal = Vector3.Zero;

				// Put the bind-pose normal into joint-local space
				// so the animated normal can be computed faster later
				for (int w = 0; w < m_vertices [v].WeightCount; ++w) {
					SkeletalWeightMD5 weight = m_weights [m_vertices [v].WeightStartIndex + w];
					SkeletalJoint joint = m_joints [weight.JointIndex];
					m_vertices [v].Normal 
					+= Vector3.Transform (normal, joint.Md5.BaseLocation.Orientation.Inverted()) * weight.Bias;
				}

				// normalize the joint-local normal
				m_vertices [v].Normal = m_vertices [v].Normal.Normalized ();
			}
		}

		protected struct SkeletalVertexMD5
		{
			#region from MD5 mesh
			//public int VertexIndex;
			public Vector2 TextureCoords;
			public Vector3 Normal;
			public int WeightStartIndex;
			public int WeightCount;
			#endregion

			public SkeletalVertexMD5(SSMD5Parser parser, out int vertexIndex)
			{
				Match[] matches
				= parser.seekEntry(
					"vert",
					SSMD5Parser.c_uintRegex, // vertex index
					SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose, // texture coord
					SSMD5Parser.c_uintRegex, // weight start index
					SSMD5Parser.c_uintRegex // weight count
				);
				vertexIndex = Convert.ToInt32(matches[1].Value);
				TextureCoords.X = (float)Convert.ToDouble(matches[3].Value);
				TextureCoords.Y = (float)Convert.ToDouble(matches[4].Value);
				WeightStartIndex = Convert.ToInt32(matches[6].Value);
				WeightCount = Convert.ToInt32(matches[7].Value);
				Normal = Vector3.Zero;
			}
		}

		protected struct SkeletalWeightMD5
		{
			#region from MD5 mesh
			//public int WeightIndex;
			public int JointIndex;
			public float Bias;
			public Vector3 Position;
			#endregion

			public SkeletalWeightMD5(SSMD5Parser parser, out int weightIndex)
			{
				Match[] matches
				= parser.seekEntry(
					"weight",
					SSMD5Parser.c_uintRegex, // weight index
					SSMD5Parser.c_uintRegex, // joint index
					SSMD5Parser.c_floatRegex, // bias
					SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose // position
				);

				weightIndex = Convert.ToInt32(matches[1].Value);
				JointIndex = Convert.ToInt32(matches[2].Value);
				Bias = (float)Convert.ToDouble(matches[3].Value);
				Position.X = (float)Convert.ToDouble(matches[5].Value);
				Position.Y = (float)Convert.ToDouble(matches[6].Value);
				Position.Z = (float)Convert.ToDouble(matches[7].Value);
			}
		}

		public class SkeletalJointMD5
		{
			#region from MD5 mesh
			public string Name;
			public int ParentIndex;
			public SSSkeletalJointLocation BaseLocation;
			#endregion

			public SkeletalJointMD5 (SSMD5Parser parser)
			{
				Match[] matches = parser.seekEntry (
					SSMD5Parser.c_nameRegex, // joint name
					SSMD5Parser.c_intRegex,  // parent index
					SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose, // position
					SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose  // orientation			
				);
				Name = matches[0].Captures[0].Value;
				ParentIndex = Convert.ToInt32(matches[1].Value);

				BaseLocation.Position.X = (float)Convert.ToDouble(matches[3].Value);
				BaseLocation.Position.Y = (float)Convert.ToDouble(matches[4].Value); 
				BaseLocation.Position.Z = (float)Convert.ToDouble(matches[5].Value);

				BaseLocation.Orientation.X = (float)Convert.ToDouble(matches[8].Value);
				BaseLocation.Orientation.Y = (float)Convert.ToDouble(matches[9].Value); 
				BaseLocation.Orientation.Z = (float)Convert.ToDouble(matches[10].Value);
				BaseLocation.ComputeQuatW();
			}
		}

		public class SkeletalJoint
		{
			public SSSkeletalJointLocation CurrentLocation;
			protected SkeletalJointMD5 m_md5;

			public SkeletalJointMD5 Md5 {
				get { return m_md5; }
			}

			public SkeletalJoint(SkeletalJointMD5 md5)
			{
				m_md5 = md5;
				ComputeLocationFromMd5();
			}

			public void ComputeLocationFromMd5()
			{
				CurrentLocation = m_md5.BaseLocation;
			}
		}
	}
}

