using System;
using OpenTK;

namespace SimpleScene
{
	public class SSSkeletalAnimation
	{
		public string name;

		protected int _frameRate;

		protected SSAABB[] _bounds;
		protected SSSkeletalJoint[] _hierarchy;
		protected SSSkeletalJointLocation[][] _frames;

		// temp use
		protected float[] _floatComponents;

		public int numFrames {
			get { return _frames.Length; }
		}

		public int numJoints {
			get { return _hierarchy.Length; }
		}

		public int frameRate {
			get { return _frameRate; }
		}

		public float frameDuration {
			get { return 1f / (float)_frameRate; }
		}

		public float totalDuration {
			get { return (float)(_frames.Length-1) / (float)_frameRate; }
		}

		public SSSkeletalJoint[] hierarchy {
			get { return _hierarchy; }
		}

		public SSAABB[] bounds {
			get { return _bounds; }
		}

		public SSSkeletalJoint[] jointHierarchy {
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

		/// <summary>
		/// Retrieves a joint location for a particular timestamp; in joint-local coordinates
		/// </summary>
		public SSSkeletalJointLocation computeJointFrame(int jointIdx, float t)
		{
			int leftFrameIdx = (int)(t / frameDuration);
			SSSkeletalJointLocation leftJointFrame = _frames [leftFrameIdx] [jointIdx];
			float remainder = t - ((float)leftFrameIdx * frameDuration);
			if (remainder == 0) {
				return leftJointFrame;
			} else {
				SSSkeletalJointLocation rightJointFrame = _frames [leftFrameIdx+1] [jointIdx];
				return SSSkeletalJointLocation.interpolate (
					leftJointFrame,	rightJointFrame, remainder / frameDuration);
			}
		}
	}
}

