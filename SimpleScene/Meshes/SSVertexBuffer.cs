using System;
using System.Runtime.InteropServices; // for Marshall.SizeOf()

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public interface ISSVertexLayout {
		void bindGLAttributes();
	}

	// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

    public interface ISSVertexBuffer
    {
        void DrawBind();
        void DrawUnbind();
    }

    public class SSVertexBuffer<VB> : ISSVertexBuffer
        where VB : struct, ISSVertexLayout {
        private readonly BufferUsageHint m_usageHint;
        private int m_VBOid = 0;
        private int m_numVertices = 0;
        // dummy vertex for calling bindGLAttributes()
        private static readonly VB c_dummy = new VB();
        private static readonly int c_vertexSz = Marshal.SizeOf(c_dummy);

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

        public void DrawArrays(PrimitiveType primType, bool doBind = true) {
            if (doBind) DrawBind();
            drawPrivate(primType);
            if (doBind) DrawUnbind();
        }

        public void UpdateAndDrawArrays(VB[] vertices,
                                        PrimitiveType primType,
                                        bool doBind = true)
        {
            genBufferPrivate();
            if (doBind) DrawBind();
            updatePrivate(vertices);
            drawPrivate(primType);
            if (doBind) DrawUnbind();
        }

        public void DrawBind() {
            // bind for use and setup for drawing
            bindPrivate();
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            c_dummy.bindGLAttributes();
        }

        public void DrawUnbind() {
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
                (IntPtr)(m_numVertices * c_vertexSz),
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

