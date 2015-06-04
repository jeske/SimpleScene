using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenTK;

namespace SimpleScene
{
	public struct SSSkeletalVertexRuntime
	{
		public Vector3 bindPoseNormal;        
		private SSSkeletalVertex _baseInfo;

		public SSSkeletalVertex baseInfo {
			get { return _baseInfo; }
		}

		public SSSkeletalVertexRuntime(SSSkeletalVertex vertex)
		{
			_baseInfo = vertex;
			bindPoseNormal = Vector3.Zero;            
		}
	}

	public struct SSSkeletalWeightRuntime
	{
		public SSSkeletalWeight baseInfo;
		public Vector3 jointLocalNormal;

		public SSSkeletalWeightRuntime(SSSkeletalWeight weight)
		{
			baseInfo = weight;
			jointLocalNormal = Vector3.Zero;
		}
	}

	public class SSSkeletalMeshRuntime
	{
		protected SSSkeletalHierarchyRuntime _hierarchy = null;
		protected SSSkeletalVertexRuntime[] _vertices = null;
		protected SSSkeletalWeightRuntime[] _weights = null;
		protected UInt16[] _triangleIndices = null;

		public int numVertices {
			get { return _vertices.Length; }
		}

		public int numIndices {
			get { return _triangleIndices.Length; }
		}

		public int numTriangles {
			get { return _triangleIndices.Length / 3; }
		}

		public ushort[] indices {
			get { return _triangleIndices; }
		}

		public SSSkeletalMeshRuntime (SSSkeletalMesh mesh, SSSkeletalHierarchyRuntime hierarchy)
		{
			_hierarchy = hierarchy;

			_vertices = new SSSkeletalVertexRuntime[mesh.vertices.Length];
			for (int v = 0; v < mesh.vertices.Length; ++v) {
				_vertices [v] = new SSSkeletalVertexRuntime (mesh.vertices [v]);
			}

			_weights = new SSSkeletalWeightRuntime[mesh.weights.Length];
			for (int w = 0; w < mesh.weights.Length; ++w) {
				_weights [w] = new SSSkeletalWeightRuntime (mesh.weights [w]);
			}
			_triangleIndices = mesh.triangleIndices;
			_preComputeNormals ();
		}

        public Vector3 computeVertexPosFromTriIndex(int triangleVertexIndex) {
            int vertexIndex = _triangleIndices[triangleVertexIndex];
            return computeVertexPos(vertexIndex);
        }

		/// <summary>
		/// Computes a vertex position based on the state of runtime joint hierarchy
		/// </summary>
		public Vector3 computeVertexPos(int vertexIndex)
		{
			Vector3 currentPos = Vector3.Zero;
			SSSkeletalVertex vertex = _vertices [vertexIndex].baseInfo;

			for (int w = 0; w < vertex.weightCount; ++w) {
				var weight = _weights [vertex.weightStartIndex + w];
				var joint = _hierarchy.joints[weight.baseInfo.jointIndex];

				Vector3 currWeightPos = Vector3.Transform (weight.baseInfo.position, joint.currentLocation.orientation); 
				currentPos += weight.baseInfo.bias * (joint.currentLocation.position + currWeightPos);
			}
			return currentPos;
		}

		/// <summary>
		/// Computes a vertex normal based on the state of runtime joint hierarchy
		/// </summary>
		public Vector3 computeVertexNormal(int vertexIndex)
		{            
            SSSkeletalVertex vertex = _vertices[vertexIndex].baseInfo;
            Vector3 currentPos = Vector3.Zero;
            Vector3 currentNormalEndpoint = Vector3.Zero;

            for (int w = 0; w < vertex.weightCount; ++w) {
                var weight = _weights[vertex.weightStartIndex + w];
				var joint = _hierarchy.joints[weight.baseInfo.jointIndex];

				Vector3 currWeightPos = Vector3.Transform(weight.baseInfo.position, joint.currentLocation.orientation);
				currentPos += weight.baseInfo.bias * (joint.currentLocation.position + currWeightPos);

				Vector3 currWeightNormalEndpointPos = Vector3.Transform(weight.baseInfo.position + weight.jointLocalNormal, joint.currentLocation.orientation);
				currentNormalEndpoint += weight.baseInfo.bias * (joint.currentLocation.position + currWeightNormalEndpointPos);
            }

            return (currentNormalEndpoint - currentPos).Normalized();
		}

		/// <summary>
		/// Retrieve bind pose normal for a vertex
		/// </summary>
        public Vector3 bindPoseNormal(int vertexIndex) 
		{
            return _vertices[vertexIndex].bindPoseNormal;
        }

		/// <summary>
		/// Retrieve texture coordinates for a vertex
		/// </summary>
		public Vector2 textureCoords(int vertexIndex)
		{
			return _vertices [vertexIndex].baseInfo.textureCoords;
		}

		/// <summary>
		/// Precompute normals in joint-local space
		/// http://3dgep.com/loading-and-animating-md5-models-with-opengl/#The_MD5Mesh::PrepareNormals_Method
		/// </summary>
		private void _preComputeNormals()
		{
            // step 0: initialize per-vertex normals to zero..
            for (int v = 0; v < _vertices.Length; ++v) {
                _vertices[v].bindPoseNormal = Vector3.Zero;
            }

			// step 1: walk each triangle, and add the triangle normal contribution to it's verticies.
			for (int i = 0; i < numTriangles; ++i) {
				int baseIdx = i * 3;
				int v0 = _triangleIndices [baseIdx];
				int v1 = _triangleIndices [baseIdx + 1];
				int v2 = _triangleIndices [baseIdx + 2];
				Vector3 p0 = computeVertexPos (v0);
				Vector3 p1 = computeVertexPos (v1);
				Vector3 p2 = computeVertexPos (v2);
				Vector3 triNormal = Vector3.Cross (p1 - p0, p2 - p0);
				_vertices [v0].bindPoseNormal += triNormal;
				_vertices [v1].bindPoseNormal += triNormal;
				_vertices [v2].bindPoseNormal += triNormal;
			}

			// step 2: walk each vertex, normalize the normal, and convert into joint-local space
			for (int v = 0; v < _vertices.Length; ++v) {
				// Normalize
				Vector3 normal = _vertices [v].bindPoseNormal.Normalized();				
				_vertices [v].bindPoseNormal = normal;
                Console.WriteLine("normal = {0}",normal);

				// Put the bind-pose normal into joint-local space
				// so the animated normal can be computed faster later
				var vertBaseInfo = _vertices [v].baseInfo;
                for (int w = 0; w < vertBaseInfo.weightCount; ++w) {
					var weight = _weights [vertBaseInfo.weightStartIndex + w];
					var joint = _hierarchy.joints [weight.baseInfo.jointIndex];

                    // write the joint local normal
                    _weights[vertBaseInfo.weightStartIndex + w].jointLocalNormal =                     
                        Vector3.Transform(normal, joint.baseInfo.bindPoseLocation.orientation.Inverted());

                    Console.WriteLine("Joint-Weight local normal: {0}", weight.jointLocalNormal);                       
				}

			}
		}
	}
}

