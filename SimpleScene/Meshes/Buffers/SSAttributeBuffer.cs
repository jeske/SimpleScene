using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSAttributeLayout
    {
        void PrepareAttribute (int instancesPerValue, int attrLoc);
        void DisableAttribute (int attrLoc);
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

        public void PrepareAttribute (int instancesPerValue = -1, int attrLoc = -1)
        {
            bind();
            GL.PushClientAttrib(ClientAttribMask.ClientAllAttribBits);
            c_dummyElement.PrepareAttribute(instancesPerValue, attrLoc);
            unbind();
        }

        public void DisableAttribute(int attrLoc = -1)
        {
            c_dummyElement.DisableAttribute(attrLoc);
            GL.PopClientAttrib();
        }
    }
}

