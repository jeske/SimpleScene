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
			seekEntry ("commandline", SSMD5Parser._quotedStrRegex);

			matches = seekEntry ("numJoints", SSMD5Parser._uintRegex);
			var joints = new SSSkeletalJoint[Convert.ToUInt32(matches[1].Value)];

			matches = seekEntry ( "numMeshes", SSMD5Parser._uintRegex);
			var meshes = new SSSkeletalMeshMD5[Convert.ToUInt32 (matches [1].Value)];

			seekEntry ("joints", "{");
			for (int j = 0; j < joints.Length; ++j) {
				joints[j] = readJoint();
				joints [j].jointIndex = j;
			}
			seekEntry ("}");
			transformBindPoseToJointLocal (joints);

			for (int m = 0; m < meshes.Length; ++m) {
				seekEntry ("mesh", "{");
				meshes [m] = readMesh (joints);
				meshes [m].joints = joints;
				seekEntry ("}");

				meshes [m].assetContext = Context;
			}
			return meshes;
		}

		private SSSkeletalJoint readJoint()
		{
			Match[] matches = seekEntry (
				SSMD5Parser._quotedStrRegex, // joint name
				SSMD5Parser._intRegex,  // parent index
				SSMD5Parser._parOpen, 
					SSMD5Parser._floatRegex, 
					SSMD5Parser._floatRegex, 
					SSMD5Parser._floatRegex, 
				SSMD5Parser._parClose, // position
				SSMD5Parser._parOpen, 
					SSMD5Parser._floatRegex, 
					SSMD5Parser._floatRegex, 
					SSMD5Parser._floatRegex, 
				SSMD5Parser._parClose  // orientation			
			);
			SSSkeletalJoint ret = new SSSkeletalJoint();
			ret.name = matches[0].Captures[0].Value;
			ret.parentIndex = Convert.ToInt32(matches[1].Value);

			ret.bindPoseLocation.position.X = (float)Convert.ToDouble(matches[3].Value);
			ret.bindPoseLocation.position.Y = (float)Convert.ToDouble(matches[4].Value); 
			ret.bindPoseLocation.position.Z = (float)Convert.ToDouble(matches[5].Value);

			ret.bindPoseLocation.orientation.X = (float)Convert.ToDouble(matches[8].Value);
			ret.bindPoseLocation.orientation.Y = (float)Convert.ToDouble(matches[9].Value); 
			ret.bindPoseLocation.orientation.Z = (float)Convert.ToDouble(matches[10].Value);
			ret.bindPoseLocation.computeQuatW();
			return ret;
		}

		/// <summary>
		/// Transform bind pose coordinates from mesh global form into joint-local form
		/// </summary>
		private static void transformBindPoseToJointLocal(SSSkeletalJoint[] joints)
		{
			for (int j = joints.Length-1; j > 0; --j) {
				var joint = joints [j];
				var parLoc = joints [joint.parentIndex].bindPoseLocation;
				joint.bindPoseLocation.undoPrecedingTransform (parLoc);
			}
		}

		private SSSkeletalMeshMD5 readMesh(SSSkeletalJoint[] joints)
		{
			SSSkeletalMeshMD5 newMesh = new SSSkeletalMeshMD5 ();

			Match[] matches;
			matches = seekEntry("shader", SSMD5Parser._quotedStrRegex);
			newMesh.materialShaderString = matches[1].Value;

			matches = seekEntry ("numverts", SSMD5Parser._uintRegex);
			int numVertices = Convert.ToInt32 (matches [1].Value);
			newMesh.vertices = new SSSkeletalVertex[numVertices];

			for (int v = 0; v < numVertices; ++v) {
				int vertexIndex;
				var vertex = readVertex (out vertexIndex);
				newMesh.vertices [vertexIndex] = vertex;
			}

			matches = seekEntry ("numtris", SSMD5Parser._uintRegex);
			int numTris = Convert.ToUInt16 (matches [1].Value);
			newMesh.triangleIndices = new UInt16[numTris * 3];
			for (int t = 0; t < numTris; ++t) {
				readTriangle (newMesh.triangleIndices);
			}

			matches = seekEntry ("numweights", SSMD5Parser._uintRegex);
			int numWeights = Convert.ToInt32 (matches [1].Value);
			newMesh.weights = new SSSkeletalWeight[numWeights];
			for (int w = 0; w < numWeights; ++w) {
				int weightIdx;
				SSSkeletalWeight weight = readWeight(out weightIdx);
				newMesh.weights [weightIdx] = weight;
			}

			return newMesh;
		}

		private SSSkeletalVertex readVertex(out int vertexIndex)
		{
			Match[] matches = seekEntry(
				"vert",
				SSMD5Parser._uintRegex, // vertex index
				SSMD5Parser._parOpen, SSMD5Parser._floatRegex, SSMD5Parser._floatRegex, SSMD5Parser._parClose, // texture coord
				SSMD5Parser._uintRegex, // weight start index
				SSMD5Parser._uintRegex // weight count
			);
			vertexIndex = Convert.ToInt32(matches[1].Value);
			SSSkeletalVertex ret;
			ret.textureCoords.X = (float)Convert.ToDouble(matches[3].Value);
			ret.textureCoords.Y = (float)Convert.ToDouble(matches[4].Value);
			ret.weightStartIndex = Convert.ToInt32(matches[6].Value);
			ret.weightCount = Convert.ToInt32(matches[7].Value);
			return ret;
		}

		private void readTriangle(UInt16[] triangleIndices)
		{
			Match[] matches;
			matches = seekEntry (
				"tri",
				SSMD5Parser._uintRegex, // triangle index
				SSMD5Parser._uintRegex, SSMD5Parser._uintRegex, SSMD5Parser._uintRegex // 3 vertex indices
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
				SSMD5Parser._uintRegex, // weight index
				SSMD5Parser._uintRegex, // joint index
				SSMD5Parser._floatRegex, // bias
				SSMD5Parser._parOpen, SSMD5Parser._floatRegex, SSMD5Parser._floatRegex, SSMD5Parser._floatRegex, SSMD5Parser._parClose // position
			);

			weightIndex = Convert.ToInt32(matches[1].Value);
			SSSkeletalWeight ret;
			ret.jointIndex = Convert.ToInt32(matches[2].Value);
			ret.bias = (float)Convert.ToDouble(matches[3].Value);
			ret.position.X = (float)Convert.ToDouble(matches[5].Value);
			ret.position.Y = (float)Convert.ToDouble(matches[6].Value);
			ret.position.Z = (float)Convert.ToDouble(matches[7].Value);
			return ret;
		}
	}
}

