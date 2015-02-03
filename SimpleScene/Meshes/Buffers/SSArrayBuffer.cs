using System;
using System.Runtime.InteropServices; // for Marshall.SizeOf()

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public abstract class SSArrayBuffer<Element> 
        where Element: struct
    {
        // dummy vertex for calling bindGLAttributes()
        protected static readonly Element c_dummyElement = new Element();
        protected static readonly int c_elementSz = Marshal.SizeOf(c_dummyElement);

        private readonly BufferUsageHint m_usageHint;
        private int m_bufferIdx = -1;
        private int m_numElements = 0;

        public int NumElements { get { return m_numElements; } }

        public SSArrayBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw) 
        {
            m_usageHint = hint;
            // TODO: pre-allocate buffer? will that help if updating buffer partially?
        }

        public SSArrayBuffer (Element[] elements, 
                              BufferUsageHint hint = BufferUsageHint.StaticDraw) 
            : this(hint) 
        {
            UpdateBufferData(elements);
        }

        public void Delete() {
            GL.DeleteBuffer(m_bufferIdx);
            m_bufferIdx = -1;
            m_numElements = 0;
        }

        public void UpdateBufferData(Element[] vertices)
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

        protected void updatePrivate(Element[] vertices) {
            m_numElements = vertices.Length;
            GL.BufferData(BufferTarget.ArrayBuffer,
                (IntPtr)(m_numElements * c_elementSz),
                vertices,
                m_usageHint);
        }
    }
}