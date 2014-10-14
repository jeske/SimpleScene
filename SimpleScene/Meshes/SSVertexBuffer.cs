using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public interface ISSVertexLayout {
		int sizeOf();
		void bindGLAttributes(SSShaderProgram shader);
	}

	// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

	public class  SSVertexBuffer<VB> where VB : struct, ISSVertexLayout {
		private VB[] m_vb;
		private int m_VBOid;
        private BufferUsageHint m_usageHint;

		public unsafe SSVertexBuffer (VB[] vertexBufferArray,
                                     BufferUsageHint hint = BufferUsageHint.StaticDraw) {
			m_vb = vertexBufferArray;
            m_usageHint = hint;
			m_VBOid = GL.GenBuffer ();

			try {
                UpdateBufferData();
			} finally {
				GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
			}
		}

        void UpdateBufferData()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_VBOid);
            GL.BufferData(BufferTarget.ArrayBuffer,
               (IntPtr)(m_vb.Length * m_vb[0].sizeOf()),
               m_vb,
               m_usageHint);
        }

		public void Delete() {
			GL.DeleteBuffer (m_VBOid);
			m_VBOid = 0;
		}

		public void bind(SSShaderProgram shaderPgm) {
			if (shaderPgm != null) {
				GL.UseProgram (shaderPgm.ProgramID);
			} else {
				GL.UseProgram (0);
			}
			GL.BindBuffer (BufferTarget.ArrayBuffer, m_VBOid);
			m_vb [0].bindGLAttributes (shaderPgm);
		}
		public void unbind() {
			GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
			//GL.DisableClientState (EnableCap.VertexArray);
			//GL.DisableClientState (EnableCap.NormalArray);
			//GL.DisableClientState (EnableCap.TextureCoordArray);
		}

	}
}

