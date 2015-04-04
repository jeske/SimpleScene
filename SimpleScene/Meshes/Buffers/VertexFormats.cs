// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public static class SSVertexFormatHelper
	{
		public static void PreparePosition (int stride, IntPtr offset)
		{
			// this is the "transitional" GLSL 120 way of assigning buffer contents
			// http://www.opentk.com/node/80?page=1
			GL.EnableClientState (ArrayCap.VertexArray);
			GL.VertexPointer (3, VertexPointerType.Float, stride, offset);
		}

        public static void PrepareColor(int stride, IntPtr offset)
        {
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.ColorPointer(4, ColorPointerType.UnsignedInt, stride, offset);
        }

		public static void PrepareNormal(ref SSRenderConfig renderConfig, int stride, IntPtr offset)
		{
			ISSInstancableShaderProgram isp = getActiveInstanceShader (ref renderConfig);
			if (isp == null) { // no instancing
				// this is the "transitional" GLSL 120 way of assigning buffer contents
				// http://www.opentk.com/node/80?page=1
				GL.EnableClientState (ArrayCap.NormalArray);
				GL.NormalPointer (NormalPointerType.Float, stride, offset);
			} else { // instancing
				preparePerVertexAttribute (stride, offset, isp.AttrNormal, 3);
			}
		}

		public static void PrepareTexCoord(ref SSRenderConfig renderConfig, int stride, IntPtr offset)
		{
			ISSInstancableShaderProgram isp = getActiveInstanceShader (ref renderConfig);
			if (isp == null) { // no instancing
				// this is the "transitional" GLSL 120 way of assigning buffer contents
				// http://www.opentk.com/node/80?page=1
				GL.EnableClientState (ArrayCap.TextureCoordArray);
				GL.TexCoordPointer (2, TexCoordPointerType.Float, stride, offset);
			} else { // instancing
				preparePerVertexAttribute (stride, offset, isp.AttrTexCoord, 2);
			}
		}

		private static ISSInstancableShaderProgram getActiveInstanceShader(ref SSRenderConfig renderConfig)
		{

			if (renderConfig.InstanceShader != null && renderConfig.InstanceShader.IsActive) {
				return renderConfig.InstanceShader;
			} else if (renderConfig.InstancePssmShader != null && renderConfig.InstancePssmShader.IsActive) {
				return renderConfig.InstancePssmShader;
			}
			return null;
		}

		private static void preparePerVertexAttribute(
			int stride, IntPtr offset, int attrLoc, int attrNumFloats)
		{
			if (attrLoc != -1) {
				GL.EnableVertexAttribArray (attrLoc);
				GL.VertexAttribPointer ( 
					attrLoc, attrNumFloats, VertexAttribPointerType.Float, false,
					stride, offset);
			}
		}
	}

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_PosNormTexDiff : ISSVertexLayout {
		static private int Size;
		static private IntPtr PositionOffset;
		static private IntPtr NormalOffset;
		static private IntPtr TexCoordOffset;

		static unsafe SSVertex_PosNormTexDiff()
		{
			Type type = typeof(SSVertex_PosNormTexDiff);
			Size = Marshal.SizeOf (type);
			PositionOffset = Marshal.OffsetOf (type, "Position");
			NormalOffset = Marshal.OffsetOf (type, "Normal");
			TexCoordOffset = Marshal.OffsetOf (type, "TexCoord");
		}

        public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TexCoord;
		public Int32 DiffuseColor;

		public float Tu {
			get { return TexCoord.X; }
			set { TexCoord.X = value; }
		}

		public float Tv {
			get { return TexCoord.Y; }
			set { TexCoord.Y = value; }
		}

		public void BindGlAttributes(ref SSRenderConfig renderConfig) {
			SSVertexFormatHelper.PreparePosition (Size, PositionOffset);
			SSVertexFormatHelper.PrepareNormal (ref renderConfig, Size, NormalOffset);
			SSVertexFormatHelper.PrepareTexCoord (ref renderConfig, Size, TexCoordOffset);
        }
    }

	///////////////////////////////////////////////////////

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SSVertex_PosNormTex : ISSVertexLayout {
		static private int Size;
		static private IntPtr PositionOffset;
		static private IntPtr NormalOffset;
		static private IntPtr TexCoordOffset;

		static unsafe SSVertex_PosNormTex()
		{
			Type type = typeof(SSVertex_PosNormTex);
			Size = Marshal.SizeOf (type);
			PositionOffset = Marshal.OffsetOf (type, "Position");
			NormalOffset = Marshal.OffsetOf (type, "Normal");
			TexCoordOffset = Marshal.OffsetOf (type, "TexCoord");
		}

		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TexCoord;

		public float Tu {
			get { return TexCoord.X; }
			set { TexCoord.X = value; }
		}

		public float Tv {
			get { return TexCoord.Y; }
			set { TexCoord.Y = value; }
		}

		public SSVertex_PosNormTex(Vector3 position, Vector3 normal, Vector2 texCoord)
		{
			Position = position;
			Normal = normal;
			TexCoord = texCoord;
		}

		public void  BindGlAttributes(ref SSRenderConfig renderConfig) {
			SSVertexFormatHelper.PreparePosition (Size, PositionOffset);
			SSVertexFormatHelper.PrepareNormal (ref renderConfig, Size, NormalOffset);
			SSVertexFormatHelper.PrepareTexCoord (ref renderConfig, Size, TexCoordOffset);
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
		static private int Size;
		static private IntPtr PositionOffset;

		static unsafe SSVertex_Pos()
		{
			Type type = typeof(SSVertex_Pos);
			Size = Marshal.SizeOf (type);
			PositionOffset = Marshal.OffsetOf (type, "Position");
		}

        public Vector3 Position;

        public SSVertex_Pos(float x, float y, float z) {
            Position = new Vector3 (x, y, z);
        }

		public SSVertex_Pos(Vector3 pos) {
			Position = pos;
		}

		public void BindGlAttributes(ref SSRenderConfig renderConfig) {
			SSVertexFormatHelper.PreparePosition (Size, PositionOffset);
        }
    }

    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_PosColor : ISSVertexLayout
    {
        static private int Size;
        static private IntPtr PositionOffset;
        static private IntPtr ColorOffset;

        static unsafe SSVertex_PosColor()
        {
            Type type = typeof(SSVertex_Pos);
            Size = Marshal.SizeOf (type);
            PositionOffset = Marshal.OffsetOf (type, "Position");
            ColorOffset = Marshal.OffsetOf(type, "Color");
        }

        public Vector3 Position;
        public UInt32 Color;

        public SSVertex_PosColor(Vector3 pos, Color4 color) {
            Position = pos;
            Color = Color4Helper.ToUInt32(color);
        }

        public void BindGlAttributes(ref SSRenderConfig renderConfig) {
            SSVertexFormatHelper.PreparePosition (Size, PositionOffset);
            SSVertexFormatHelper.PrepareColor(Size, ColorOffset);
        }
    }

    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_PosTex : ISSVertexLayout
    {
		static private int Size;
		static private IntPtr PositionOffset;
		static private IntPtr TexCoordOffset;

		static unsafe SSVertex_PosTex()
		{
			Type type = typeof(SSVertex_PosTex);
			Size = Marshal.SizeOf (type);
			PositionOffset = Marshal.OffsetOf (type, "Position");
			TexCoordOffset = Marshal.OffsetOf (type, "TexCoord");
		}

        public Vector2 TexCoord;
        public Vector3 Position;

        public SSVertex_PosTex(float x, float y, float z, float u, float v) {
            TexCoord = new Vector2 (u, v);
            Position = new Vector3 (x, y, z);
        }

		public SSVertex_PosTex(Vector3 position, Vector2 texCoord)
		{
			TexCoord = texCoord;
			Position = position;
		}

		public void BindGlAttributes(ref SSRenderConfig renderConfig) {
			SSVertexFormatHelper.PreparePosition (Size, PositionOffset);
			SSVertexFormatHelper.PrepareTexCoord (ref renderConfig, Size, TexCoordOffset);
        }
    }
}

