using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSAttributeLayout
    {
        void PrepareAttribute (int attrLoc);

    }

    public interface ISSAttributeBuffer
    {
        void PrepareAttribute (int attrLoc, int instancesPerValue);
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

        public void PrepareAttribute (int instancesPerValue, int attrLoc = 0)
        {
            GL.EnableVertexAttribArray(attrLoc);
            bind();
            c_dummyElement.PrepareAttribute(attrLoc);
            unbind();
            GL.VertexAttribDivisor(attrLoc, instancesPerValue);
        }

        public void DisableAttribute(int attrLoc)
        {
            GL.DisableVertexAttribArray(attrLoc);
        }
    }
}

