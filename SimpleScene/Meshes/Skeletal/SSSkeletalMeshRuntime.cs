using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenTK;

namespace SimpleScene
{
	public struct SSSkeletalVertexRuntime
	{
		public Vector3 BindPoseNormal;        
		private SSSkeletalVertex m_baseInfo;

		public SSSkeletalVertex BaseInfo {
			get { return m_baseInfo; }
		}

		public SSSkeletalVertexRuntime(SSSkeletalVertex vertex)
		{
			m_baseInfo = vertex;
			BindPoseNormal = Vector3.Zero;            
		}
	}

	public struct SSSkeletalWeightRuntime
	{
		public SSSkeletalWeight BaseInfo;
		public Vector3 JointLocalNormal;

		public SSSkeletalWeightRuntime(SSSkeletalWeight weight)
		{
			BaseInfo = weight;
			JointLocalNormal = Vector3.Zero;
		}
	}

	public class SSSkeletalMeshRuntime
	{
		protected SSSkeletalHierarchyRuntime m_hierarchy = null;
		protected SSSkeletalVertexRuntime[] m_vertices = null;
		protected SSSkeletalWeightRuntime[] m_weights = null;
		protected UInt16[] m_triangleIndices = null;

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

		public SSSkeletalMeshRuntime (SSSkeletalMesh mesh, SSSkeletalHierarchyRuntime hierarchy)
		{
			m_hierarchy = hierarchy;

			m_vertices = new SSSkeletalVertexRuntime[mesh.Vertices.Length];
			for (int v = 0; v < mesh.Vertices.Length; ++v) {
				m_vertices [v] = new SSSkeletalVertexRuntime (mesh.Vertices [v]);
			}

			m_weights = new SSSkeletalWeightRuntime[mesh.Weights.Length];
			for (int w = 0; w < mesh.Weights.Length; ++w) {
				m_weights [w] = new SSSkeletalWeightRuntime (mesh.Weights [w]);
			}
			m_triangleIndices = mesh.TriangleIndices;
			preComputeNormals ();
		}

        public Vector3 ComputeVertexPosFromTriIndex(int triangleVertexIndex) {
            int vertexIndex = m_triangleIndices[triangleVertexIndex];
            return ComputeVertexPos(vertexIndex);
        }

		/// <summary>
		/// Computes a vertex position based on the state of runtime joint hierarchy
		/// </summary>
		public Vector3 ComputeVertexPos(int vertexIndex)
		{
			Vector3 currentPos = Vector3.Zero;
			SSSkeletalVertex vertex = m_vertices [vertexIndex].BaseInfo;

			for (int w = 0; w < vertex.WeightCount; ++w) {
				var weight = m_weights [vertex.WeightStartIndex + w];
				var joint = m_hierarchy.joints[weight.BaseInfo.JointIndex];

				Vector3 currWeightPos = Vector3.Transform (weight.BaseInfo.Position, joint.CurrentLocation.Orientation); 
				currentPos += weight.BaseInfo.Bias * (joint.CurrentLocation.Position + currWeightPos);
			}
			return currentPos;
		}

		/// <summary>
		/// Computes a vertex normal based on the state of runtime joint hierarchy
		/// </summary>
		public Vector3 ComputeVertexNormal(int vertexIndex)
		{            
            SSSkeletalVertex vertex = m_vertices[vertexIndex].BaseInfo;
            Vector3 currentPos = Vector3.Zero;
            Vector3 currentNormalEndpoint = Vector3.Zero;

            for (int w = 0; w < vertex.WeightCount; ++w) {
                var weight = m_weights[vertex.WeightStartIndex + w];
				var joint = m_hierarchy.joints[weight.BaseInfo.JointIndex];

				Vector3 currWeightPos = Vector3.Transform(weight.BaseInfo.Position, joint.CurrentLocation.Orientation);
				currentPos += weight.BaseInfo.Bias * (joint.CurrentLocation.Position + currWeightPos);

				Vector3 currWeightNormalEndpointPos = Vector3.Transform(weight.BaseInfo.Position + weight.JointLocalNormal, joint.CurrentLocation.Orientation);
				currentNormalEndpoint += weight.BaseInfo.Bias * (joint.CurrentLocation.Position + currWeightNormalEndpointPos);
            }

            return (currentNormalEndpoint - currentPos).Normalized();
		}

		/// <summary>
		/// Retrieve bind pose normal for a vertex
		/// </summary>
        public Vector3 BindPoseNormal(int vertexIndex) 
		{
            return m_vertices[vertexIndex].BindPoseNormal;
        }

		/// <summary>
		/// Retrieve texture coordinates for a vertex
		/// </summary>
		public Vector2 TextureCoords(int vertexIndex)
		{
			return m_vertices [vertexIndex].BaseInfo.TextureCoords;
		}

		/// <summary>
		/// Precompute normals in joint-local space
		/// http://3dgep.com/loading-and-animating-md5-models-with-opengl/#The_MD5Mesh::PrepareNormals_Method
		/// </summary>
		private void preComputeNormals()
		{
            // step 0: initialize per-vertex normals to zero..
            for (int v = 0; v < m_vertices.Length; ++v) {
                m_vertices[v].BindPoseNormal = Vector3.Zero;
            }

			// step 1: walk each triangle, and add the triangle normal contribution to it's verticies.
			for (int i = 0; i < NumTriangles; ++i) {
				int baseIdx = i * 3;
				int v0 = m_triangleIndices [baseIdx];
				int v1 = m_triangleIndices [baseIdx + 1];
				int v2 = m_triangleIndices [baseIdx + 2];
				Vector3 p0 = ComputeVertexPos (v0);
				Vector3 p1 = ComputeVertexPos (v1);
				Vector3 p2 = ComputeVertexPos (v2);
				Vector3 triNormal = Vector3.Cross (p1 - p0, p2 - p0);
				m_vertices [v0].BindPoseNormal += triNormal;
				m_vertices [v1].BindPoseNormal += triNormal;
				m_vertices [v2].BindPoseNormal += triNormal;
			}

			// step 2: walk each vertex, normalize the normal, and convert into joint-local space
			for (int v = 0; v < m_vertices.Length; ++v) {
				// Normalize
				Vector3 normal = m_vertices [v].BindPoseNormal.Normalized();				
				m_vertices [v].BindPoseNormal = normal;
                Console.WriteLine("normal = {0}",normal);

				// Put the bind-pose normal into joint-local space
				// so the animated normal can be computed faster later
				var vertBaseInfo = m_vertices [v].BaseInfo;
                for (int w = 0; w < vertBaseInfo.WeightCount; ++w) {
					var weight = m_weights [vertBaseInfo.WeightStartIndex + w];
					var joint = m_hierarchy.joints [weight.BaseInfo.JointIndex];

                    // write the joint local normal
                    m_weights[vertBaseInfo.WeightStartIndex + w].JointLocalNormal =                     
                        Vector3.Transform(normal, joint.BaseInfo.BindPoseLocation.Orientation.Inverted());

                    Console.WriteLine("Joint-Weight local normal: {0}", weight.JointLocalNormal);                       
				}

			}
		}
	}
}

