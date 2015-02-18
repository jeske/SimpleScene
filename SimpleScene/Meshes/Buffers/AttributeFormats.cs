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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeMasterScale : ISSAttributeLayout, IEquatable<SSAttributeMasterScale>
    {
        public float Scale;

        public SSAttributeMasterScale(float scale)
        {
            Scale = scale;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }
        public Int32 ComponentNum() { return 1; }
        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributeMasterScale other)
        {
            return this.Scale == other.Scale;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeOrientation : ISSAttributeLayout, IEquatable<SSAttributeOrientation>
    {
        public Vector3 Euler;

        public SSAttributeOrientation(Vector3 euler)
        {
            Euler = euler;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }
        public Int32 ComponentNum() { return 3; }
        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributeOrientation other)
        {
            return this.Euler == other.Euler;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSAttributeComponentScale : ISSAttributeLayout, IEquatable<SSAttributeComponentScale>
    {
        public Vector3 Scale;

        public SSAttributeComponentScale(Vector3 scale)
        {
            Scale = scale;
        }

        public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }
        public Int32 ComponentNum() { return 3; }
        public bool IsNormalized() { return false; }

        public bool Equals(SSAttributeComponentScale other)
        {
            return this.Scale == other.Scale;
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
}

