using System;
using System.Runtime.InteropServices;


using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSAttributeFormats
    {
        ///////////////////////////////////////////////////////

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SSAttributePos : ISSAttributeLayout
        {
            public Vector3 Position;

            public SSAttributePos(float x, float y, float z) {
                Position = new Vector3 (x, y, z);
            }

            public VertexAttribPointerType AttributeType() { return VertexAttribPointerType.Float; }

            public bool IsNormalized() { return false; }
        }
    }
}

