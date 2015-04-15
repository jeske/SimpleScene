using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalMeshMD5
	{
		public static float ComputeQuatW(ref Vector3 quatXyz)
		{
			float t = 1f - quatXyz.X * quatXyz.X - quatXyz.Y * quatXyz.Y - quatXyz.Z * quatXyz.Z;
			return t < 0f ? 0f : -(float)Math.Sqrt(t);
		}

		#region from MD5 mesh
		protected string m_materialShaderString;

		protected SkeletalVertexMD5[] m_vertices = null;
		protected SkeletalWeightMD5[] m_weights = null;
		protected UInt16[] m_triangleIndices = null;
		protected SkeletalJoint[] m_joints = null;
		#endregion

		#region runtime only use
		protected Vector3[] m_baseNormals = null;
		#endregion

		public int NumJoints {
			get { return m_joints.Length; }
		}

		public int NumVertices {
			get { return m_vertices.Length; }
		}

		public int NumIndices {
			get { return m_triangleIndices.Length; }
		}

		public int NumTriangles {
			get { return m_triangleIndices.Length / 3; }
		}

		public ushort[] Indices {
			get { return m_triangleIndices; }
		}

		public void ComputeJointPosFromMD5Mesh(bool computeNormals=true)
		{
			for (int j = 0; j < NumJoints; ++j) {
				m_joints [j].CurrentLocation.Position = m_joints [j].Md5.BasePosition;

				Vector3 orient = m_joints [j].Md5.BaseOrientation;
				m_joints [j].CurrentLocation.Orientation.Xyz = orient;
				m_joints [j].CurrentLocation.Orientation.W = ComputeQuatW (ref orient);
			}

			if (computeNormals) {
				// TODO compute normals?

			}
		}

		public Vector3 ComputeVertexPos(int vertexIndex)
		{
			Vector3 currentPos = Vector3.Zero;
			SkeletalVertexMD5 vertex = m_vertices [vertexIndex];

			for (int w = 0; w < vertex.WeightCount; ++w) {
				SkeletalWeightMD5 weight = m_weights [vertex.WeightStartIndex + w];
				SkeletalJoint joint = m_joints [weight.JointIndex];

				Vector3 currWeightPos = Vector3.Transform (weight.Position, joint.CurrentLocation.Orientation); 
				currentPos += weight.Bias * (joint.CurrentLocation.Position + currWeightPos);
			}
			return currentPos;
		}

		public static SSSkeletalMeshMD5[] ReadMeshes(SSAssetManager.Context ctx, string filename)
		{
			StreamReader reader = ctx.OpenText (filename);
			System.Console.WriteLine ("Reading MD5 file: " + ctx.fullResourcePath(filename));

			Match[] matches;
			int lineIdx = 0;

			MD5Parser.seekEntry (reader, ref lineIdx, "MD5Version", "10");
			MD5Parser.seekEntry (reader, ref lineIdx, "commandline", MD5Parser.c_nameRegex);

			matches = MD5Parser.seekEntry (reader, ref lineIdx, "numJoints", MD5Parser.c_uintRegex);
			SkeletalJoint[] joints = new SkeletalJoint[Convert.ToUInt32(matches[1].Value)];

			matches = MD5Parser.seekEntry (reader, ref lineIdx, "numMeshes", MD5Parser.c_uintRegex);
			SSSkeletalMeshMD5[] meshes = new SSSkeletalMeshMD5[Convert.ToUInt32 (matches [1].Value)];

			MD5Parser.seekEntry (reader, ref lineIdx, "joints", "{");
			for (int j = 0; j < joints.Length; ++j) {
				joints [j] = new SkeletalJoint ();
				joints[j].Md5 = new SkeletalJointMD5(reader, ref lineIdx);
			}
			MD5Parser.seekEntry (reader, ref lineIdx, "}");

			// TODO Build meshes
			for (int m = 0; m < meshes.Length; ++m) {
				meshes [m] = new SSSkeletalMeshMD5 (reader, ref lineIdx);
				meshes [m].m_joints = joints;
			}

			foreach (SSSkeletalMeshMD5 mesh in meshes) {
				mesh.ComputeJointPosFromMD5Mesh ();
			}
			return meshes;
		}

		public SSSkeletalMeshMD5 (StreamReader reader, ref int lineIdx)
		{
			MD5Parser.seekEntry(reader, ref lineIdx, "mesh", "{");

			Match[] matches;
			matches = MD5Parser.seekEntry(reader, ref lineIdx, "shader", MD5Parser.c_nameRegex);
			m_materialShaderString = matches[1].Value;

			matches = MD5Parser.seekEntry (reader, ref lineIdx, "numverts", MD5Parser.c_uintRegex);
			m_vertices = new SkeletalVertexMD5[Convert.ToUInt32(matches[1].Value)];

			for (int v = 0; v < m_vertices.Length; ++v) {
				int vertexIndex;
				var vertex = new SkeletalVertexMD5 (reader, ref lineIdx, out vertexIndex);
				m_vertices [vertexIndex] = vertex;
			}

			matches = MD5Parser.seekEntry (reader, ref lineIdx, "numtris", MD5Parser.c_uintRegex);
			int numTris = Convert.ToUInt16 (matches [1].Value);
			m_triangleIndices = new UInt16[numTris * 3];
			for (int t = 0; t < numTris; ++t) {
				readTri (reader, ref lineIdx);
			}

			matches = MD5Parser.seekEntry (reader, ref lineIdx, "numweights", MD5Parser.c_uintRegex);
			int numWeights = Convert.ToInt32 (matches [1].Value);
			m_weights = new SkeletalWeightMD5[numWeights];
			for (int w = 0; w < m_weights.Length; ++w) {
				int weightIdx;
				SkeletalWeightMD5 weight = new SkeletalWeightMD5 (reader, ref lineIdx, out weightIdx);
				m_weights [weightIdx] = weight;
			}
		}

		public Vector2 TextureCoords(int vertexIndex)
		{
			return m_vertices [vertexIndex].TextureCoords;
		}

		private void readTri(StreamReader reader, ref int lineIdx)
		{
			Match[] matches;
			matches = MD5Parser.seekEntry (reader, ref lineIdx,
				"tri",
				MD5Parser.c_uintRegex, // triangle index
				MD5Parser.c_uintRegex, MD5Parser.c_uintRegex, MD5Parser.c_uintRegex // 3 vertex indices
			);
			UInt32 triIdx = Convert.ToUInt32 (matches [1].Value);
			UInt32 indexBaseIdx = triIdx * 3;
			m_triangleIndices [indexBaseIdx] = Convert.ToUInt16 (matches [2].Value);
			m_triangleIndices [indexBaseIdx+1] = Convert.ToUInt16 (matches [3].Value);
			m_triangleIndices [indexBaseIdx+2] = Convert.ToUInt16 (matches [4].Value);
		}

		private static class MD5Parser
		{
			// TODO fix quotes not getting discarded
			public static readonly string c_nameRegex = @"(?<="")[^\""]*(?="")";
			public static readonly string c_uintRegex = @"^(\d+)$";
			public static readonly string c_intRegex = @"^(-*\d+)$";
			public static readonly string c_floatRegex = @"(-*\d*\.\d*[Ee]*-*\d*)";
			public static readonly string c_parOpen = @"\(";
			public static readonly string c_parClose = @"\)";

			private static Dictionary<string, Regex> s_regexCache = new Dictionary<string, Regex>();

			static public Match[] seekEntry(StreamReader reader, ref int lineIdx, params string[] wordRegExStrArray)
			{
				Regex[] regexArray = new Regex[wordRegExStrArray.Length];
				for (int w = 0; w < wordRegExStrArray.Length; ++w) {
					string regExStr = wordRegExStrArray [w];
					Regex regex = null;
					if (!s_regexCache.TryGetValue(regExStr, out regex)) {
						regex = new Regex (regExStr);
						s_regexCache.Add (regExStr, regex);
					}
					regexArray [w] = regex;
				}
				return seekRegexEntry (reader, ref lineIdx, regexArray, wordRegExStrArray);
			}

			private static Match[] seekRegexEntry(StreamReader reader, ref int lineIdx, Regex[] wordRegEx, string[] wordRegExStr)
			{
				char[] wordDelimeters = { ' ', '\t' };

				string line;

				while ((line = reader.ReadLine ()) != null) {
					int commentsIdx = line.IndexOf ("//");
					if (commentsIdx >= 0) {
						line = line.Substring (0, commentsIdx);
					}

					string[] words = line.Split (wordDelimeters);
					if (words.Length > 1 || (words.Length == 1 && words[0].Length > 0)) {

						// combine words when in brackets
						bool openBracket = false;
						var adjustedWords = new List<string> ();
						for (int w = 0; w < words.Length; ++w) {
							string word = words [w];
							if (word.Length > 0) {
								if (openBracket) {
									adjustedWords [adjustedWords.Count - 1] += " ";
									adjustedWords [adjustedWords.Count - 1] += word;
									if (word [word.Length - 1] == '\"') {
										openBracket = false;
									}
								} else {
									adjustedWords.Add (word);
									if (word[0] == '\"' 
										&& (word.Length == 0 || word[word.Length-1] != '\"')) {
										openBracket = true;
									}
								}
							}
						}


						if (adjustedWords.Count != wordRegEx.Length) {
							entryFailure (lineIdx, line, wordRegExStr);
						}

						Match[] ret = new Match[wordRegEx.Length];
						for (int w = 0; w < wordRegEx.Length; ++w) {
							ret [w] = wordRegEx [w].Match (adjustedWords [w]);
							if (!ret [w].Success) {
								entryFailure (lineIdx, line, wordRegExStr);
							}
						}
						lineIdx++;
						return ret;
					}
					lineIdx++;
				}
				entryFailure (lineIdx, "EOF", wordRegExStr);
				return null;
			}

			private static void entryFailure(int lineIdx, string line, string[] regexStr)
			{
				string expectingStr = "";
				for (int r = 0; r < regexStr.Length; ++r) {
					expectingStr += regexStr [r] + ' ';
				}

				string errorStr = String.Format (
					"Failed to read MD5: line {0}: {1} *** Expecting: {2}",
					lineIdx, line, expectingStr);
				System.Console.WriteLine (errorStr);
				throw new Exception (errorStr);
			}
		}

		public struct SkeletalVertexMD5
		{
			#region from MD5 mesh
			//public int VertexIndex;
			public Vector2 TextureCoords;
			public int WeightStartIndex;
			public int WeightCount;
			#endregion

			public SkeletalVertexMD5(StreamReader reader, ref int lineIdx, out int vertexIndex)
			{
				Match[] matches
				= MD5Parser.seekEntry(reader, ref lineIdx,
					"vert",
					MD5Parser.c_uintRegex, // vertex index
					MD5Parser.c_parOpen, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_parClose, // texture coord
					MD5Parser.c_uintRegex, // weight start index
					MD5Parser.c_uintRegex // weight count
				);
				vertexIndex = Convert.ToInt32(matches[1].Value);
				TextureCoords.X = (float)Convert.ToDouble(matches[3].Value);
				TextureCoords.Y = (float)Convert.ToDouble(matches[4].Value);
				WeightStartIndex = Convert.ToInt32(matches[6].Value);
				WeightCount = Convert.ToInt32(matches[7].Value);
			}
		}

		public struct SkeletalWeightMD5
		{
			#region from MD5 mesh
			//public int WeightIndex;
			public int JointIndex;
			public float Bias;
			public Vector3 Position;
			#endregion

			public SkeletalWeightMD5(StreamReader reader, ref int lineIdx, out int weightIndex)
			{
				Match[] matches
				= MD5Parser.seekEntry(reader, ref lineIdx,
					"weight",
					MD5Parser.c_uintRegex, // weight index
					MD5Parser.c_uintRegex, // joint index
					MD5Parser.c_floatRegex, // bias
					MD5Parser.c_parOpen, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_parClose // position
				);

				weightIndex = Convert.ToInt32(matches[1].Value);
				JointIndex = Convert.ToInt32(matches[2].Value);
				Bias = (float)Convert.ToDouble(matches[3].Value);
				Position.X = (float)Convert.ToDouble(matches[5].Value);
				Position.Y = (float)Convert.ToDouble(matches[6].Value);
				Position.Z = (float)Convert.ToDouble(matches[7].Value);
			}
		}

		public struct SkeletalJointMD5
		{
			#region from MD5 mesh
			public string Name;
			public int ParentIndex;
			public Vector3 BasePosition;
			public Vector3 BaseOrientation;
			#endregion

			public SkeletalJointMD5 (StreamReader reader, ref int lineIdx)
			{
				Match[] matches = MD5Parser.seekEntry (reader, ref lineIdx, 
					MD5Parser.c_nameRegex, // joint name
					MD5Parser.c_intRegex,  // parent index
					MD5Parser.c_parOpen, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_parClose, // position
					MD5Parser.c_parOpen, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_parClose  // orientation			
				);
				Name = matches[0].Captures[0].Value;
				ParentIndex = Convert.ToInt32(matches[1].Value);

				BasePosition.X = (float)Convert.ToDouble(matches[3].Value);
				BasePosition.Y = (float)Convert.ToDouble(matches[4].Value); 
				BasePosition.Z = (float)Convert.ToDouble(matches[5].Value);

				BaseOrientation.X = (float)Convert.ToDouble(matches[8].Value);
				BaseOrientation.Y = (float)Convert.ToDouble(matches[9].Value); 
				BaseOrientation.Z = (float)Convert.ToDouble(matches[10].Value);
			}
		}

		public struct SkeletalJointLocation
		{
			public Vector3 Position;
			public Quaternion Orientation;
		}

		public class SkeletalJoint
		{
			public SkeletalJointMD5 Md5;
			public SkeletalJointLocation CurrentLocation;

			public SkeletalJointLocation[] Frames = null;
		}
	}
}

