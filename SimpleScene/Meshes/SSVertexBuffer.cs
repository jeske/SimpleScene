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
			m_VBOid = GL.GenBuffer();
            UpdateBufferData();
		}

        public void Delete() {
            GL.DeleteBuffer(m_VBOid);
            m_VBOid = 0;
        }

        public void UpdateBufferData()
        {
            bind();
            GL.BufferData(BufferTarget.ArrayBuffer,
               (IntPtr)(m_vb.Length * m_vb[0].sizeOf()),
               m_vb,
               m_usageHint);
            unbind();
        }

        public void DrawArrays(PrimitiveType primType, 
                               SSShaderProgram shaderPgm = null) {
            bind(shaderPgm);
            GL.DrawArrays(primType, 0, m_vb.Length);
            unbind();
        }

		public void bind(SSShaderProgram shaderPgm = null) {
			if (shaderPgm != null) {
				GL.UseProgram (shaderPgm.ProgramID);
			} else {
				GL.UseProgram (0);
			}
			GL.BindBuffer (BufferTarget.ArrayBuffer, m_VBOid);
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
			m_vb [0].bindGLAttributes (shaderPgm);
		}
		public void unbind() {
			GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
            GL.UseProgram(0);
            GL.PopClientAttrib();
		}

	}
}

