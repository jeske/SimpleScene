using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSSkeletalRenderMesh : SSIndexedMesh<SSVertex_PosNormTex>
	{
		protected readonly SSSkeletalMeshMD5 m_md5;
		protected readonly SSVertex_PosNormTex[] m_vertices;

		protected SSSkeletalAnimationMD5 m_prevAnim = null;
		protected SSSkeletalAnimationMD5 m_anim = null;

		public SSSkeletalRenderMesh (SSSkeletalMeshMD5 md5)
			: base(null, md5.Indices)
		{
			m_md5 = md5;
			m_vertices = new SSVertex_PosNormTex[md5.NumVertices];
			for (int v = 0; v < md5.NumVertices; ++v) {
				m_vertices [v].TexCoord = md5.TextureCoords (v);
			}
			ComputeVertices ();
		}

		public void ComputeVertices()
		{
			for (int v = 0; v < m_md5.NumVertices; ++v) {
				m_vertices [v].Position = m_md5.ComputeVertexPos (v);
				m_vertices [v].Normal = m_md5.ComputeVertexNormal (v);
			}
			m_vbo.UpdateBufferData (m_vertices);
		}

		/// <summary>
		/// Transition into an animation
		/// </summary>
		/// <param name="anim">Animation to transition to.</param>
		/// <param name="animationTransitionTime">Time to transition from the current animation into the target animation</param>
		/// <param name="repeatInterpolationTime">Time for to transition from the end of the animation to the beginning. Zero value means no looping</param>
		public void LoadAnimation (SSSkeletalAnimationMD5 anim, 
 			                      float animationTransitionTime = 0f,
  			                      float repeatInterpolationTime = 0f)
		{
			m_anim = anim;
			m_md5.LoadAnimationFrame (anim, 1f);
			ComputeVertices ();
		}

		public override void RenderMesh(ref SSRenderConfig renderConfig)
		{
			base.RenderMesh (ref renderConfig);
			//renderNormals ();
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

