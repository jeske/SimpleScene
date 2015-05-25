using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalAnimation
	{
		public string Name;

		protected int _frameRate;

		protected SSAABB[] _bounds;
		protected SSSkeletalJoint[] _hierarchy;
		protected SSSkeletalJointLocation[][] _frames;

		// temp use
		protected float[] _floatComponents;

		public int NumFrames {
			get { return _frames.Length; }
		}

		public int NumJoints {
			get { return _hierarchy.Length; }
		}

		public int FrameRate {
			get { return _frameRate; }
		}

		public float FrameDuration {
			get { return 1f / (float)_frameRate; }
		}

		public float TotalDuration {
			get { return (float)(_frames.Length-1) / (float)_frameRate; }
		}

		public SSSkeletalJoint[] Hierarchy {
			get { return _hierarchy; }
		}

		public SSAABB[] Bounds {
			get { return _bounds; }
		}

		public SSSkeletalJoint[] JointHierarchy {
			get { return _hierarchy; }
		}

		public SSSkeletalAnimation (int frameRate,
									SSSkeletalJoint[] jointInfo,
									SSSkeletalJointLocation[][] frames,
									SSAABB[] bounds = null)
		{
			_hierarchy = jointInfo;
			_frames = frames;
			_bounds = bounds;
			_frameRate = frameRate;
		}

		public SSSkeletalJointLocation ComputeJointFrame(int jointIdx, float t)
		{
			int leftFrameIdx = (int)(t / FrameDuration);
			SSSkeletalJointLocation leftJointFrame = _frames [leftFrameIdx] [jointIdx];
			float remainder = t - ((float)leftFrameIdx * FrameDuration);
			if (remainder == 0) {
				return leftJointFrame;
			} else {
				SSSkeletalJointLocation rightJointFrame = _frames [leftFrameIdx+1] [jointIdx];
				return SSSkeletalJointLocation.Interpolate (
					leftJointFrame,	rightJointFrame, remainder / FrameDuration);
			}
		}
	}
}

