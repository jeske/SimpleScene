using System;

using OpenTK.Graphics.OpenGL;
namespace SimpleScene
{
	public class SSIndexBuffer : ISSInstancable
	{
        private readonly ISSVertexBuffer m_vbo;
        private readonly BufferUsageHint m_usageHint;
        private int m_IBOid = 0;
        private int m_numIndices = 0;

        public int NumIndices { get { return m_numIndices; } }

        public SSIndexBuffer (ISSVertexBuffer vbo, BufferUsageHint hint = BufferUsageHint.DynamicDraw)
		{
            m_vbo = vbo;
            m_usageHint = hint;
		}

        public SSIndexBuffer(UInt16[] indices, ISSVertexBuffer vbo, BufferUsageHint hint = BufferUsageHint.StaticDraw) 
        : this(vbo, hint) 
        {
            UpdateBufferData(indices);
        }

		public void Delete() 
        {
			GL.DeleteBuffer (m_IBOid);
            m_IBOid = 0;
            m_numIndices = 0;
		}

        public void UpdateBufferData(UInt16[] indices) 
        {
            if (m_IBOid == 0) {
                m_IBOid = GL.GenBuffer();
            }
            m_numIndices = indices.Length;
            Bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                         (IntPtr)(m_numIndices * sizeof(UInt16)),
                         indices, 
                         m_usageHint);
            Unbind();
        }

        public void DrawElements(PrimitiveType primType, bool doBind = true) 
        {
            if (doBind) {
                m_vbo.DrawBind();
                Bind();
            }
            GL.DrawElements(primType,
                            m_numIndices,
                            DrawElementsType.UnsignedShort,
                            IntPtr.Zero);
            if (doBind) {
                Unbind();
                m_vbo.DrawUnbind();
            }
        }

		public void RenderInstanced(int instanceCount, PrimitiveType primType = PrimitiveType.Triangles)
        {
            m_vbo.DrawBind();
            Bind();
            GL.DrawElementsInstanced(
                primType,
                m_numIndices,
                DrawElementsType.UnsignedShort,
                IntPtr.Zero,
                instanceCount
            );
            Unbind();
            m_vbo.DrawUnbind();
        }

		public SSTexture InstanceTexture() { return null; }

        public void Bind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, m_IBOid);
		}

        public void Unbind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, 0);
		}
	}
}

