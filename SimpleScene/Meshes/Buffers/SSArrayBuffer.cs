using System;
using System.Runtime.InteropServices; // for Marshall.SizeOf()

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSAttributeBuffer
    {
        void DescribteAttribute (int attrLoc);
    }

    public abstract class SSArrayBuffer<V> 
        where V: struct
    {
        // dummy vertex for calling bindGLAttributes()
        protected static readonly V c_dummyElement = new V();
        protected static readonly int c_elementSz = Marshal.SizeOf(c_dummyElement);

        private readonly BufferUsageHint m_usageHint;
        private int m_bufferIdx = -1;
        private int m_numElements = 0;

        public int NumElements { get { return m_numElements; } }

        public SSArrayBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw) 
        {
            m_usageHint = hint;
        }

        public SSArrayBuffer (V[] vertices, 
                              BufferUsageHint hint = BufferUsageHint.StaticDraw) 
            : this(hint) 
        {
            UpdateBufferData(vertices);
        }

        public void Delete() {
            GL.DeleteBuffer(m_bufferIdx);
            m_bufferIdx = -1;
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
            if (m_bufferIdx == -1) {
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
}