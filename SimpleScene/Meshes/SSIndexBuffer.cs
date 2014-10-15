using System;

using OpenTK.Graphics.OpenGL;
namespace SimpleScene
{
	public class SSIndexBuffer
	{
        private readonly ISSVertexBuffer m_vbo;
        private readonly BufferUsageHint m_usageHint;
        private int m_IBOid = 0;
        private int m_numIndices = 0;
		
        public SSIndexBuffer (ISSVertexBuffer vbo, BufferUsageHint hint = BufferUsageHint.DynamicDraw)
		{
            m_vbo = vbo;
            m_usageHint = hint;
		}

        public SSIndexBuffer(UInt16[] indices, ISSVertexBuffer vbo, BufferUsageHint hint = BufferUsageHint.StaticDraw) 
        : this(vbo, hint) {
            UpdateBufferData(indices);
        }

		public void Delete() {
			GL.DeleteBuffer (m_IBOid);
            m_IBOid = 0;
            m_numIndices = 0;
		}

        public void UpdateBufferData(UInt16[] indices) {
            if (m_IBOid == 0) {
                m_IBOid = GL.GenBuffer();
            }
            m_numIndices = indices.Length;
            bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                         (IntPtr)(m_numIndices * sizeof(UInt16)),
                         indices, 
                         m_usageHint);
            unbind();
        }

        public void DrawElements(PrimitiveType primType, SSShaderProgram pgm = null) {
            m_vbo.drawBind(pgm);
            bind();
            GL.DrawElements(primType,
                            m_numIndices,
                            DrawElementsType.UnsignedShort,
                            IntPtr.Zero);
            unbind();
            m_vbo.drawUnbind();
        }

		private void bind(SSShaderProgram pgm = null) {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, m_IBOid);
		}

		private void unbind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, 0);
		}
	}
}

