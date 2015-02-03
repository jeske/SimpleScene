using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSAttributeLayout
    {
        VertexAttribType attrType();
        bool normalized ();
    }

    public interface ISSAttributeBuffer
    {
        void DescribeAttribute (int attrLoc);
    }

    public class SSAttributeBuffer<Attribute> : SSArrayBuffer<Attribute>, ISSAttributeBuffer
        where Attribute : struct, ISSAttributeLayout
    {
        public SSAttributeBuffer(BufferUsageHint hint = BufferUsageHint.DynamicDraw)
            : base(hint)
        { }

        public SSAttributeBuffer (Attribute[] attributes, 
                                  BufferUsageHint hint = BufferUsageHint.StaticDraw) 
            : base(attributes, hint)
        { }

        public void DescribeAttribute (int attrLoc)
        {

        }
    }
}

