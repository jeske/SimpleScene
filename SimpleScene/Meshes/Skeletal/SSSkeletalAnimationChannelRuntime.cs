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
		protected readonly List<int> _topLevelActiveJoints;
		protected readonly int _id; // for debugging only

		protected SSSkeletalAnimation _currAnimation = null;
		protected SSSkeletalAnimation _prevAnimation = null;
		protected float _currT = 0f;
		protected float _prevT = 0f;
		protected float _prevTimeout = 0f;
		protected bool _interChannelFade = false;

		protected bool _repeat = false;
		protected float _transitionTime = 0f;

		public List<int> TopLevelActiveJoints {
			get { return _topLevelActiveJoints; }
		}

		public float TransitionTime {
			get { return _transitionTime; }
		}

		public float TimeRemaining {
			get {
				if (_currAnimation != null) {
					return _currAnimation.TotalDuration - _currT;
				} else if (_prevAnimation != null) {
					return _prevAnimation.TotalDuration - _prevT;
				} else {
					return 0f;
				}
			}
		}

		public bool IsActive {
			get { return _currAnimation != null || _prevAnimation != null; }
		}

		public bool IsEnding {
			get {
				if (_repeat) {
					return false;
				}
				return _prevAnimation != null && _currAnimation == null;
			}
		}

		public bool IsStarting {
			get {
				return _prevAnimation == null && _currT < _transitionTime;
			}
		}

		public float FadeInRatio {
			get { 
				return _currT / _transitionTime;
			}
		}

		public bool InterChannelFade {
			get {
				return _interChannelFade;
			}
		}

		public SSSkeletalAnimationChannelRuntime (int id, int[] topLevelActiveJoints)
		{
			_topLevelActiveJoints = new List<int>(topLevelActiveJoints);
			_id = id;
		}

		public void PlayAnimation(SSSkeletalAnimation animation, 
								  bool repeat, float transitionTime, bool interChannelFade)
		{
			//System.Console.WriteLine ("play: {0}, repeat: {1}, transitionTime {2}, ichf: {3}",
			//	animation != null ? animation.Name : "null", repeat, transitionTime, interChannelFade);

			if (transitionTime == 0) {
				_prevAnimation = null;
				_prevT = 0;
			} else {
				_prevAnimation = _currAnimation;
				_prevT = _currT;
				if (_prevAnimation != null) {
					_prevTimeout = Math.Min (_prevAnimation.TotalDuration, _prevT + transitionTime);
				}
			}

			_currAnimation = animation;
			_currT = 0f;

			_repeat = repeat;
			_transitionTime = transitionTime;
			_interChannelFade = interChannelFade;
		}

		/// <summary>
		/// Update the animation channel based on time elapsed.
		/// </summary>
		/// <param name="timeElapsed">Time elapsed in seconds</param>
		public void Update(float timeElapsed)
		{
			if (_prevAnimation != null) {
				_prevT += timeElapsed;
				if (_prevT > _prevTimeout) {
					_prevAnimation = null;
					_prevT = 0f;
				}
			}

			if (_currAnimation != null) {
				_currT += timeElapsed;
				if (_repeat) {
					if (_currT >= _currAnimation.TotalDuration - _transitionTime) {
						PlayAnimation (_currAnimation, true, _transitionTime, _interChannelFade);
					}
				} else {
					if (_currT >= _transitionTime) {
						_transitionTime = 0;
					}
					if (_currT >= _currAnimation.TotalDuration) {
						_currAnimation = null;
						_currT = 0;
					}
				}
			} else if (IsEnding) {
				// maintain FadeIn ratio for use with interchannel interpolation, until 
				_currT += timeElapsed;
				if (_currT >= _transitionTime) {
					_currT = 0;
				}

			}
		}

		public SSSkeletalJointLocation ComputeJointFrame(int jointIdx)
		{
			if (_currAnimation != null) {
				var loc = _currAnimation.ComputeJointFrame (jointIdx, _currT);
				if (_prevAnimation == null) {
					return loc;
				} else {
					GL.Color4 (new Color4(FadeInRatio, 1f - FadeInRatio, 0, 1f));
					var prevLoc = _prevAnimation.ComputeJointFrame (jointIdx, _prevT);
					var ret =  SSSkeletalJointLocation.Interpolate (
						loc, prevLoc, FadeInRatio);
					return ret;
				}
			} else if (_prevAnimation != null) {
				return _prevAnimation.ComputeJointFrame (jointIdx, _prevT);
			} else {
				var errMsg = "Attempting to compute a joint frame location from an inactive channel.";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg); 
			}
		}
	}
}

