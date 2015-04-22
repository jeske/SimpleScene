using System;

namespace SimpleScene
{
	public class SSSkeletalMesh
	{
		public SSSkeletalVertex[] Vertices = null;
		public SSSkeletalWeight[] Weights = null;
		public UInt16[] TriangleIndices = null;
		public SSSkeletalJointBaseInfo[] Joints = null;
		public string MaterialShaderString = "";
	}
}

