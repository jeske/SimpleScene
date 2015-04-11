using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using OpenTK;

namespace SimpleScene
{
	public struct SkeletalVertexMD5
	{
		#region from MD5 mesh
		public int VertexIndex;
		public Vector2 TextureCoords;
		public int WeightStartIndex;
		public int WeightCount;
		#endregion
	}

	public struct SkeletalWeightMD5
	{
		#region from MD5 mesh
		public int WeightIndex;
		public int JointIndex;
		public float Bias;
		public Vector3 Position;
		#endregion
	}

	public struct SkeletalJointMD5
	{
		#region from MD5 mesh
		public int ParentIndex;
		public string Name;
		public Vector3 BasePosition;
		public Vector3 BaseOrientation;
		#endregion
	}

	public struct SkeletalJointLocation
	{
		public Vector3 Position;
		public Quaternion Orientation;
	}

	public class SkeletalJoint
	{
		public SkeletalJointMD5 Md5;
		public SkeletalJointLocation Current;

		public SkeletalJointLocation[] Frames = null;
	}

	public class SSSkeletalMeshMD5
	{
		public static float ComputeQuatW(ref Vector3 quatXyz)
		{
			float t = 1f - quatXyz.X * quatXyz.X - quatXyz.Y * quatXyz.Y - quatXyz.Z * quatXyz.Z;
			return t < 0f ? 0f : -(float)Math.Sqrt(t);
		}

		#region from MD5 mesh
		protected string MaterialShaderString;

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
				m_joints [j].Current.Position = m_joints [j].Md5.BasePosition;

				Vector3 orient = m_joints [j].Md5.BaseOrientation;
				m_joints [j].Current.Orientation.Xyz = orient;
				m_joints [j].Current.Orientation.W = ComputeQuatW (ref orient);
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

				Vector3 currWeightPos = Vector3.Transform (weight.Position, joint.Current.Orientation); 
				currentPos += weight.Bias * (joint.Current.Position + currWeightPos);
			}
			return currentPos;
		}

		public Vector2 TextureCoords(int vertexIndex)
		{
			return m_vertices [vertexIndex].TextureCoords;
		}

		public static SSSkeletalMeshMD5[] ReadMeshes(SSAssetManager.Context ctx, string filename)
		{
			string fullFilename = ctx.fullResourcePath (filename);
			StreamReader reader = File.OpenText (fullFilename);
			System.Console.WriteLine ("Reading MD5 file: " + fullFilename);

			Match[] matches;
			seekEntry (reader, "MD5Version", "10");
			seekEntry (reader, "commandline", @"""[A-Za-z0-9 _]""");

			matches = seekEntry (reader, "numJoints", @"(\d+)");
			SkeletalJoint[] joints = new SkeletalJoint[Convert.ToInt32(matches[1].Value)];

			matches = seekEntry (reader, "numMeshes", @"(\d+)");
			SSSkeletalMeshMD5[] meshes = new SSSkeletalMeshMD5[Convert.ToInt32 (matches [1].Value)];

			seekEntry (reader, "joints {");

			foreach (SSSkeletalMeshMD5 mesh in meshes) {
				mesh.ComputeJointPosFromMD5Mesh ();
			}
			return meshes;
		}



		static private Match[] seekEntry(StreamReader reader, params string[] wordRegExStr)
		{
			Regex[] regex = new Regex[wordRegExStr.Length];
			for (int w = 0; w < wordRegExStr.Length; ++w) {
				regex[w] = new Regex (wordRegExStr[w]);
			}
			return seekEntry (reader, regex);
		}

		private static Match[] seekEntry(StreamReader reader, params Regex[] wordRegEx)
		{
			char[] wordDelimeters = { ' ', '\t' };

			string line;
			int lineIdx = 0;

			while ((line = reader.ReadLine ()) != null) {
				int commentsIdx = line.IndexOf ("//");
				if (commentsIdx > 0) {
					line = line.Substring (0, commentsIdx);
				}

				string[] words = line.Split (wordDelimeters);
				if (words.Length > 0) {

					// combine words when in brackets
					bool openBracket = false;
					var adjustedWords = new List<string> ();
					for (int w = 0; w < words.Length; ++w) {
						string word = words [w];
						if (word.Length > 0) {
							if (openBracket) {
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
						entryFailure (lineIdx, line, wordRegEx);
					}

					Match[] ret = new Match[wordRegEx.Length];
					for (int w = 0; w < wordRegEx.Length; ++w) {
						ret [w] = wordRegEx [w].Match (adjustedWords [w]);
						if (!ret [w].Success) {
							entryFailure (lineIdx, line, wordRegEx);
						}
					}
					return ret;
				}
				lineIdx++;
			}
			entryFailure (lineIdx, "EOF", wordRegEx);
			return null;
		}

		private static void entryFailure(int lineIdx, string line, Regex[] regex)
		{
			string errorStr = String.Format (
				"Failed to read MD5: line {0}: {1} *** Expecting {2}",
				lineIdx, line, regex.ToString());
			System.Console.WriteLine (errorStr);
			throw new Exception (errorStr);
		}


	}
}

