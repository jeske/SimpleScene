// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_PosNormDiffTex1 : ISSVertexLayout {
        public float Tu, Tv;
        public Int32 DiffuseColor;

        public Vector3 Normal;
        public Vector3 Position;

        public unsafe void  BindGlAttributes() {
            // this is the "transitional" GLSL 120 way of assigning buffer contents
            // http://www.opentk.com/node/80?page=1

            GL.EnableClientState (ArrayCap.VertexArray);
            GL.VertexPointer (3, VertexPointerType.Float, sizeof(SSVertex_PosNormDiffTex1), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormDiffTex1), "Position"));

            GL.EnableClientState (ArrayCap.NormalArray);
            GL.NormalPointer (NormalPointerType.Float, sizeof(SSVertex_PosNormDiffTex1), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormDiffTex1), "Normal"));

            GL.EnableClientState (ArrayCap.TextureCoordArray);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(SSVertex_PosNormDiffTex1), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormDiffTex1), "Tu"));
        }
    }

    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_PosNormDiff {
        public Vector3 Position;
        public Vector3 Normal;
        public int DiffuseColor;
    }

    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_Pos : ISSVertexLayout
    {
        public Vector3 Position;

        public SSVertex_Pos(float x, float y, float z) {
            Position = new Vector3 (x, y, z);
        }

        public unsafe void BindGlAttributes() {
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer(3, VertexPointerType.Float, sizeof(SSVertex_Pos), (IntPtr)Marshal.OffsetOf(typeof(SSVertex_Pos), "Position"));
        }
    }

    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_PosTex1 : ISSVertexLayout
    {
        public Vector2 TexCoord;
        public Vector3 Position;

        public SSVertex_PosTex1(float x, float y, float z, float u, float v) {
            TexCoord = new Vector2 (u, v);
            Position = new Vector3 (x, y, z);
        }

        public unsafe void BindGlAttributes() {
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer(3, VertexPointerType.Float, sizeof(SSVertex_PosTex1), (IntPtr)Marshal.OffsetOf(typeof(SSVertex_PosTex1), "Position"));

            GL.EnableClientState (ArrayCap.TextureCoordArray);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(SSVertex_PosTex1), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosTex1), "TexCoord"));
        }
    }
}

