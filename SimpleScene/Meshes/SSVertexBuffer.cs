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

    public interface SSIVertexBuffer
    {
        void drawBind(SSShaderProgram shaderPgm);
        void drawUnbind();
    }

    public class SSVertexBuffer<VB> : SSIVertexBuffer
        where VB : struct, ISSVertexLayout {
		private readonly VB[] m_vb;
        private readonly BufferUsageHint m_usageHint;
        private int m_VBOid = 0;

		public unsafe SSVertexBuffer (VB[] vertexBufferArray,
                                     BufferUsageHint hint = BufferUsageHint.StaticDraw) {
			m_vb = vertexBufferArray;
            m_usageHint = hint;
            UpdateBufferData();
		}

        public void Delete() {
            GL.DeleteBuffer(m_VBOid);
            m_VBOid = 0;
        }

        public void UpdateBufferData()
        {
            if (m_VBOid == 0) {
                m_VBOid = GL.GenBuffer();
            }
            bindPrivate();
            GL.BufferData(BufferTarget.ArrayBuffer,
               (IntPtr)(m_vb.Length * m_vb[0].sizeOf()),
               m_vb,
               m_usageHint);
            unbindPrivate();
        }

        public void DrawArrays(PrimitiveType primType, 
                               SSShaderProgram shaderPgm = null) {
            drawBind(shaderPgm);
            GL.DrawArrays(primType, 0, m_vb.Length);
            drawUnbind();
        }

        public void drawBind(SSShaderProgram shaderPgm = null) {
            // bind for use and setup for drawing
            bindPrivate();
            if (shaderPgm != null) {
                GL.UseProgram(shaderPgm.ProgramID);
            } else {
                GL.UseProgram(0);
            }
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            m_vb[0].bindGLAttributes(shaderPgm);
        }

        public void drawUnbind() {
            // unbind from use and undo draw settings
            GL.UseProgram(0);
            GL.PopClientAttrib();
            unbindPrivate();
        }

		private void bindPrivate() {
            // bind for use
			GL.BindBuffer (BufferTarget.ArrayBuffer, m_VBOid);
		}
		private void unbindPrivate() {
            // unbind from use
			GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
		}
	}
}

