using System;
using System.Collections.Generic;

using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL; // debug hacks

namespace SimpleScene
{
	/// <summary>
	/// SS skeletal animation channel.
	/// 
	/// Scenarios to handle:
	/// 1. Repeat the same animation
	/// 2. Transition from one animation to another animation
	/// 
	/// Additionally, SSSkeletalMesh will handle the following scenario:
	/// 3. Transition from an ending partial animation to a higher level animation
	/// </summary>

	public class SSSkeletalAnimationChannelRuntime
	{
		protected readonly List<int> m_topLevelActiveJoints;

		protected SSSkeletalAnimation m_currAnimation = null;
		protected SSSkeletalAnimation m_prevAnimation = null;
		protected float m_currT = 0f;
		protected float m_prevT = 0f;

		protected bool m_repeat = false;
		protected float m_transitionTime = 0f;

		public List<int> TopLevelActiveJoints {
			get { return m_topLevelActiveJoints; }
		}

		public float TransitionTime {
			get { return m_transitionTime; }
		}

		public float TimeRemaining {
			get {
				if (m_currAnimation != null) {
					return m_currAnimation.TotalDuration - m_currT;
				} else if (m_prevAnimation != null) {
					return m_prevAnimation.TotalDuration - m_prevT;
				} else {
					return 0f;
				}
			}
		}

		public bool IsActive {
			get { return m_currAnimation != null || m_prevAnimation != null; }
		}

		public bool IsEnding {
			get {
				if (m_repeat) {
					return false;
				}
				return m_currAnimation == null;
			}
		}

		public float FadeBlendPosition {
			get { 
				return m_currT / m_transitionTime;
			}
		}

		public SSSkeletalAnimationChannelRuntime (int[] topLevelActiveJoints)
		{
			m_topLevelActiveJoints = new List<int>(topLevelActiveJoints);
		}

		public void PlayAnimation(SSSkeletalAnimation animation, 
								  bool repeat, float transitionTime)
		{
			m_prevAnimation = m_currAnimation;
			m_prevT = m_currT;

			m_currAnimation = animation;
			m_currT = 0f;

			m_repeat = repeat;
			m_transitionTime = transitionTime;
		}

		/// <summary>
		/// Update the animation channel based on time elapsed.
		/// </summary>
		/// <param name="timeElapsed">Time elapsed in seconds</param>
		public void Update(float timeElapsed)
		{
			if (m_prevAnimation != null) {
				m_prevT += timeElapsed;
				if (m_prevT > m_prevAnimation.TotalDuration) {
					m_prevAnimation = null;
					m_prevT = 0f;
					if (!m_repeat) {
						m_transitionTime = 0f;
					}
				}
			}

			if (m_currAnimation != null) {
				m_currT += timeElapsed;
				float transitionThreshold = m_currAnimation.TotalDuration - m_transitionTime;
				if (m_currT >= transitionThreshold) {
					if (m_transitionTime > 0f) {
						m_prevAnimation = m_currAnimation;
						m_prevT = m_currT;
						m_currT -= transitionThreshold;
					} else {
						m_currT = 0f;
					}
					if (!m_repeat) {
						m_currAnimation = null;
					}
				}
			}
		}

		public SSSkeletalJointLocation ComputeJointFrame(int jointIdx)
		{
			if (m_currAnimation != null) {
				var loc = m_currAnimation.ComputeJointFrame (jointIdx, m_currT);
				if (m_prevAnimation == null) {
					return loc;
				} else {
					GL.Color4 (Color4.Yellow);
					var prevLoc = m_prevAnimation.ComputeJointFrame (jointIdx, m_prevT);
					return SSSkeletalJointLocation.Interpolate (
						loc, prevLoc, FadeBlendPosition);
				}
			} else if (m_prevAnimation != null) {
				return m_prevAnimation.ComputeJointFrame (jointIdx, m_prevT);
			} else {
				var errMsg = "Attempting to compute a joint frame location from an inactive channel.";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg);
			}
		}
	}
}

