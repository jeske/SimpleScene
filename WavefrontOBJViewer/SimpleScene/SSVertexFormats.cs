// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Runtime.InteropServices;

using OpenTK;

namespace WavefrontOBJViewer
{
	
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct SSVertex_PosNormDiffTex1 {
		public Vector3 Position;
        public Vector3 Normal;
        public int DiffuseColor;
        public float Tu, Tv;
	}
}

