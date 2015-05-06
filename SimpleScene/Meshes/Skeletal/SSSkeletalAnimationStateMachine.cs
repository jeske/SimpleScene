using System;
using System.Collections.Generic;

namespace SimpleScene
{
	// TODO repeat count of sorts

	/// <summary>
	/// Simple skeletal animation state machine.
	/// 
	/// Every animation state is defined by a set of channel IDs and corresponding animations to be played for that channel
	/// State transitions are defined by a source state, target state, and transition/blend time
	/// 
	/// Note that it should be possible to apply multiple state machines effectively so long as they affect non-overlapping 
	/// channels.
	/// 
	/// </summary>
	public class SSSkeletalAnimationStateMachine
	{
		protected readonly Dictionary<string, AnimationState> _animationStates = new Dictionary<string, AnimationState>();
		protected readonly List<TransitionInfo> _transitions = new List<TransitionInfo>();

		protected AnimationState _activeState = null;
		protected Dictionary<int, SSSkeletalAnimationChannelRuntime> _channelsRuntime = null;

		public SSSkeletalAnimationStateMachine (Dictionary<int, SSSkeletalAnimationChannelRuntime> chanRuntime = null)
		{
			ConnectRuntimeChannels (chanRuntime);
		}

		public void ConnectRuntimeChannels(Dictionary<int, SSSkeletalAnimationChannelRuntime> chanRuntime)
		{
			if (_channelsRuntime != chanRuntime) {
				_channelsRuntime = chanRuntime;
				if (_activeState != null) {
					forceState (_activeState);
				}
			}
		}

		/// <summary>
		/// Adds an animation state
		/// </summary>
		/// <param name="stateName">State name.</param>
		/// <param name="makeDefault">If set to <c>true</c> forces the state machine into this state.</param>
		public void AddState(string stateName, bool makeActive = false)
		{
			var newState = new AnimationState ();
			_animationStates.Add (stateName, newState);
			if (_activeState == null || makeActive) {
				ForceState (stateName);
			}
		}

		/// <summary>
		/// Adds animation for a state at a specified channel
		/// </summary>
		public void AddStateAnimation(string stateName, int channelId, SSSkeletalAnimation animation)
		{
			AnimationState animState = _animationStates [stateName];
			foreach (var channelState in animState.channelStates) {
				if (channelState.channelId == channelId) {
					var errMsg = "state " + stateName + " already has an animation for channel " + channelId;
					System.Console.WriteLine (errMsg);
					throw new Exception (errMsg);
				}
			}

			var newChanState = new ChannelState ();
			newChanState.channelId = channelId;
			newChanState.animation = animation;
			animState.channelStates.Add (newChanState);
		}

		/// <summary>
		/// Adds an activatable state transition.
		/// </summary>
		/// <param name="fromState">From state. Null or empty means transition from any state</param>
		/// <param name="targetState">Target animation state.</param>
		/// <param name="transitionTime">Animation transition/overlap time.</param>
		/// <param name="channelEndsTrigger">
		///     If specified, animation ending on this channel of the source animation state will trigger the transition to the
		///     target animation state
		/// </param>
		public void AddStateTransition(string fromState, string targetState, float transitionTime,
									   int channelEndsTrigger = -1)
		{
			if (targetState == null || targetState.Length == 0) {
				var errMsg = "target state must be specified";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg);
			}

			var newTransition = new TransitionInfo ();
			if (fromState == null || fromState.Length == 0) {
				if (channelEndsTrigger >= 0) {
					var errMsg = "from state must be specified for after-playback transitions";
					System.Console.WriteLine (errMsg);
					throw new Exception (errMsg);
				}
				newTransition.sorce = null;
			} else {
				newTransition.sorce = _animationStates [fromState];
			}
			newTransition.target = _animationStates [targetState];
			newTransition.transitionTime = transitionTime;
			newTransition.channelEndsTrigger = channelEndsTrigger;

			foreach (var transition in _transitions) {
				if (transition.Equals (newTransition)) {
					var errMsg = "Idential animation transition already defined.";
					System.Console.WriteLine (errMsg);
					throw new Exception (errMsg);
				}
			}
			_transitions.Add (newTransition);
		}

		public void TriggerAutomaticTransitions()
		{
			foreach (var transition in _transitions) {
				if (transition.sorce == _activeState && transition.channelEndsTrigger >= 0) {
					foreach (var chanState in _activeState.channelStates) {
						int cid = chanState.channelId;
						if (cid == transition.channelEndsTrigger) {
							var chanRuntime = _channelsRuntime [cid];
							if (!chanRuntime.IsActive 
							 || chanRuntime.TimeRemaining < transition.transitionTime) {
								requestTransition (transition.target);
							}
							return;
						}
					}
				}
			}
		}

		public void RequestTransition(string targetStateName)
		{
			var targetState = _animationStates [targetStateName];
			requestTransition (targetState);
		}

		public void ForceState(string targetStateName)
		{
			var targetState = _animationStates [targetStateName];
			forceState (targetState);
		}

		protected void requestTransition(AnimationState targetState)
		{
			foreach (var transition in _transitions) {
				if (transition.target == targetState
				&& (transition.sorce == null || transition.sorce == _activeState)) {
					foreach (var chanState in targetState.channelStates) {
						var channel = _channelsRuntime [chanState.channelId];
						channel.PlayAnimation (
							chanState.animation, false, transition.transitionTime);
					}
					_activeState = targetState;
				}
			}
		}

		protected void forceState(AnimationState targetState)
		{
			if (_channelsRuntime != null) {
				foreach (var channelState in targetState.channelStates) {
					_channelsRuntime [channelState.channelId].PlayAnimation (
						channelState.animation, true, 0f);
				}
			}
			_activeState = targetState;
		}

		protected class TransitionInfo
		{
			public AnimationState sorce = null;
			public AnimationState target = null;
			public float transitionTime = 0f;
			public int channelEndsTrigger = -1; // transition will be triggered when animation at this channel id expires 
		}

		protected class ChannelState
		{
			public int channelId;
			public SSSkeletalAnimation animation;
		}

		protected class AnimationState
		{
			public List<ChannelState> channelStates = new List<ChannelState>();
		}


	}
}

