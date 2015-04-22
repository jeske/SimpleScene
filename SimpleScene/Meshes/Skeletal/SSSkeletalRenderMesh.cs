using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSSkeletalRenderMesh : SSIndexedMesh<SSVertex_PosNormTex>
	{
		protected readonly SSSkeletalMeshRuntime m_skeletalMesh;
		protected readonly SSVertex_PosNormTex[] m_vertices;
		protected Dictionary<int, SSSkeletalAnimationChannel> m_animChannels
			= new Dictionary<int, SSSkeletalAnimationChannel>();
		protected SSSphere m_boundingSphere;

		public SSSkeletalRenderMesh (SSSkeletalMesh skeletalMesh)
			: base(null, skeletalMesh.TriangleIndices)
		{
			m_skeletalMesh = new SSSkeletalMeshRuntime(skeletalMesh);
			m_vertices = new SSVertex_PosNormTex[m_skeletalMesh.NumVertices];
			for (int v = 0; v < m_skeletalMesh.NumVertices; ++v) {
				m_vertices [v].TexCoord = m_skeletalMesh.TextureCoords (v);
			}
			computeVertices ();
		}

		public void AddChannel(int channelId, params int[] topLevelActiveJointIds)
		{
			foreach (int cid in topLevelActiveJointIds) {
				if (cid == -1) { // means include all
					topLevelActiveJointIds = m_skeletalMesh.TopLevelJoints;
					break;
				}
			}
			var channel = new SSSkeletalAnimationChannel (topLevelActiveJointIds);
			m_animChannels.Add (channelId, channel);
		}

		public void AddChannel(int channelId, params string[] topLevelActiveJointNames)
		{
			int[] topLevelActiveJointsIds = new int[topLevelActiveJointNames.Length];
			for (int n = 0; n < topLevelActiveJointsIds.Length; ++n) {
				topLevelActiveJointsIds [n] = m_skeletalMesh.JointIndex (topLevelActiveJointNames [n]);
			}
			AddChannel (channelId, topLevelActiveJointsIds);
		}

		public void PlayAnimation(int channelId, SSSkeletalAnimation anim,
								  bool repeat, float transitionTime)
		{
			m_skeletalMesh.VerifyAnimation (anim);
			var channel = m_animChannels [channelId];
			channel.PlayAnimation (anim, repeat, transitionTime);
		}

		public override void RenderMesh(ref SSRenderConfig renderConfig)
		{
			var channels = new List<SSSkeletalAnimationChannel> ();
			channels.AddRange (m_animChannels.Values);

			SSAABB aabb;
			m_skeletalMesh.ApplyAnimationChannels (channels, out aabb);
			m_boundingSphere = aabb.ToSphere ();
			MeshChanged ();

			computeVertices ();

			base.RenderMesh (ref renderConfig);
			//renderNormals ();

			#if false
			// bounding box debugging
			GL.Disable (EnableCap.Texture2D);
			GL.Translate (aabb.Center ());
			GL.Scale (aabb.Diff ());
			GL.Color4 (1f, 0f, 0f, 0.1f);
			SSTexturedCube.Instance.DrawArrays (ref renderConfig, PrimitiveType.Triangles);
			#endif
		}

		public override void Update(float elapsedS)
		{
			foreach (var channel in m_animChannels.Values) {
				channel.Update (elapsedS);
			}
		}

		private void computeVertices()
		{
			for (int v = 0; v < m_skeletalMesh.NumVertices; ++v) {
				m_vertices [v].Position = m_skeletalMesh.ComputeVertexPos (v);
				m_vertices [v].Normal = m_skeletalMesh.ComputeVertexNormal (v);
			}
			m_vbo.UpdateBufferData (m_vertices);
		}


		private void renderNormals()
		{
			SSShaderProgram.DeactivateAll ();
			GL.Color4 (Color4.Magenta);
			for (int v = 0; v < m_vertices.Length; ++v) {
				GL.Begin (PrimitiveType.Lines);
				GL.Vertex3 (m_vertices [v].Position);
				GL.Vertex3 (m_vertices [v].Position + m_vertices [v].Normal * 0.1f); 
				GL.End ();
			}
		}

		public override Vector3 Center ()
		{
			return m_boundingSphere.center;
		}

		public override float Radius ()
		{
			return m_boundingSphere.radius;
		}
	}
}

