using System;
using System.Runtime.InteropServices; // for Marshall.SizeOf()

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public interface ISSVertexLayout {
		void bindGLAttributes();
	}

    public interface ISSVertexBuffer
    {
        void DrawBind();
        void DrawUnbind();
    }

    public interface ISSAttributeBuffer
    {
        void ConfigureAttribute (int attrLoc);
    }

    public abstract class SSArrayBuffer<V> where V: struct
    {
        // dummy vertex for calling bindGLAttributes()
        protected static readonly V c_dummyElement = new V();
        protected static readonly int c_elementSz = Marshal.SizeOf(c_dummyElement);

        private readonly BufferUsageHint m_usageHint;
        private int m_bufferIdx = 0;
        private int m_numElements = 0;

        public int NumElements { get { return m_numElements; } }

        public SSArrayBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw) {
            m_usageHint = hint;
        }

        public SSArrayBuffer (V[] vertices, 
                                 BufferUsageHint hint = BufferUsageHint.StaticDraw) 
            : this(hint) {
            UpdateBufferData(vertices);
        }

        public void Delete() {
            GL.DeleteBuffer(m_bufferIdx);
            m_bufferIdx = 0;
            m_numElements = 0;
        }

        public void UpdateBufferData(V[] vertices)
        {
            genBufferPrivate();
            bindPrivate();
            updatePrivate(vertices);
            unbindPrivate();
        }

        protected void bindPrivate() {
            // bind for use
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_bufferIdx);
        }

        protected void unbindPrivate() {
            // unbind from use
            GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
        }

        protected void genBufferPrivate() {
            if (m_bufferIdx == 0) {
                m_bufferIdx = GL.GenBuffer();
            }
        }

        protected void updatePrivate(V[] vertices) {
            m_numElements = vertices.Length;
            GL.BufferData(BufferTarget.ArrayBuffer,
                (IntPtr)(m_numElements * c_elementSz),
                vertices,
                m_usageHint);
        }
    }

    // http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects
    public class SSVertexBuffer<V> : SSArrayBuffer<V>, ISSVertexBuffer
        where V : struct, ISSVertexLayout 
    {
        public SSVertexBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw)
            : base(hint)
        { }

        public SSVertexBuffer (V[] vertices, 
                               BufferUsageHint hint = BufferUsageHint.StaticDraw) 
            : base(vertices, hint)
        { }

        public void DrawArrays(PrimitiveType primType, bool doBind = true) {
            if (doBind) DrawBind();
            drawPrivate(primType);
            if (doBind) DrawUnbind();
        }

        public void UpdateAndDrawArrays(V[] vertices,
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
            c_dummyElement.bindGLAttributes();
        }

        public void DrawUnbind() {
            // unbind from use and undo draw settings
            GL.PopClientAttrib();
            unbindPrivate();
        }

        protected void drawPrivate(PrimitiveType primType) {
            GL.DrawArrays(primType, 0, NumElements);
        }
	}
}

