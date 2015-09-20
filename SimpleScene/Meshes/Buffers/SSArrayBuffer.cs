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
        private int _numElements = 0;
        private Element[] _lastAssignedElements = null;

        public int numElements { get { return _numElements; } }
        public Element[] lastAssignedElements {
            get { return _lastAssignedElements; }
        }

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
            _numElements = 0;
        }

        public void UpdateBufferData(Element[] elements)
        {
            genBufferPrivate();
            bind();
            updatePrivate(elements);
            unbind();
        }

        protected void bind() {
            // bind for use
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_bufferIdx);
        }

        protected void unbind() {
            // unbind from use
            GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
        }

        protected void genBufferPrivate() {
            if (m_bufferIdx == -1) {
                m_bufferIdx = GL.GenBuffer();
            }
        }

		protected void updatePrivate(Element[] elements, int numElements = -1) {
			if (numElements <= 0) {
				numElements = elements.Length;
			}
            _lastAssignedElements = elements;
			_numElements = numElements;
            GL.BufferData(BufferTarget.ArrayBuffer,
                (IntPtr)(_numElements * c_elementSz),
                elements,
                m_usageHint);
        }
    }
}