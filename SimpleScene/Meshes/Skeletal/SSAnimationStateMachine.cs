using System;
using System.Collections.Generic;

namespace SimpleScene
{
	public class SSAnimationStateMachine
	{
		protected readonly Dictionary<string, AnimationState> _animationStates = new Dictionary<string, AnimationState>();
		protected readonly List<TransitionInfo> _transitions = new List<TransitionInfo>();

		public Dictionary<string, AnimationState> states {
			get { return _animationStates; }
		}

		public List<TransitionInfo> transitions {
			get { return _transitions; }
		}

		//-----------------

		/// <summary>
		/// Adds an animation state
		/// </summary>
		/// <param name="stateName">State name.</param>
		/// <param name="makeDefault">If set to <c>true</c> forces the state machine into this state.</param>
		public void AddState(string stateName, 
							 SSSkeletalAnimation animation,
							 bool makeDefault = false,
							 bool interChannelFade = true)
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
			newState.interChannelFade = interChannelFade;
			newState.animation = animation;
			_animationStates.Add (stateName, newState);
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

			addStateTransition (fromState, targetState, transitionTime, false);
		}

		public void AddAnimationEndsTransition(string fromStateStr, string targetStateStr, 
											   float transitionTime)

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
			AnimationState fromState = _animationStates [fromStateStr];
			AnimationState targetState = _animationStates [targetStateStr];
			addStateTransition (fromState, targetState, transitionTime, true);
		}

		protected void addStateTransition(AnimationState fromState, 
										  AnimationState targetState, 
									      float transitionTime,
										  bool triggerOnAnimationEnd)
		{
			var newTransition = new TransitionInfo ();
			newTransition.sorce = fromState;
			newTransition.target = targetState;
			newTransition.transitionTime = transitionTime;
			newTransition.triggerOnAnimationEnd = triggerOnAnimationEnd;

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
			public bool triggerOnAnimationEnd = false; // transition will be triggered when animation at this channel id expires 
		}

		public class AnimationState
		{
			public SSSkeletalAnimation animation;
			public bool isDefault = false;
			public bool interChannelFade = true;
		}
	}
}

