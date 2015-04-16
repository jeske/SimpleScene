using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalIIndexedMesh : SSIndexedMesh<SSVertex_PosNormTex>
	{
		protected readonly SSSkeletalMeshMD5 m_md5;
		protected readonly SSVertex_PosNormTex[] m_vertices;

		public SSSkeletalIIndexedMesh (SSSkeletalMeshMD5 md5)
			: base(null, md5.Indices)
		{
			m_md5 = md5;
			m_vertices = new SSVertex_PosNormTex[md5.NumVertices];
			for (int v = 0; v < md5.NumVertices; ++v) {
				m_vertices [v].TexCoord = md5.TextureCoords (v);
			}
			ComputeVertices ();

			ambientTexture = md5.MainTexture;
		}

		public void ComputeVertices()
		{
			for (int v = 0; v < m_md5.NumVertices; ++v) {
				m_vertices [v].Position = m_md5.ComputeVertexPos (v);
				m_vertices [v].Normal = m_md5.ComputeVertexNormal (v);
			}
			m_vbo.UpdateBufferData (m_vertices);
		}
	}
}

