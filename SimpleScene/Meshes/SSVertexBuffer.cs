using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public interface ISSVertexLayout {
        int sizeOf();
		void bindGLAttributes();
	}

	// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

    public interface ISSVertexBuffer
    {
        void drawBind();
        void drawUnbind();
    }

    public class SSVertexBuffer<VB> : ISSVertexBuffer
        where VB : struct, ISSVertexLayout {
        private readonly BufferUsageHint m_usageHint;
        private int m_VBOid = 0;
        private int m_numVertices = 0;
        // dummy vertex for calling bindGLAttributes() and sizeOf()
        private static readonly VB m_dummy = new VB();

        public int NumVertices { get { return m_numVertices; } }

        public SSVertexBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw) {
            m_usageHint = hint;
        }
        
        public SSVertexBuffer (VB[] vertices,
                               BufferUsageHint hint = BufferUsageHint.StaticDraw) 
        : this(hint) {
            UpdateBufferData(vertices);
		}

        public void Delete() {
            GL.DeleteBuffer(m_VBOid);
            m_VBOid = 0;
            m_numVertices = 0;
        }

        public void UpdateBufferData(VB[] vertices)
        {
            genBufferPrivate();
            bindPrivate();
            updatePrivate(vertices);
            unbindPrivate();
        }

        public void DrawArrays(PrimitiveType primType) {
            drawPrivate(primType);
            drawUnbind();
        }

        public void UpdateAndDrawArrays(VB[] vertices,
                                        PrimitiveType primType) {
            genBufferPrivate();
            drawBind();
            updatePrivate(vertices);
            drawPrivate(primType);
            drawUnbind();
        }

        public void drawBind() {
            // bind for use and setup for drawing
            bindPrivate();
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            m_dummy.bindGLAttributes();
        }

        public void drawUnbind() {
            // unbind from use and undo draw settings
            GL.PopClientAttrib();
            unbindPrivate();
        }

        private void genBufferPrivate() {
            if (m_VBOid == 0) {
                m_VBOid = GL.GenBuffer();
            }
        }

        private void updatePrivate(VB[] vertices) {
            m_numVertices = vertices.Length;
            GL.BufferData(BufferTarget.ArrayBuffer,
               (IntPtr)(m_numVertices * m_dummy.sizeOf()),
               vertices,
               m_usageHint);
        }

        private void drawPrivate(PrimitiveType primType) {
            GL.DrawArrays(primType, 0, m_numVertices);
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

