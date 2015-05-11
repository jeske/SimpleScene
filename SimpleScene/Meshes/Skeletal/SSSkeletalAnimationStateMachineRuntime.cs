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
	/// Note that it should be possible to apply multiple state machines effectively to a single skeletal mesh so long
	/// as they affect non-overlapping channels.
	/// 
	/// </summary>
	public class SSSkeletalAnimationStateMachineRuntime
	{
		protected SSSkeletalAnimationStateMachine _description;
		protected SSSkeletalAnimationStateMachine.AnimationState _activeState = null;
		protected Dictionary<int, SSSkeletalAnimationChannelRuntime> _channelsRuntime = null;

		public SSSkeletalAnimationStateMachineRuntime (
			SSSkeletalAnimationStateMachine description,
			Dictionary<int, SSSkeletalAnimationChannelRuntime> chanRuntime)
		{
			_description = description;
			_channelsRuntime = chanRuntime;
			foreach (var state in _description.States.Values) {
				if (state.isDefault) {
					forceState (state);
					return;
				}
			}
			var errMsg = "no default state is specified.";
			System.Console.WriteLine (errMsg);
			throw new Exception (errMsg);
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

		public void TriggerAutomaticTransitions()
		{
			foreach (var transition in _description.Transitions) {
				if (transition.sorce == _activeState && transition.channelEndsTrigger >= 0) {
					foreach (var chanState in _activeState.channelStates) {
						int cid = chanState.channelId;
						if (cid == transition.channelEndsTrigger) {
							var chanRuntime = _channelsRuntime [cid];
							if (transition.transitionTime == 0 && !chanRuntime.IsActive) {
								requestTransition (transition, 0f);
							}
							else if (chanRuntime.TimeRemaining <= transition.transitionTime) {
								requestTransition (transition, chanRuntime.TimeRemaining);
							}
							return;
						}
					}
				}
			}
		}

		public void RequestTransition(string targetStateName)
		{
			var targetState = _description.States [targetStateName];
			foreach (var transition in _description.Transitions) {
				if (transition.target == targetState
				    && (transition.sorce == null || transition.sorce == _activeState)) {
					requestTransition (transition, transition.transitionTime);
					return;
				}
			}
		}

		public void ForceState(string targetStateName)
		{
			var targetState = _description.States [targetStateName];
			forceState (targetState);
		}

		protected void requestTransition(
			SSSkeletalAnimationStateMachine.TransitionInfo transition, float transitionTime)
		{
			foreach (var chanState in transition.target.channelStates) {
				var channel = _channelsRuntime [chanState.channelId];
				channel.PlayAnimation (
					chanState.animation, false, transitionTime, chanState.interChannelFade);
			}
			_activeState = transition.target;
		}

		protected void forceState(SSSkeletalAnimationStateMachine.AnimationState targetState)
		{
			if (_channelsRuntime != null) {
				foreach (var channelState in targetState.channelStates) {
					_channelsRuntime [channelState.channelId].PlayAnimation (
						channelState.animation, false, 0f, channelState.interChannelFade);
				}
			}
			_activeState = targetState;
		}
	}
}

