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

