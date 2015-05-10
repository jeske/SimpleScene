//#define PREV_PREV_FADE

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

		protected float _transitionTime = 0f;
		protected float _currT = 0f;
		protected float _prevT = 0f;
		protected float _prevTimeout = 0f;

		protected bool _interChannelFade = false;
		protected float _interChannelFadeIntensity = 0f;
		protected float _interChannelFadeVelocity = 0f;

		protected bool _repeat = false;

		#if PREV_PREV_FADE
		protected SSSkeletalAnimation _prevPrevAnimation = null;
		protected float _prevPrevT = 0f;
		protected float _prevTransitionTime = 0f;
		#endif

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

		public bool IsFadingOut {
			get {
				if (_repeat) {
					return false;
				}
				return _currAnimation == null && _currT < _transitionTime;
			}
		}

		public bool InterChannelFade {
			get {
				return _interChannelFade;
			}
		}

		public float InterChannelFadeIntensity {
			get {
				return _interChannelFadeIntensity;
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

			// control interchannel fade intensity
			if (animation == null) {
				if (transitionTime == 0) {
					_interChannelFadeIntensity = 0f;
					_interChannelFadeVelocity = 0f;
				} else {
					_interChannelFadeVelocity = -_interChannelFadeIntensity / transitionTime;
				}
			} else { // animation != null
				if (transitionTime == 0) {
					_interChannelFadeIntensity = 1f;
					_interChannelFadeVelocity = 0f;
				} else {
					_interChannelFadeVelocity = (1f - _interChannelFadeIntensity) / transitionTime;
				}
			}

			// update "previous" variables
			if (transitionTime == 0f) {
				_prevAnimation = null;
				_prevT = 0f;
				#if PREV_PREV_FADE
				_prevPrevAnimation = null;
				_prevPrevT = 0f;
				#endif
			} else { // transitionTime != 0f
				if (_currAnimation != null) {
					#if PREV_PREV_FADE
					if (_prevAnimation != null && _prevT < _transitionTime) {
						// update "previous" previous variables
						_prevPrevT = _prevT;
						_prevPrevAnimation = _prevAnimation;
						_prevTransitionTime = _transitionTime;
					}
					#endif
					_prevAnimation = _currAnimation;
					_prevT = _currT;
				}
				if (_prevAnimation != null) {
					_prevTimeout = _prevT + transitionTime;
				}
			}

			_currAnimation = animation;
			_currT = 0;

			_repeat = repeat;
			_interChannelFade = interChannelFade;
			_transitionTime = transitionTime;
		}

		/// <summary>
		/// Update the animation channel based on time elapsed.
		/// </summary>
		/// <param name="timeElapsed">Time elapsed in seconds</param>
		public void Update(float timeElapsed)
		{
			#if PREV_PREV_FADE
			if (_prevPrevAnimation != null) {
				_prevPrevT += timeElapsed;
				if (_prevPrevT >= _prevTransitionTime) {
					_prevPrevAnimation = null;
					_prevPrevT = 0f;
				}
			}
			#endif

			if (_prevAnimation != null) {
				_prevT += timeElapsed;
				if (_prevT >= _prevTimeout) {
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
			} 
			#if true
			else if (_currT < _transitionTime) {
				// maintain FadeIn ratio for use with interchannel interpolation, until 
				_currT += timeElapsed;
			}
			#endif

			if (IsActive) {
				_interChannelFadeIntensity += (_interChannelFadeVelocity * timeElapsed);
				// clamp
				_interChannelFadeIntensity = Math.Max (_interChannelFadeIntensity, 0f);
				_interChannelFadeIntensity = Math.Min (_interChannelFadeIntensity, 1f);
			} else { // not active
				_interChannelFadeIntensity = 0f;
				_interChannelFadeVelocity = 0f;
			}

		}

		public SSSkeletalJointLocation ComputeJointFrame(int jointIdx)
		{
			if (_currAnimation != null) {
				var loc = _currAnimation.ComputeJointFrame (jointIdx, _currT);
				if (_prevAnimation == null) {
					//GL.Color3 (1f, 0f, 0f);
					return loc;
				} else {
					var prevTime = Math.Min(_prevT, _prevAnimation.TotalDuration);
					var prevLoc = _prevAnimation.ComputeJointFrame (jointIdx, prevTime);
					#if PREV_PREV_FADE
					if (_prevPrevAnimation != null) {
						var prevPrevLoc = _prevPrevAnimation.ComputeJointFrame (jointIdx, _prevPrevT);
						var prevFade = _prevT / _prevTransitionTime;
						prevLoc = SSSkeletalJointLocation.Interpolate (prevPrevLoc, prevLoc, prevFade);
					}
					#endif
					var fadeInRatio = _currT / _transitionTime;
					GL.Color3 (fadeInRatio, 1f - fadeInRatio, 0);
					return SSSkeletalJointLocation.Interpolate (prevLoc, loc, fadeInRatio);
				}
			} else if (_prevAnimation != null) {
				//GL.Color3 (0f, 1f, 0f);
				return _prevAnimation.ComputeJointFrame (jointIdx, _prevT);
			} else {
				var errMsg = "Attempting to compute a joint frame location from an inactive channel.";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg); 
			}
		}
	}
}

