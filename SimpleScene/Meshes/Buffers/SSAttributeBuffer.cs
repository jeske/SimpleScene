using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSAttributeLayout
    {
        VertexAttribPointerType AttributeType ();
        bool IsNormalized ();
    }

    public interface ISSAttributeBuffer
    {
        void PrepareAttribute (int attrLoc);
        void DisableAttribute (int attrLoc);
    }

    public class SSAttributeBuffer<Attribute> : SSArrayBuffer<Attribute>, ISSAttributeBuffer
        where Attribute : struct, ISSAttributeLayout
    {
        public SSAttributeBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw)
            : base(hint)
        { 
        }

        public SSAttributeBuffer (Attribute[] attributes, 
                                  BufferUsageHint hint = BufferUsageHint.StaticDraw) 
            : base(attributes, hint)
        {
        }

        public void PrepareAttribute (int attrLoc)
        {
            GL.EnableVertexAttribArray(attrLoc);
            bindPrivate();
            GL.VertexAttribPointer(
                attrLoc, c_elementSz, 
                c_dummyElement.AttributeType(), c_dummyElement.IsNormalized(),
                0, IntPtr.Zero);
            unbindPrivate();
        }

        public void DisableAttribute(int attrLoc)
        {
            GL.DisableVertexAttribArray(attrLoc);

        }
    }
}

