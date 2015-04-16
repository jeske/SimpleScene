using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalMeshMD5
	{
		#region from MD5 mesh
		protected string m_materialShaderString;

		protected SkeletalVertexMD5[] m_vertices = null;
		protected SkeletalWeightMD5[] m_weights = null;
		protected UInt16[] m_triangleIndices = null;
		protected SkeletalJoint[] m_joints = null;
		protected SSTexture m_mainTexture = null;
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

		public SSTexture MainTexture {
			get { return m_mainTexture; }
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

		public Vector3 ComputeVertexNormal(int vertexIndex)
		{
			SkeletalVertexMD5 vertex = m_vertices [vertexIndex];
			Vector3 currentNormal = Vector3.Zero;

			for (int w = 0; w < vertex.WeightCount; ++w) {
				SkeletalWeightMD5 weight = m_weights [vertex.WeightStartIndex + w];
				SkeletalJoint joint = m_joints [weight.JointIndex];
				currentNormal += weight.Bias * Vector3.Transform (vertex.Normal, joint.CurrentLocation.Orientation);
			}
			return currentNormal;
		}

		/// <summary>
		/// Precompute normals in joint-local space
		/// http://3dgep.com/loading-and-animating-md5-models-with-opengl/#The_MD5Mesh::PrepareNormals_Method
		/// </summary>
		private void PreComputeNormals()
		{
			for (int i = 0; i < m_triangleIndices.Length; ++i) {
				int baseIdx = i * 3;
				int v0 = m_triangleIndices [baseIdx];
				int v1 = m_triangleIndices [baseIdx + 1];
				int v2 = m_triangleIndices [baseIdx + 2];
				Vector3 p0 = ComputeVertexPos (v0);
				Vector3 p1 = ComputeVertexPos (v1);
				Vector3 p2 = ComputeVertexPos (v2);
				Vector3 normal = Vector3.Cross (p2 - p0, p1 - p0);
				m_vertices [v0].Normal += normal;
				m_vertices [v1].Normal += normal;
				m_vertices [v1].Normal += normal;
			}

			for (int v = 0; v < m_vertices.Length; ++v) {
				// Normalize
				Vector3 normal = m_vertices [v].Normal.Normalized ();

				// Reset for the next step
				m_vertices [v].Normal = Vector3.Zero;

				// Put the bind-pose normal into joint-local space
				// so the animated normal can be computed faster later
				for (int w = 0; w < m_vertices [v].WeightCount; ++w) {
					SkeletalWeightMD5 weight = m_weights [m_vertices [v].WeightStartIndex + w];
					SkeletalJoint joint = m_joints [weight.JointIndex];
					m_vertices [v].Normal 
					+= Vector3.Transform (normal, joint.Md5.BaseLocation.Orientation.Inverted()) * weight.Bias;
				}
			}
		}


		public static SSSkeletalMeshMD5[] ReadMeshes(SSAssetManager.Context ctx, string filename)
		{
			var parser = new MD5Parser(ctx, filename);
			Match[] matches;
			parser.seekEntry ("MD5Version", "10");
			parser.seekEntry ("commandline", MD5Parser.c_nameRegex);

			matches = parser.seekEntry ("numJoints", MD5Parser.c_uintRegex);
			var joints = new SkeletalJointMD5[Convert.ToUInt32(matches[1].Value)];

			matches = parser.seekEntry ( "numMeshes", MD5Parser.c_uintRegex);
			SSSkeletalMeshMD5[] meshes = new SSSkeletalMeshMD5[Convert.ToUInt32 (matches [1].Value)];

			parser.seekEntry ("joints", "{");
			for (int j = 0; j < joints.Length; ++j) {
				joints[j] = new SkeletalJointMD5(parser);
			}
			parser.seekEntry ("}");

			for (int m = 0; m < meshes.Length; ++m) {
				meshes [m] = new SSSkeletalMeshMD5 (joints, parser);
			}
			return meshes;
		}

		public SSSkeletalMeshMD5 (SkeletalJointMD5[] joints, MD5Parser parser)
		{
			parser.seekEntry("mesh", "{");

			Match[] matches;
			matches = parser.seekEntry("shader", MD5Parser.c_nameRegex);
			m_materialShaderString = matches[1].Value;
			m_mainTexture = SSAssetManager.GetInstance<SSTexture> (parser.Context, m_materialShaderString);

			matches = parser.seekEntry ("numverts", MD5Parser.c_uintRegex);
			m_vertices = new SkeletalVertexMD5[Convert.ToUInt32(matches[1].Value)];

			for (int v = 0; v < m_vertices.Length; ++v) {
				int vertexIndex;
				var vertex = new SkeletalVertexMD5 (parser, out vertexIndex);
				m_vertices [vertexIndex] = vertex;
			}

			matches = parser.seekEntry ("numtris", MD5Parser.c_uintRegex);
			int numTris = Convert.ToUInt16 (matches [1].Value);
			m_triangleIndices = new UInt16[numTris * 3];
			for (int t = 0; t < numTris; ++t) {
				readTriangle (parser);
			}

			matches = parser.seekEntry ("numweights", MD5Parser.c_uintRegex);
			int numWeights = Convert.ToInt32 (matches [1].Value);
			m_weights = new SkeletalWeightMD5[numWeights];
			for (int w = 0; w < m_weights.Length; ++w) {
				int weightIdx;
				SkeletalWeightMD5 weight = new SkeletalWeightMD5 (parser,  out weightIdx);
				m_weights [weightIdx] = weight;
			}

			m_joints = new SkeletalJoint[joints.Length];
			for (int j = 0; j < joints.Length; ++j) {
				m_joints[j] = new SkeletalJoint (joints [j]);
			}
		}

		public Vector2 TextureCoords(int vertexIndex)
		{
			return m_vertices [vertexIndex].TextureCoords;
		}

		private void readTriangle(MD5Parser parser)
		{
			Match[] matches;
			matches = parser.seekEntry (
				"tri",
				MD5Parser.c_uintRegex, // triangle index
				MD5Parser.c_uintRegex, MD5Parser.c_uintRegex, MD5Parser.c_uintRegex // 3 vertex indices
			);
			UInt32 triIdx = Convert.ToUInt32 (matches [1].Value);
			UInt32 indexBaseIdx = triIdx * 3;
			m_triangleIndices [indexBaseIdx] = Convert.ToUInt16 (matches [2].Value);
			m_triangleIndices [indexBaseIdx+2] = Convert.ToUInt16 (matches [3].Value);
			m_triangleIndices [indexBaseIdx+1] = Convert.ToUInt16 (matches [4].Value);
		}

		public class MD5Parser
		{
			public static readonly string c_nameRegex = @"(?<="")[^\""]*(?="")";
			public static readonly string c_uintRegex = @"(\d+)";
			public static readonly string c_intRegex = @"(-*\d+)";
			public static readonly string c_floatRegex = @"(-*\d*\.\d*[Ee]*-*\d*)";
			public static readonly string c_parOpen = @"\(";
			public static readonly string c_parClose = @"\)";

			private static readonly char[] c_wordDelimeters = {' ', '\t' };

			private Dictionary<string, Regex> m_regexCache = new Dictionary<string, Regex>();
			private int m_lineIdx = 0;
			private StreamReader m_reader;
			private SSAssetManager.Context m_ctx;

			public SSAssetManager.Context Context {
				get { return m_ctx; }
			}

			public MD5Parser(SSAssetManager.Context ctx, string filename)
			{
				m_ctx = ctx;
				m_reader = ctx.OpenText (filename);
				System.Console.WriteLine ("Reading MD5 file: " + ctx.fullResourcePath(filename));
			}

			public Match[] seekEntry(params string[] wordRegExStrArray)
			{
				Regex[] regexArray = new Regex[wordRegExStrArray.Length];
				for (int w = 0; w < wordRegExStrArray.Length; ++w) {
					string regExStr = wordRegExStrArray [w];
					Regex regex = null;
					if (!m_regexCache.TryGetValue(regExStr, out regex)) {
						regex = new Regex (regExStr);
						m_regexCache.Add (regExStr, regex);
					}
					regexArray [w] = regex;
				}
				return seekRegexEntry (regexArray, wordRegExStrArray);
			}

			private Match[] seekRegexEntry(Regex[] wordRegEx, string[] wordRegExStr)
			{
				string line;

				while ((line = m_reader.ReadLine ()) != null) {
					int commentsIdx = line.IndexOf ("//");
					if (commentsIdx >= 0) {
						line = line.Substring (0, commentsIdx);
					}

					string[] words = line.Split (c_wordDelimeters);
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
										&& (word.Length == 1 || word[word.Length-1] != '\"')) {
										openBracket = true;
									}
								}
							}
						}

						if (adjustedWords.Count == 0) continue;

						if (adjustedWords.Count != wordRegEx.Length) {
							entryFailure (line, wordRegExStr);
						}

						Match[] ret = new Match[wordRegEx.Length];
						for (int w = 0; w < wordRegEx.Length; ++w) {
							ret [w] = wordRegEx [w].Match (adjustedWords [w]);
							if (!ret [w].Success) {
								entryFailure (line, wordRegExStr);
							}
						}
						m_lineIdx++;
						return ret;
					}
					m_lineIdx++;
				}
				entryFailure ("EOF", wordRegExStr);
				return null;
			}

			private void entryFailure(string line, string[] regexStr)
			{
				string expectingStr = "";
				for (int r = 0; r < regexStr.Length; ++r) {
					expectingStr += regexStr [r] + ' ';
				}

				string errorStr = String.Format (
					"Failed to read MD5: line {0}: {1} *** Expecting: {2}",
					m_lineIdx, line, expectingStr);
				System.Console.WriteLine (errorStr);
				throw new Exception (errorStr);
			}
		}

		protected struct SkeletalVertexMD5
		{
			#region from MD5 mesh
			//public int VertexIndex;
			public Vector2 TextureCoords;
			public Vector3 Normal;
			public int WeightStartIndex;
			public int WeightCount;
			#endregion

			public SkeletalVertexMD5(MD5Parser parser, out int vertexIndex)
			{
				Match[] matches
				= parser.seekEntry(
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
				Normal = Vector3.Zero;
			}
		}

		protected struct SkeletalWeightMD5
		{
			#region from MD5 mesh
			//public int WeightIndex;
			public int JointIndex;
			public float Bias;
			public Vector3 Position;
			#endregion

			public SkeletalWeightMD5(MD5Parser parser, out int weightIndex)
			{
				Match[] matches
				= parser.seekEntry(
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

		public struct SkeletalJointLocation
		{
			public Vector3 Position;
			public Quaternion Orientation;

			public void ComputeQuatW()
			{
				float t = 1f - Orientation.X * Orientation.X 
							 - Orientation.Y * Orientation.Y 
							 - Orientation.Z * Orientation.Z;
				Orientation.W = t < 0f ? 0f : -(float)Math.Sqrt(t);
			}
		}

		public class SkeletalJointMD5
		{
			#region from MD5 mesh
			public string Name;
			public int ParentIndex;
			public SkeletalJointLocation BaseLocation;
			#endregion

			public SkeletalJointMD5 (MD5Parser parser)
			{
				Match[] matches = parser.seekEntry (
					MD5Parser.c_nameRegex, // joint name
					MD5Parser.c_intRegex,  // parent index
					MD5Parser.c_parOpen, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_parClose, // position
					MD5Parser.c_parOpen, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_floatRegex, MD5Parser.c_parClose  // orientation			
				);
				Name = matches[0].Captures[0].Value;
				ParentIndex = Convert.ToInt32(matches[1].Value);

				BaseLocation.Position.X = (float)Convert.ToDouble(matches[3].Value);
				BaseLocation.Position.Y = (float)Convert.ToDouble(matches[4].Value); 
				BaseLocation.Position.Z = (float)Convert.ToDouble(matches[5].Value);

				BaseLocation.Orientation.X = (float)Convert.ToDouble(matches[8].Value);
				BaseLocation.Orientation.Y = (float)Convert.ToDouble(matches[9].Value); 
				BaseLocation.Orientation.Z = (float)Convert.ToDouble(matches[10].Value);
				BaseLocation.ComputeQuatW();
			}
		}

		public class SkeletalJoint
		{
			protected SkeletalJointMD5 m_md5;
			protected SkeletalJointLocation m_currentLocation;
			protected SkeletalJointLocation[] Frames = null;

			public SkeletalJointLocation CurrentLocation {
				get { return m_currentLocation; }
			}

			public SkeletalJointMD5 Md5 {
				get { return m_md5; }
			}

			public SkeletalJoint(SkeletalJointMD5 md5)
			{
				m_md5 = md5;
				ComputeLocationFromMd5();
			}

			public void ComputeLocationFromMd5()
			{
				m_currentLocation = m_md5.BaseLocation;
			}
		}
	}
}

