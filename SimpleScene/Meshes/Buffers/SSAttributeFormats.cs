using System;
using System.Runtime.InteropServices;


using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributePos : ISSAttributeLayout, IEquatable<SSAttributePos>
    {
        public Vector3 Position;

        public SSAttributePos(Vector3 pos) 
        {
            Position = pos;
        }

        public void PrepareAttribute(int attrLoc) {
            GL.VertexAttribPointer(
                attrLoc, Marshal.SizeOf(this),
                VertexAttribPointerType.Float, false,
                0, IntPtr.Zero);
        }

        public bool Equals(SSAttributePos other)
        {
            return this.Position == other.Position;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeColor : ISSAttributeLayout, IEquatable<SSAttributeColor>
    {
        public Int32 Color;

        public SSAttributeColor(Int32 color) {
            Color = color;
        }

        public void PrepareAttribute(int attrLoc) {
            GL.ColorPointer(4, ColorPointerType.Int, 0, IntPtr.Zero);
        }

        public bool Equals (SSAttributeColor other)
        {
            return this.Color == other.Color;
        }
    }
}

