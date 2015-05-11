using System;
using System.Collections.Generic;

namespace SimpleScene
{
	public class SSSkeletalAnimationStateMachine
	{
		protected readonly Dictionary<string, AnimationState> _animationStates = new Dictionary<string, AnimationState>();
		protected readonly List<TransitionInfo> _transitions = new List<TransitionInfo>();

		public Dictionary<string, AnimationState> States {
			get { return _animationStates; }
		}

		public List<TransitionInfo> Transitions {
			get { return _transitions; }
		}

		/// <summary>
		/// Adds an animation state
		/// </summary>
		/// <param name="stateName">State name.</param>
		/// <param name="makeDefault">If set to <c>true</c> forces the state machine into this state.</param>
		public void AddState(string stateName, bool makeDefault = false)
		{
			if (makeDefault) {
				foreach (var state in _animationStates.Values) {
					if (state.isDefault) {
						var errMsg = "Default state already defined.";
						System.Console.WriteLine (errMsg);
						throw new Exception (errMsg);
					}
				}
			}

			var newState = new AnimationState ();
			newState.isDefault = makeDefault;
			_animationStates.Add (stateName, newState);
		}

		/// <summary>
		/// Adds animation for a state at a specified channel
		/// </summary>
		public void AddStateAnimation(
			string stateName, 
			int channelId, 
			SSSkeletalAnimation animation,
			bool interChannelFade = false)
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
			newChanState.interChannelFade = interChannelFade;
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
		public void AddStateTransition(string fromStateStr, string targetStateStr, float transitionTime)
		{
			AnimationState fromState;
			if (fromStateStr == null || fromStateStr.Length == 0) {
				fromState = null;
			} else {
				fromState = _animationStates [fromStateStr];
			}
			if (targetStateStr == null || targetStateStr.Length == 0) {
				var errMsg = "target state must be specified for after-playback transitions";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg);
			}
			AnimationState targetState = _animationStates [targetStateStr];

			addStateTransition (fromState, targetState, transitionTime, -1);
		}

		public void AddAnimationEndsTransition(string fromStateStr, string targetStateStr, float transitionTime,
			int animationChannel)

		{
			if (fromStateStr == null || fromStateStr.Length == 0) {
				var errMsg = "from state must be specified for after-playback transitions";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg);
			}
			if (targetStateStr == null || targetStateStr.Length == 0) {
				var errMsg = "target state must be specified for after-playback transitions";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg);
			}
			if (animationChannel < 0) {
				var errMsg = "channel must be >= 0";
				System.Console.WriteLine (errMsg);
				throw new Exception (errMsg);
			}
			AnimationState fromState = _animationStates [fromStateStr];
			AnimationState targetState = _animationStates [targetStateStr];
			addStateTransition (fromState, targetState, transitionTime, animationChannel);
		}

		protected void addStateTransition(AnimationState fromState, AnimationState targetState, float transitionTime,
			int channelEndsTrigger)
		{
			var newTransition = new TransitionInfo ();
			newTransition.sorce = fromState;
			newTransition.target = targetState;
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

		public class TransitionInfo
		{
			public AnimationState sorce = null;
			public AnimationState target = null;
			public float transitionTime = 0f;
			public int channelEndsTrigger = -1; // transition will be triggered when animation at this channel id expires 
		}

		public class ChannelState
		{
			public int channelId;
			public SSSkeletalAnimation animation;
			public bool interChannelFade;
		}

		public class AnimationState
		{
			public List<ChannelState> channelStates = new List<ChannelState>();
			public bool isDefault = false;
		}
	}
}

