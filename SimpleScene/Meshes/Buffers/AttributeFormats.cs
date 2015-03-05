using System;
using System.Runtime.InteropServices;


using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeVec3 : ISSAttributeLayout, IEquatable<SSAttributeVec3>
    {
        public Vector3 Value;

        public SSAttributeVec3(Vector3 value) 
        {
            Value = value;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }
        public Int32 ComponentNum() { return 3; }
        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributeVec3 other)
        {
            return this.Value == other.Value;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeVec2 : ISSAttributeLayout, IEquatable<SSAttributeVec2>
    {
		public Vector2 Value;

		public SSAttributeVec2(Vector2 value)
        {
			Value = value;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }
		public Int32 ComponentNum() { return 2; }
        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributeVec2 other)
        {
			return this.Value == other.Value;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeFloat : ISSAttributeLayout, IEquatable<SSAttributeFloat>
    {
        public float Value;

        public SSAttributeFloat(float data)
        {
            Value = data;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }
        public Int32 ComponentNum() { return 1; }
        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributeFloat other)
        {
            return this.Value == other.Value;
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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeByte : ISSAttributeLayout, IEquatable<SSAttributeByte>
    {
        public byte Value;

        public SSAttributeByte(byte data)
        {
            Value = data;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Byte; }
        public Int32 ComponentNum() { return 1; }
        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributeByte other)
        {
            return this.Value == other.Value;
        }
    }
}

