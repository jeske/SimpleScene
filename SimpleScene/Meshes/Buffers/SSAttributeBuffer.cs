using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSAttributeLayout
    {
        VertexAttribPointerType AttributeType ();
        Int32 ComponentNum();
        bool IsNormalized ();
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

        public void PrepareAttribute (int attrLoc, int instancesPerValue)
        {
            if (attrLoc == -1) return;

            GL.EnableVertexAttribArray(attrLoc);
            bind();
            GL.VertexAttribPointer(attrLoc, 
                c_dummyElement.ComponentNum(), c_dummyElement.AttributeType(), c_dummyElement.IsNormalized(),
                0, IntPtr.Zero);
            unbind();
            GL.VertexAttribDivisor(attrLoc, instancesPerValue);
        }

		public void PrepareAttributeAndUpdate (int attrLoc, int instancesPerValue, Attribute[] data, int numToUpdate=-1)
		{
			if (attrLoc == -1 || numToUpdate == 0) return;
			if (numToUpdate < 0) {
				numToUpdate = data.Length;
			}

			GL.EnableVertexAttribArray(attrLoc);
			bind();
			GL.VertexAttribPointer(attrLoc, 
				c_dummyElement.ComponentNum(), c_dummyElement.AttributeType(), c_dummyElement.IsNormalized(),
				0, IntPtr.Zero);
			genBufferPrivate();
			updatePrivate (data, numToUpdate);
			unbind();
			GL.VertexAttribDivisor(attrLoc, instancesPerValue);
		}

        public void DisableAttribute(int attrLoc)
        {
            if (attrLoc != -1) {
                GL.DisableVertexAttribArray(attrLoc);
                //GL.PopClientAttrib();
            }
        }
    }
}

