using System;
using System.Text.RegularExpressions;
using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalAnimationMD5
	{
		public enum LocationFlags : byte {
			Tx = 1, Ty = 2, Tz = 4,
			Qx = 8, Qy = 16, Qz = 32
		}

		protected int m_numFrames;
		protected int m_numJoints;
		protected int m_frameRate;

		protected SSAABB[] m_bounds;
		protected HierarchyEntryMD5[] m_hierarchy;
		protected SSSkeletalJointLocation[] m_baseFrames;
		protected SSSkeletalJointLocation[][] m_frames;

		// temp use
		protected float[] m_floatComponents;

		public int NumFrames {
			get { return m_numFrames; }
		}

		public int NumJoints {
			get { return m_numJoints; }
		}

		public int FrameRate {
			get { return m_frameRate; }
		}

		public float FramePeriod {
			get { return 1f / (float)m_frameRate; }
		}

		public SSAABB[] Bounds {
			get { return m_bounds; }
		}

		public HierarchyEntryMD5[] JointHierarchy {
			get { return m_hierarchy; }
		}

		public SSSkeletalAnimationMD5 (SSAssetManager.Context ctx, string filename)
		{
			var parser = new SSMD5Parser (ctx, filename);
			Match[] matches;

			// header
			parser.seekEntry ("MD5Version", "10");
			parser.seekEntry ("commandline", SSMD5Parser.c_nameRegex);

			matches = parser.seekEntry ("numFrames", SSMD5Parser.c_uintRegex);
			m_numFrames = Convert.ToInt32 (matches [1].Value);

			matches = parser.seekEntry ("numJoints", SSMD5Parser.c_uintRegex);
			m_numJoints = Convert.ToInt32 (matches [1].Value);

			matches = parser.seekEntry ("frameRate", SSMD5Parser.c_uintRegex);
			m_frameRate = Convert.ToInt32 (matches [1].Value);

			matches = parser.seekEntry ("numAnimatedComponents", SSMD5Parser.c_uintRegex);
			int numAnimatedComponents = Convert.ToInt32 (matches [1].Value);
			m_floatComponents = new float[numAnimatedComponents];

			// hierarchy
			parser.seekEntry ("hierarchy", "{");
			m_hierarchy = new HierarchyEntryMD5[m_numJoints];
			for (int j = 0; j < m_numJoints; ++j) {
				m_hierarchy [j] = new HierarchyEntryMD5 (parser);
			}
			parser.seekEntry ("}");

			// bounds
			parser.seekEntry ("bounds", "{");
			m_bounds = new SSAABB[m_numFrames];
			for (int f = 0; f < m_numFrames; ++f) {
				m_bounds [f] = readBounds (parser);
			}
			parser.seekEntry ("}");

			// base frame
			parser.seekEntry ("baseframe", "{");
			m_baseFrames = new SSSkeletalJointLocation[m_numJoints];
			for (int j = 0; j < m_numJoints; ++j) {
				m_baseFrames [j] = readBaseFrame (parser);
			}
			parser.seekEntry ("}");

			// frames
			m_frames = new SSSkeletalJointLocation[m_numFrames][];
			for (int f = 0; f < m_numFrames; ++f) {
				matches = parser.seekEntry ("frame", SSMD5Parser.c_uintRegex, "{");
				int frameIdx = Convert.ToInt32 (matches [1].Value);
				m_frames [frameIdx] = readFrameJoints (parser);
				parser.seekEntry ("}");
			}
		}

		private static SSAABB readBounds(SSMD5Parser parser)
		{
			Match[] matches = parser.seekEntry (
				SSMD5Parser.c_parOpen, 
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
				SSMD5Parser.c_parClose,
				SSMD5Parser.c_parOpen,
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
				SSMD5Parser.c_parClose
			);
			SSAABB ret;
			ret.Min.X = (float)Convert.ToDouble (matches [1].Value);
			ret.Min.Y = (float)Convert.ToDouble (matches [2].Value);
			ret.Min.Z = (float)Convert.ToDouble (matches [3].Value);
			ret.Max.X = (float)Convert.ToDouble (matches [6].Value);
			ret.Max.Y = (float)Convert.ToDouble (matches [7].Value);
			ret.Max.Z = (float)Convert.ToDouble (matches [8].Value);
			return ret;
		}

		private static SSSkeletalJointLocation readBaseFrame(SSMD5Parser parser)
		{
			Match[] matches = parser.seekEntry (
				SSMD5Parser.c_parOpen, 
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
				SSMD5Parser.c_parClose,
				SSMD5Parser.c_parOpen,
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
					SSMD5Parser.c_floatRegex,
				SSMD5Parser.c_parClose
			);
			SSSkeletalJointLocation loc;
			loc.Position.X = (float)Convert.ToDouble (matches [1].Value);
			loc.Position.Y = (float)Convert.ToDouble (matches [2].Value);
			loc.Position.Z = (float)Convert.ToDouble (matches [3].Value);
			loc.Orientation = new Quaternion ();
			loc.Orientation.X = (float)Convert.ToDouble (matches [6].Value);
			loc.Orientation.Y = (float)Convert.ToDouble (matches [7].Value);
			loc.Orientation.Z = (float)Convert.ToDouble (matches [8].Value);
			//loc.ComputeQuatW ();
			return loc;
		}

		private SSSkeletalJointLocation[] readFrameJoints(SSMD5Parser parser)
		{
			parser.seekFloats (m_floatComponents);
			var thisFrameLocations = new SSSkeletalJointLocation[m_numJoints];
			int compIdx = 0;
			for (int j = 0; j < m_numJoints; ++j) {
				HierarchyEntryMD5 jointInfo = m_hierarchy [j];
				byte flags = jointInfo.Flags;

				SSSkeletalJointLocation loc = m_baseFrames [j];
				if ((flags & (byte)LocationFlags.Tx) != 0) {
					loc.Position.X = m_floatComponents [compIdx++];
				}
				if ((flags & (byte)LocationFlags.Ty) != 0) {
					loc.Position.Y = m_floatComponents [compIdx++];
				}
				if ((flags & (byte)LocationFlags.Tz) != 0) {
					loc.Position.Z = m_floatComponents [compIdx++];
				}
				if ((flags & (byte)LocationFlags.Qx) != 0) {
					loc.Orientation.X = m_floatComponents [compIdx++];
				}
				if ((flags & (byte)LocationFlags.Qy) != 0) {
					loc.Orientation.Y = m_floatComponents [compIdx++];
				}
				if ((flags & (byte)LocationFlags.Qz) != 0) {
					loc.Orientation.Z = m_floatComponents [compIdx++];
				}
				loc.ComputeQuatW ();

				if (jointInfo.Parent >= 0) { // has a parent
					SSSkeletalJointLocation parentLoc = thisFrameLocations [jointInfo.Parent];
					loc.Position = Vector3.Transform (loc.Position, parentLoc.Orientation);
					loc.Orientation = Quaternion.Multiply (loc.Orientation, parentLoc.Orientation);
					loc.Orientation.Normalize ();
				}
				thisFrameLocations[j] = loc;
			}
			return thisFrameLocations;
		}

		public class HierarchyEntryMD5 {
			private string m_name;
			private int m_parent;
			private byte m_flags;
			private int m_startIndex;

			public string Name { get { return m_name; } }
			public int Parent { get { return m_parent; } }
			public byte Flags { get { return m_flags; } }
			public int StartIndex { get { return m_startIndex; } }

			public HierarchyEntryMD5(SSMD5Parser parser)
			{
				Match[] matches = parser.seekEntry(
					SSMD5Parser.c_nameRegex, // name
					SSMD5Parser.c_intRegex, // parent index
					SSMD5Parser.c_uintRegex, // flags
					SSMD5Parser.c_uintRegex // start index
				);
				m_name = matches[0].Value;
				m_parent = Convert.ToInt32(matches[1].Value);
				m_flags = Convert.ToByte(matches[2].Value);
				m_startIndex = Convert.ToInt32(matches[3].Value);
			}
		}
	}
}

