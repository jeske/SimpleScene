using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSSkeletalRenderMesh : SSIndexedMesh<SSVertex_PosNormTex>
	{
		protected readonly SSSkeletalMesh m_skeletalMesh;
		protected readonly SSVertex_PosNormTex[] m_vertices;
		protected Dictionary<int, SSSkeletalAnimationChannel> m_animChannels
			= new Dictionary<int, SSSkeletalAnimationChannel>();

		public SSSkeletalRenderMesh (SSSkeletalMesh skeletalMesh)
			: base(null, skeletalMesh.Indices)
		{
			m_skeletalMesh = skeletalMesh;
			m_vertices = new SSVertex_PosNormTex[skeletalMesh.NumVertices];
			for (int v = 0; v < skeletalMesh.NumVertices; ++v) {
				m_vertices [v].TexCoord = skeletalMesh.TextureCoords (v);
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

			m_skeletalMesh.ApplyAnimationChannels (channels);
			computeVertices ();

			base.RenderMesh (ref renderConfig);
			//renderNormals ();
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
	}
}

