using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalMeshRuntime
	{
		internal SSSkeletalVertexRuntime[] m_vertices = null;
        internal SSSkeletalWeight[] m_weights = null;
		internal UInt16[] m_triangleIndices = null;
        internal SSSkeletalJointRuntime[] m_joints = null;

		#region runtime only use
		protected readonly List<int> m_topLevelJoints = new List<int> ();
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

		public int[] TopLevelJoints {
			get { return m_topLevelJoints.ToArray (); }
		}

		public SSSkeletalJointRuntime[] Joints {
			get { return m_joints; }
		}

		public SSSkeletalMeshRuntime (SSSkeletalMesh mesh, SSSkeletalJointRuntime[] sharedJoints = null)
		{
			if (sharedJoints == null) {
				m_joints = new SSSkeletalJointRuntime[mesh.Joints.Length];
				for (int j = 0; j < mesh.Joints.Length; ++j) {
					m_joints [j] = new SSSkeletalJointRuntime (mesh.Joints [j]);
					int parentIdx = mesh.Joints [j].ParentIndex;
					if (parentIdx != -1) {
						m_joints [parentIdx].Children.Add (j);
					} else {
						m_topLevelJoints.Add (j);
					}
				}
			} else {
				m_joints = sharedJoints;
			}
			m_vertices = new SSSkeletalVertexRuntime[mesh.Vertices.Length];
			for (int v = 0; v < mesh.Vertices.Length; ++v) {
				m_vertices [v] = new SSSkeletalVertexRuntime (mesh.Vertices [v]);
			}

			m_weights = mesh.Weights;
			m_triangleIndices = mesh.TriangleIndices;
			preComputeNormals ();
		}

		public int JointIndex(string jointName)
		{
			if (String.Compare (jointName, "all", true) == 0) {
				return -1;
			}

			for (int j = 0; j < m_joints.Length; ++j) {
				if (m_joints [j].BaseInfo.Name == jointName) {
					return j;
				}
			}
			string errMsg = string.Format ("Joint not found: \"{0}\"", jointName);
			System.Console.WriteLine (errMsg);
			throw new Exception (errMsg);
		}

		public SSSkeletalJointLocation JointLocation(int jointIdx) 
		{
			return m_joints [jointIdx].CurrentLocation;
		}

        public Vector3 ComputeVertexPosFromTriIndex(int triangleVertexIndex) {
            int vertexIndex = m_triangleIndices[triangleVertexIndex];
            return ComputeVertexPos(vertexIndex);
        }

		public Vector3 ComputeVertexPos(int vertexIndex)
		{
			Vector3 currentPos = Vector3.Zero;
			SSSkeletalVertex vertex = m_vertices [vertexIndex].BaseInfo;

			for (int w = 0; w < vertex.WeightCount; ++w) {
				SSSkeletalWeight weight = m_weights [vertex.WeightStartIndex + w];
				SSSkeletalJointRuntime joint = m_joints [weight.JointIndex];

				Vector3 currWeightPos = Vector3.Transform (weight.Position, joint.CurrentLocation.Orientation); 
				currentPos += weight.Bias * (joint.CurrentLocation.Position + currWeightPos);
			}
			return currentPos;
		}

		public Vector3 ComputeVertexNormal(int vertexIndex)
		{            
            SSSkeletalVertex vertex = m_vertices[vertexIndex].BaseInfo;
            Vector3 currentPos = Vector3.Zero;
            Vector3 currentNormalEndpoint = Vector3.Zero;

            for (int w = 0; w < vertex.WeightCount; ++w) {
                SSSkeletalWeight weight = m_weights[vertex.WeightStartIndex + w];
                SSSkeletalJointRuntime joint = m_joints[weight.JointIndex];

                Vector3 currWeightPos = Vector3.Transform(weight.Position, joint.CurrentLocation.Orientation);
                currentPos += weight.Bias * (joint.CurrentLocation.Position + currWeightPos);

                Vector3 currWeightNormalEndpointPos = Vector3.Transform(weight.Position + weight.JointLocalNormal, joint.CurrentLocation.Orientation);
                currentNormalEndpoint += weight.Bias * (joint.CurrentLocation.Position + currWeightNormalEndpointPos);
            }

            return (currentNormalEndpoint - currentPos).Normalized();
		}        

        public Vector3 BindPoseNormal(int vertexIndex) {
            return m_vertices[vertexIndex].BindPoseNormal;
        }

		public Vector2 TextureCoords(int vertexIndex)
		{
			return m_vertices [vertexIndex].BaseInfo.TextureCoords;
		}

		public void VerifyAnimation(SSSkeletalAnimation animation)
		{
			if (this.NumJoints != animation.NumJoints) {
				string str = string.Format (
		             "Joint number mismatch: {0} in md5mesh, {1} in md5anim",
		             this.NumJoints, animation.NumJoints);
				Console.WriteLine (str);
				throw new Exception (str);
			}
			for (int j = 0; j < NumJoints; ++j) {
				SSSkeletalJoint meshInfo = this.m_joints [j].BaseInfo;
				SSSkeletalJoint animInfo = animation.JointHierarchy [j];
				if (meshInfo.Name != animInfo.Name) {
					string str = string.Format (
						"Joint name mismatch: {0} in md5mesh, {1} in md5anim",
						meshInfo.Name, animInfo.Name);
					Console.WriteLine (str);
					throw new Exception (str);
				}
				if (meshInfo.ParentIndex != animInfo.ParentIndex) {
					string str = string.Format (
						"Hierarchy parent mismatch for joint \"{0}\": {1} in md5mesh, {2} in md5anim",
						meshInfo.Name, meshInfo.ParentIndex, animInfo.ParentIndex);
					Console.WriteLine (str);
					throw new Exception (str);
				}
			}
		}

		public void LoadAnimationFrame(SSSkeletalAnimation anim, float t)
		{
			for (int j = 0; j < NumJoints; ++j) {
				m_joints [j].CurrentLocation = anim.ComputeJointFrame (j, t);
			}
		}

		public void ApplyAnimationChannels(List<SSSkeletalAnimationChannel> channels)
		{
			foreach (int j in m_topLevelJoints) {
				traverseWithChannels (j, channels, null, null);
			}
		}

		private void traverseWithChannels(int j, 
									      List<SSSkeletalAnimationChannel> channels,
										  SSSkeletalAnimationChannel activeChannel,
										  SSSkeletalAnimationChannel prevActiveChannel)
		{
			foreach (var channel in channels) {
				if (channel.IsActive && channel.TopLevelActiveJoints.Contains (j)) {
					if (activeChannel != null && !channel.IsEnding) {
						prevActiveChannel = activeChannel;
					}
					activeChannel = channel;
				}
			}
			SSSkeletalJointRuntime joint = m_joints [j];

			if (activeChannel == null) {
				joint.CurrentLocation = joint.BaseInfo.BaseLocation;
			} else {
				SSSkeletalJointLocation activeLoc = activeChannel.ComputeJointFrame (j);
				int parentIdx = joint.BaseInfo.ParentIndex;
				if (parentIdx != -1) {
					activeLoc.ApplyParentTransform (m_joints [parentIdx].CurrentLocation);
				}

				if (activeChannel.IsEnding) {
					// TODO smarter, multi layer fallback
					SSSkeletalJointLocation fallbackLoc;
					if (prevActiveChannel == null || prevActiveChannel.IsEnding) {
						fallbackLoc = joint.BaseInfo.BaseLocation;
					} else {
						fallbackLoc = prevActiveChannel.ComputeJointFrame (j);
						if (joint.BaseInfo.ParentIndex != -1) {
							fallbackLoc.ApplyParentTransform (m_joints [parentIdx].CurrentLocation);
						}
					}
					joint.CurrentLocation = SSSkeletalJointLocation.Interpolate (
						activeLoc, fallbackLoc, activeChannel.FadeBlendPosition);
				} else {
					joint.CurrentLocation = activeLoc;
				}
			}

			foreach (int child in joint.Children) {
				traverseWithChannels (child, channels, activeChannel, prevActiveChannel);
			}
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
					SSSkeletalWeight weight = m_weights [vertBaseInfo.WeightStartIndex + w];
					SSSkeletalJointRuntime joint = m_joints [weight.JointIndex];

                    // write the joint local normal
                    m_weights[vertBaseInfo.WeightStartIndex + w].JointLocalNormal =                     
                        Vector3.Transform(normal, joint.BaseInfo.BaseLocation.Orientation.Inverted());

                    Console.WriteLine("Joint-Weight local normal: {0}", weight.JointLocalNormal);                       
				}

			}
		}
	}

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

	public class SSSkeletalJointRuntime
	{
		public SSSkeletalJointLocation CurrentLocation;
		public List<int> Children = new List<int>();

		protected SSSkeletalJoint m_baseInfo;

		public SSSkeletalJoint BaseInfo {
			get { return m_baseInfo; }
		}

		public SSSkeletalJointRuntime(SSSkeletalJoint baseInfo)
		{
			m_baseInfo = baseInfo;
			CurrentLocation = m_baseInfo.BaseLocation;
		}
	}
}

