using System;

using OpenTK.Graphics.OpenGL;
namespace SimpleScene
{
	public class SSIndexBuffer
	{
        private readonly UInt16[] m_indices;
        private readonly SSIVertexBuffer m_vbo;
        private readonly BufferUsageHint m_usageHint;
        private int m_IBOid = 0;
		
        public unsafe SSIndexBuffer (UInt16[] indices, SSIVertexBuffer vbo, BufferUsageHint hint = BufferUsageHint.StaticDraw)
		{
            m_indices = indices;
            m_vbo = vbo;
            m_usageHint = hint;
            UpdateBufferData();
		}

		public void Delete() {
			GL.DeleteBuffer (m_IBOid);
            m_IBOid = 0;
		}

        public void UpdateBufferData() {
            if (m_IBOid == 0) {
                m_IBOid = GL.GenBuffer();
            }
            bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer, 
                         (IntPtr)(m_indices.Length * sizeof(UInt16)),
                         m_indices, 
                         m_usageHint);
            unbind();
        }

        public void DrawElements(PrimitiveType primType, SSShaderProgram pgm = null) {
            bind(pgm);
            GL.DrawElements(primType,
                            m_indices.Length,
                            DrawElementsType.UnsignedShort,
                            IntPtr.Zero);
            unbind();
        }

		private void bind(SSShaderProgram pgm = null) {
            m_vbo.bind(pgm);
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, m_IBOid);
		}

		private void unbind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, 0);
            m_vbo.unbind();
		}
	}
}

