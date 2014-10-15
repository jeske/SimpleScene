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
	public struct SSVertex_PosNormDiffTex1 : IEqualityComparer<SSVertex_PosNormDiffTex1>, ISSVertexLayout {
		public float Tu, Tv;
		public Int32 DiffuseColor;

        public Vector3 Normal;
		public Vector3 Position;

		private void checkGLError() {
			ErrorCode glERR;
			if ((glERR = GL.GetError ()) != ErrorCode.NoError) {
				throw new Exception (String.Format ("GL Error: {0}", glERR));
		 	}
		}
		public unsafe void  bindGLAttributes(SSShaderProgram shader) {
			// this is the "transitional" GLSL 120 way of assigning buffer contents
			// http://www.opentk.com/node/80?page=1

			GL.EnableClientState (ArrayCap.VertexArray);
			GL.VertexPointer (3, VertexPointerType.Float, sizeof(SSVertex_PosNormDiffTex1), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormDiffTex1), "Position"));

			GL.EnableClientState (ArrayCap.NormalArray);
			GL.NormalPointer (NormalPointerType.Float, sizeof(SSVertex_PosNormDiffTex1), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormDiffTex1), "Normal"));

			GL.EnableClientState (ArrayCap.TextureCoordArray);
			GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(SSVertex_PosNormDiffTex1), (IntPtr) Marshal.OffsetOf (typeof(SSVertex_PosNormDiffTex1), "Tu"));
		}
        
        public bool Equals(SSVertex_PosNormDiffTex1 a, SSVertex_PosNormDiffTex1 b) {
        		return 
					a.Position==b.Position 
					&& a.Normal==b.Normal 
					&& a.DiffuseColor==b.DiffuseColor
					&& a.Tu==b.Tu
					&& a.Tv==b.Tv;
        }
        public int GetHashCode(SSVertex_PosNormDiffTex1 a) {
            return a.GetHashCode();
        }
        public unsafe int sizeOf() {
            return sizeof (SSVertex_PosNormDiffTex1);
        }
        public override bool Equals( object ob ){
			if( ob is SSVertex_PosNormDiffTex1 ) {
				SSVertex_PosNormDiffTex1 c = (SSVertex_PosNormDiffTex1) ob;
				return this.Equals(this,c);
			}
			else {
				return false;
			}
		}
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}

	///////////////////////////////////////////////////////

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SSVertex_PosNormDiff {
		public Vector3 Position;
        public Vector3 Normal;
        
        public int DiffuseColor;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SSVertex_Pos : IEqualityComparer<SSVertex_Pos>, ISSVertexLayout
    {
        public Vector3 Position;

        public unsafe void bindGLAttributes(SSShaderProgram shader) {
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer(3, VertexPointerType.Float, sizeof(SSVertex_Pos), (IntPtr)Marshal.OffsetOf(typeof(SSVertex_Pos), "Position"));
        }

        public int GetHashCode(SSVertex_Pos a) {
            return a.GetHashCode();
        }
        unsafe public int sizeOf() {
            return sizeof(SSVertex_Pos);
        }

        public bool Equals(SSVertex_Pos a, SSVertex_Pos b) {
            return a.Position == b.Position;
        }

        public override bool Equals(object ob) {
            if (ob is SSVertex_Pos) {
                SSVertex_Pos c = (SSVertex_Pos)ob;
                return this.Equals(this, c);
            } else {
                return false;
            }
        }
    }
}

