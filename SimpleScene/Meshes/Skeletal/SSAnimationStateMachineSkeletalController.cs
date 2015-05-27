using System;
using System.Collections.Generic;

using OpenTK.Graphics.OpenGL; // debug hacks


namespace SimpleScene
{
	// TODO repeat count of sorts

	/// <summary>
	/// Simple skeletal animation state machine.
	/// 
	/// Every animation state is defined by a set of channel IDs and corresponding animations to be played for that channel
	/// State transitions are defined by a source state, target state, and transition/blend time
	/// 
	/// Note that it should be possible to apply multiple state machines effectively to a single skeletal mesh so long
	/// as they affect non-overlapping channels.
	/// 
	/// </summary>
	public class SSAnimationStateMachineSkeletalController : SSSkeletalChannelController
	{
		protected SSAnimationStateMachine _smDescription;
		protected SSAnimationStateMachine.AnimationState _activeState = null;
		protected readonly List<int> _topLevelActiveJoints = null;
		protected readonly Dictionary<int, bool> _jointIsControlledCache = new Dictionary<int, bool>();

		protected readonly ChannelManager _channelManager = new ChannelManager();

		protected float _interChannelFadeIntensity = 0f;
		protected float _interChannelFadeVelocity = 0f;

		public SSAnimationStateMachineSkeletalController (
			SSAnimationStateMachine description,
			params int[] topLevelJoints)
		{
			_smDescription = description;
			if (topLevelJoints != null && topLevelJoints.Length > 0) {
				_topLevelActiveJoints = new List<int> (topLevelJoints);
			}
			foreach (var state in _smDescription.states.Values) {
				if (state.isDefault) {
					forceState (state);
					return;
				}
			}
			var errMsg = "no default state is specified.";
			System.Console.WriteLine (errMsg);
			throw new Exception (errMsg);
		}

		#region SSSkeletalChannelController compliance
		public override bool isActive (SSSkeletalJointRuntime joint)
		{
			if (!_channelManager.IsActive) {
				return false;
			} else if (_topLevelActiveJoints == null) {
				return true;
			} else {
				bool jointIsControlled;
				int jointIdx = joint.BaseInfo.JointIndex;
				if (_jointIsControlledCache.ContainsKey (jointIdx)) {
					jointIsControlled = _jointIsControlledCache [jointIdx] ;
				} else {
					if (_topLevelActiveJoints.Contains (jointIdx)) {
						jointIsControlled = true;
					} else if (joint.BaseInfo.ParentIndex == -1) {
						jointIsControlled = false;
					} else {
						jointIsControlled = isActive(joint.Parent);
					}
					_jointIsControlledCache [jointIdx] = jointIsControlled;
				}
				return jointIsControlled;
			}
		}

		public override float interChannelFadeIndentisy ()
		{
			return _interChannelFadeIntensity;
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			var ret = _channelManager.ComputeJointFrame (joint.BaseInfo.JointIndex);
			if (joint.Parent != null) {
				ret.ApplyPrecedingTransform (joint.Parent.CurrentLocation);
			}
			return ret;
		}

		public override void update (float timeElapsed)
		{
			_channelManager.update (timeElapsed);

			triggerAutomaticTransitions (); // after channel manager to avoid null states during transitions

			if (_channelManager.IsActive) {
				_interChannelFadeIntensity += (_interChannelFadeVelocity * timeElapsed);
				// clamp
				_interChannelFadeIntensity = Math.Max (_interChannelFadeIntensity, 0f);
				_interChannelFadeIntensity = Math.Min (_interChannelFadeIntensity, 1f);
			} else { // not active
				_interChannelFadeIntensity = 0f;
				_interChannelFadeVelocity = 0f;
			}
		}
		#endregion

		#region API for user interaction
		public void RequestTransition(string targetStateName)
		{
			var targetState = _smDescription.states [targetStateName];
			foreach (var transition in _smDescription.transitions) {
				if (transition.target == targetState
				    && (transition.sorce == null || transition.sorce == _activeState)) {
					requestTransition (transition, transition.transitionTime);
					return;
				}
			}
		}

		public void ForceState(string targetStateName)
		{
			var targetState = _smDescription.states [targetStateName];
			forceState (targetState);
		}
		#endregion

		private void triggerAutomaticTransitions()
		{
			foreach (var transition in _smDescription.transitions) {
				if (transition.sorce == _activeState && transition.triggerOnAnimationEnd) {
					if (transition.transitionTime == 0 && !_channelManager.IsActive) {
						requestTransition (transition, 0f);
					}
					else if (_channelManager.TimeRemaining <= transition.transitionTime) {
						requestTransition (transition, _channelManager.TimeRemaining);
					}
					return;
				}
			}
		}

		protected void requestTransition(
			SSAnimationStateMachine.TransitionInfo transition, float transitionTime)
		{
			_activeState = transition.target;

			if (_activeState.animation == null) {
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

			_channelManager.PlayAnimation(_activeState.animation, false, transitionTime);
		}

		protected void forceState(SSAnimationStateMachine.AnimationState targetState)
		{
			_activeState = targetState;

			_interChannelFadeVelocity = 0f;
			if (_activeState.animation == null) {
				_interChannelFadeIntensity = 0f;
			} else {
				_interChannelFadeIntensity = 1f;
			}
			
			_channelManager.PlayAnimation(_activeState.animation, false, 0f);
		}


		/// <summary>
		/// SS skeletal animation channel.
		/// 
		/// Scenarios to handle:
		/// 1. Repeat the same animation
		/// 2. Transition from one animation to another animation
		protected class ChannelManager
		{
			protected SSSkeletalAnimation _currAnimation = null;
			protected SSSkeletalAnimation _prevAnimation = null;

			protected float _transitionTime = 0f;
			protected float _currT = 0f;
			protected float _prevT = 0f;
			protected float _prevTimeout = 0f;

			protected bool _repeat = false;

			#if PREV_PREV_FADE
			protected SSSkeletalAnimation _prevPrevAnimation = null;
			protected float _prevPrevT = 0f;
			protected float _prevTransitionTime = 0f;
			#endif

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

			public void PlayAnimation(SSSkeletalAnimation animation, 
									  bool repeat, float transitionTime)
			{
				//System.Console.WriteLine ("play: {0}, repeat: {1}, transitionTime {2}, ichf: {3}",
				//	animation != null ? animation.Name : "null", repeat, transitionTime, interChannelFade);


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
				_transitionTime = transitionTime;
			}

			/// <summary>
			/// Update the animation channel based on time elapsed.
			/// </summary>
			/// <param name="timeElapsed">Time elapsed in seconds</param>
			public void update(float timeElapsed)
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
							PlayAnimation (_currAnimation, true, _transitionTime);
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
}

