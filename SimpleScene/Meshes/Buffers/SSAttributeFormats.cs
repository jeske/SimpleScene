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

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }

        public Int32 ComponentNum() { return 3; }

        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributePos other)
        {
            return this.Position == other.Position;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeColor : ISSAttributeLayout, IEquatable<SSAttributeColor>
    {
        public UInt32 Color;

        public SSAttributeColor(UInt32 color) {
            Color = color;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.UnsignedByte; }

        public Int32 ComponentNum() { return 4; }

        public bool IsNormalized() { return true; }

        public bool Equals (SSAttributeColor other)
        {
            return this.Color == other.Color;
        }
    }
}

