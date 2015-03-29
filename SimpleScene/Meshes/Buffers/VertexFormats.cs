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
    public struct SSVertex_PosNormTexDiff : ISSVertexLayout {

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

		public unsafe void  BindGlAttributes(ref SSRenderConfig renderConfig) {
            // this is the "transitional" GLSL 120 way of assigning buffer contents
            // http://www.opentk.com/node/80?page=1

            GL.EnableClientState (ArrayCap.VertexArray);
            GL.VertexPointer (3, VertexPointerType.Float, sizeof(SSVertex_PosNormTexDiff), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormTexDiff), "Position"));

            GL.EnableClientState (ArrayCap.NormalArray);
            GL.NormalPointer (NormalPointerType.Float, sizeof(SSVertex_PosNormTexDiff), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormTexDiff), "Normal"));

			if (renderConfig.InstanceShader != null && renderConfig.InstanceShader.IsActive) {
				// instance pssm shader is not affected by this texture coordinate workaround
				int texcoordID = GL.GetAttribLocation (renderConfig.InstanceShader.m_programID, "texCoord");
				if (texcoordID != -1) {
					GL.EnableVertexAttribArray (texcoordID);
					GL.VertexAttribPointer (texcoordID, 
						2, VertexAttribPointerType.Float, false, 
						sizeof(SSVertex_PosNormTexDiff),
						(IntPtr)Marshal.OffsetOf (typeof(SSVertex_PosNormTexDiff), "TexCoord"));
				}
			} else {
				GL.EnableClientState (ArrayCap.TextureCoordArray);
				GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(SSVertex_PosNormTexDiff), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormTexDiff), "TexCoord"));
			}
        }
    }

	///////////////////////////////////////////////////////

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SSVertex_PosNormTex : ISSVertexLayout {
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TexCoord;

		public SSVertex_PosNormTex(Vector3 position, Vector3 normal, Vector2 texCoord)
		{
			Position = position;
			Normal = normal;
			TexCoord = texCoord;
		}

		public unsafe void  BindGlAttributes(ref SSRenderConfig renderConfig) {
			// this is the "transitional" GLSL 120 way of assigning buffer contents
			// http://www.opentk.com/node/80?page=1

			GL.EnableClientState (ArrayCap.VertexArray);
			GL.VertexPointer (3, VertexPointerType.Float, sizeof(SSVertex_PosNormTex), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormTex), "Position"));

			GL.EnableClientState (ArrayCap.NormalArray);
			GL.NormalPointer (NormalPointerType.Float, sizeof(SSVertex_PosNormTex), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormTex), "Normal"));

			if (renderConfig.InstanceShader != null && renderConfig.InstanceShader.IsActive) {
				// instance pssm shader is not affected by this texture coordinate workaround
				int texcoordID = GL.GetAttribLocation (renderConfig.InstanceShader.m_programID, "texCoord");
				if (texcoordID != -1) {
					GL.EnableVertexAttribArray (texcoordID);
					GL.VertexAttribPointer (texcoordID, 
						2, VertexAttribPointerType.Float, false, 
						sizeof(SSVertex_PosNormTex),
						(IntPtr)Marshal.OffsetOf (typeof(SSVertex_PosNormTex), "TexCoord"));
				}
			} else {
				GL.EnableClientState (ArrayCap.TextureCoordArray);
				GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(SSVertex_PosNormTex), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormTex), "TexCoord"));
			}
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

		public SSVertex_Pos(Vector3 pos) {
			Position = pos;
		}

		public unsafe void BindGlAttributes(ref SSRenderConfig renderConfig) {
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer(3, VertexPointerType.Float, sizeof(SSVertex_Pos), (IntPtr)Marshal.OffsetOf(typeof(SSVertex_Pos), "Position"));
        }
    }

    ///////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_PosTex : ISSVertexLayout
    {
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

		public unsafe void BindGlAttributes(ref SSRenderConfig renderConfig) {
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer(3, VertexPointerType.Float, sizeof(SSVertex_PosTex), (IntPtr)Marshal.OffsetOf(typeof(SSVertex_PosTex), "Position"));

			if (renderConfig.InstanceShader != null && renderConfig.InstanceShader.IsActive) {
				// instance pssm shader is not affected by this texture coordinate workaround
				int texcoordID = GL.GetAttribLocation (renderConfig.InstanceShader.m_programID, "texCoord");
				if (texcoordID != -1) {
					GL.EnableVertexAttribArray (texcoordID);
					GL.VertexAttribPointer (texcoordID, 
						2, VertexAttribPointerType.Float, false, 
						sizeof(SSVertex_PosTex),
						(IntPtr)Marshal.OffsetOf (typeof(SSVertex_PosTex), "TexCoord"));
				}
			} else {
				GL.EnableClientState (ArrayCap.TextureCoordArray);
				GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(SSVertex_PosTex), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosTex), "TexCoord"));
			}
        }
    }
}

