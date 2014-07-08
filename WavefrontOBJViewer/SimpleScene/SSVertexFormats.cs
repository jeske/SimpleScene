// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using OpenTK;

namespace WavefrontOBJViewer
{
	
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SSVertex_PosNormDiffTex1 : IEqualityComparer<SSVertex_PosNormDiffTex1> {
		public Vector3 Position;
        public Vector3 Normal;
        
        public int DiffuseColor;

        public float Tu, Tv;
        
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


}

