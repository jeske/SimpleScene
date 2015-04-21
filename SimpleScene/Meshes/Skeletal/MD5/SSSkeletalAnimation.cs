using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalAnimation
	{
		protected int m_frameRate;

		protected SSAABB[] m_bounds;
		protected SSSkeletalJointBaseInfo[] m_hierarchy;
		protected SSSkeletalJointLocation[][] m_frames;

		// temp use
		protected float[] m_floatComponents;

		public int NumFrames {
			get { return m_frames.Length; }
		}

		public int NumJoints {
			get { return m_hierarchy.Length; }
		}

		public int FrameRate {
			get { return m_frameRate; }
		}

		public float FrameDuration {
			get { return 1f / (float)m_frameRate; }
		}

		public float TotalDuration {
			get { return (float)m_frames.Length / (float)m_frameRate; }
		}

		public SSAABB[] Bounds {
			get { return m_bounds; }
		}

		public SSSkeletalJointBaseInfo[] JointHierarchy {
			get { return m_hierarchy; }
		}

		public SSSkeletalAnimation (int frameRate,
									SSSkeletalJointBaseInfo[] jointInfo,
									SSSkeletalJointLocation[][] frames,
									SSAABB[] bounds = null)
		{
			m_hierarchy = jointInfo;
			m_frames = frames;
			m_bounds = bounds;
			m_frameRate = frameRate;
		}

		public SSSkeletalJointLocation ComputeJointFrame(int jointIdx, float t)
		{
			int leftFrameIdx = (int)(t / FrameDuration);
			SSSkeletalJointLocation leftJointFrame = m_frames [leftFrameIdx] [jointIdx];
			float remainder = ((float)leftFrameIdx * FrameDuration - t);
			if (remainder == 0) {
				return leftJointFrame;
			} else {
				SSSkeletalJointLocation rightJointFrame = m_frames [leftFrameIdx+1] [jointIdx];
				return SSSkeletalJointLocation.Interpolate (
					leftJointFrame,	rightJointFrame, remainder / FrameDuration);
			}
		}
	}
}

