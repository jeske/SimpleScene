using System;
using System.Text.RegularExpressions;
using OpenTK;

namespace SimpleScene
{
	// only used to register with the asset manager
	public class SSSkeletalMeshMD5 : SSSkeletalMesh 
	{ 
	}

	public class SSMD5MeshParser : SSMD5Parser
	{
		public static SSSkeletalMeshMD5[] ReadMeshes(SSAssetManager.Context ctx, string filename)
		{
			var parser = new SSMD5MeshParser (ctx, filename);
			return parser.readMeshes ();
		}

		private SSMD5MeshParser (SSAssetManager.Context ctx, string filename)
			: base(ctx, filename)
		{
		}

		private SSSkeletalMeshMD5[] readMeshes()
		{
			Match[] matches;
			seekEntry ("MD5Version", "10");
			seekEntry ("commandline", SSMD5Parser.c_nameRegex);

			matches = seekEntry ("numJoints", SSMD5Parser.c_uintRegex);
			var joints = new SSSkeletalJointBaseInfo[Convert.ToUInt32(matches[1].Value)];

			matches = seekEntry ( "numMeshes", SSMD5Parser.c_uintRegex);
			var meshes = new SSSkeletalMeshMD5[Convert.ToUInt32 (matches [1].Value)];

			seekEntry ("joints", "{");
			for (int j = 0; j < joints.Length; ++j) {
				joints[j] = readJoint();
			}
			seekEntry ("}");

			for (int m = 0; m < meshes.Length; ++m) {
				seekEntry ("mesh", "{");
				meshes [m] = readMesh (joints);
				meshes [m].Joints = joints;
				seekEntry ("}");
			}
			return meshes;
		}

		private SSSkeletalJointBaseInfo readJoint()
		{
			Match[] matches = seekEntry (
				SSMD5Parser.c_nameRegex, // joint name
				SSMD5Parser.c_intRegex,  // parent index
				SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose, // position
				SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose  // orientation			
			);
			SSSkeletalJointBaseInfo ret = new SSSkeletalJointBaseInfo();
			ret.Name = matches[0].Captures[0].Value;
			ret.ParentIndex = Convert.ToInt32(matches[1].Value);

			ret.BaseLocation.Position.X = (float)Convert.ToDouble(matches[3].Value);
			ret.BaseLocation.Position.Y = (float)Convert.ToDouble(matches[4].Value); 
			ret.BaseLocation.Position.Z = (float)Convert.ToDouble(matches[5].Value);

			ret.BaseLocation.Orientation.X = (float)Convert.ToDouble(matches[8].Value);
			ret.BaseLocation.Orientation.Y = (float)Convert.ToDouble(matches[9].Value); 
			ret.BaseLocation.Orientation.Z = (float)Convert.ToDouble(matches[10].Value);
			ret.BaseLocation.ComputeQuatW();
			return ret;
		}

		private SSSkeletalMeshMD5 readMesh(SSSkeletalJointBaseInfo[] joints)
		{
			SSSkeletalMeshMD5 newMesh = new SSSkeletalMeshMD5 ();

			Match[] matches;
			matches = seekEntry("shader", SSMD5Parser.c_nameRegex);
			newMesh.MaterialShaderString = matches[1].Value;

			matches = seekEntry ("numverts", SSMD5Parser.c_uintRegex);
			int numVertices = Convert.ToInt32 (matches [1].Value);
			newMesh.Vertices = new SSSkeletalVertex[numVertices];

			for (int v = 0; v < numVertices; ++v) {
				int vertexIndex;
				var vertex = readVertex (out vertexIndex);
				newMesh.Vertices [vertexIndex] = vertex;
			}

			matches = seekEntry ("numtris", SSMD5Parser.c_uintRegex);
			int numTris = Convert.ToUInt16 (matches [1].Value);
			newMesh.TriangleIndices = new UInt16[numTris * 3];
			for (int t = 0; t < numTris; ++t) {
				readTriangle (newMesh.TriangleIndices);
			}

			matches = seekEntry ("numweights", SSMD5Parser.c_uintRegex);
			int numWeights = Convert.ToInt32 (matches [1].Value);
			newMesh.Weights = new SSSkeletalWeight[numWeights];
			for (int w = 0; w < numWeights; ++w) {
				int weightIdx;
				SSSkeletalWeight weight = readWeight(out weightIdx);
				newMesh.Weights [weightIdx] = weight;
			}

			return newMesh;
		}

		private SSSkeletalVertex readVertex(out int vertexIndex)
		{
			Match[] matches = seekEntry(
				"vert",
				SSMD5Parser.c_uintRegex, // vertex index
				SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose, // texture coord
				SSMD5Parser.c_uintRegex, // weight start index
				SSMD5Parser.c_uintRegex // weight count
			);
			vertexIndex = Convert.ToInt32(matches[1].Value);
			SSSkeletalVertex ret;
			ret.TextureCoords.X = (float)Convert.ToDouble(matches[3].Value);
			ret.TextureCoords.Y = (float)Convert.ToDouble(matches[4].Value);
			ret.WeightStartIndex = Convert.ToInt32(matches[6].Value);
			ret.WeightCount = Convert.ToInt32(matches[7].Value);
			ret.Normal = Vector3.Zero;
			return ret;
		}

		private void readTriangle(UInt16[] triangleIndices)
		{
			Match[] matches;
			matches = seekEntry (
				"tri",
				SSMD5Parser.c_uintRegex, // triangle index
				SSMD5Parser.c_uintRegex, SSMD5Parser.c_uintRegex, SSMD5Parser.c_uintRegex // 3 vertex indices
			);
			UInt32 triIdx = Convert.ToUInt32 (matches [1].Value);
			UInt32 indexBaseIdx = triIdx * 3;
			triangleIndices [indexBaseIdx] = Convert.ToUInt16 (matches [2].Value);
			triangleIndices [indexBaseIdx+2] = Convert.ToUInt16 (matches [3].Value);
			triangleIndices [indexBaseIdx+1] = Convert.ToUInt16 (matches [4].Value);
		}

		private SSSkeletalWeight readWeight(out int weightIndex)
		{
			Match[] matches = seekEntry(
				"weight",
				SSMD5Parser.c_uintRegex, // weight index
				SSMD5Parser.c_uintRegex, // joint index
				SSMD5Parser.c_floatRegex, // bias
				SSMD5Parser.c_parOpen, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_floatRegex, SSMD5Parser.c_parClose // position
			);

			weightIndex = Convert.ToInt32(matches[1].Value);
			SSSkeletalWeight ret;
			ret.JointIndex = Convert.ToInt32(matches[2].Value);
			ret.Bias = (float)Convert.ToDouble(matches[3].Value);
			ret.Position.X = (float)Convert.ToDouble(matches[5].Value);
			ret.Position.Y = (float)Convert.ToDouble(matches[6].Value);
			ret.Position.Z = (float)Convert.ToDouble(matches[7].Value);
			return ret;
		}
	}
}

