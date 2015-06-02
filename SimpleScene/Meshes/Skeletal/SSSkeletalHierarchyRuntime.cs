using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL; // debug hacks

namespace SimpleScene
{
	public class SSSkeletalJointRuntime
	{
		public List<SSSkeletalJointRuntime> Children = new List<SSSkeletalJointRuntime>();
		public SSSkeletalJointRuntime Parent = null;

		public SSSkeletalJointLocation CurrentLocation;

		protected SSSkeletalJoint _baseInfo;

		public SSSkeletalJoint BaseInfo {
			get { return _baseInfo; }
		}

		public SSSkeletalJointRuntime(SSSkeletalJoint baseInfo)
		{
			_baseInfo = baseInfo;
			CurrentLocation = _baseInfo.BindPoseLocation;
		}
	}

	public class SSSkeletalHierarchyRuntime
	{
		protected readonly SSSkeletalJointRuntime[] _joints = null;
		protected readonly List<int> _topLevelJoints = new List<int> ();

		public SSSkeletalJointRuntime[] joints {
			get { return _joints; }
		}

		public int numJoints {
			get { return _joints.Length; }
		}

		public SSSkeletalHierarchyRuntime(SSSkeletalJoint[] joints)
		{
			_joints = new SSSkeletalJointRuntime[joints.Length];
			for (int j = 0; j < joints.Length; ++j) {
				var jointInput = joints [j];
				_joints [j] = new SSSkeletalJointRuntime(jointInput);
				int parentIdx = jointInput.ParentIndex;
				if (parentIdx < 0) {
					_topLevelJoints.Add (j);
				} else {
					_joints [j].Parent = _joints [parentIdx];
					_joints [parentIdx].Children.Add (_joints[j]);
				}
			}
		}

		public int jointIndex(string jointName)
		{
			if (String.Compare (jointName, "all", true) == 0) {
				return -1;
			}

			for (int j = 0; j < _joints.Length; ++j) {
				if (_joints [j].BaseInfo.Name == jointName) {
					return j;
				}
			}
			string errMsg = string.Format ("Joint not found: \"{0}\"", jointName);
			System.Console.WriteLine (errMsg);
			throw new Exception (errMsg);
		}

		public int[] jointIndices(string[] jointNames) 
		{
			if (jointNames == null || jointNames.Length == 0) {
				return null;
			}
			int count = jointNames.Length;
			int[] ret = new int[count];
			for (int i = 0; i < count; ++i) {
				int idx = jointIndex (jointNames [i]);
				if (idx == -1) {
					return null;
				}
				ret [i] = idx;
			}
			return ret;
		}

		public SSSkeletalJointLocation jointLocation(int jointIdx) 
		{
			return _joints [jointIdx].CurrentLocation;
		}

		public int[] topLevelJoints {
			get { return _topLevelJoints.ToArray (); }
		}

		public void verifyAnimation(SSSkeletalAnimation animation)
		{
			if (this.numJoints != animation.NumJoints) {
				string str = string.Format (
					"Joint number mismatch: {0} in md5mesh, {1} in md5anim",
					this.numJoints, animation.NumJoints);
				Console.WriteLine (str);
				throw new Exception (str);
			}
			for (int j = 0; j < numJoints; ++j) {
				SSSkeletalJoint thisJointInfo = this._joints [j].BaseInfo;
				SSSkeletalJoint animJointInfo = animation.JointHierarchy [j];
				if (thisJointInfo.Name != animJointInfo.Name) {
					string str = string.Format (
						"Joint name mismatch: {0} in md5mesh, {1} in md5anim",
						thisJointInfo.Name, animJointInfo.Name);
					Console.WriteLine (str);
					throw new Exception (str);
				}
				if (thisJointInfo.ParentIndex != animJointInfo.ParentIndex) {
					string str = string.Format (
						"Hierarchy parent mismatch for joint \"{0}\": {1} in md5mesh, {2} in md5anim",
						thisJointInfo.Name, thisJointInfo.ParentIndex, animJointInfo.ParentIndex);
					Console.WriteLine (str);
					throw new Exception (str);
				}
			}
		}

		public void verifyJoints(SSSkeletalJoint[] joints)
		{
			if (this.numJoints != joints.Length) {
				string str = string.Format (
					"Joint number mismatch: {0} in this hierarchy, {1} in other joints",
					this.numJoints, joints.Length);
				Console.WriteLine (str);
				throw new Exception (str);
			}
			for (int j = 0; j < numJoints; ++j) {
				SSSkeletalJoint thisJointInfo = this._joints [j].BaseInfo;
				SSSkeletalJoint otherJointInfo = joints [j];
				if (thisJointInfo.Name != otherJointInfo.Name) {
					string str = string.Format (
						"Joint name mismatch: {0} in this hierarchy, {1} in other joints",
						thisJointInfo.Name, otherJointInfo.Name);
					Console.WriteLine (str);
					throw new Exception (str);
				}
				if (thisJointInfo.ParentIndex != otherJointInfo.ParentIndex) {
					string str = string.Format (
						"Hierarchy parent mismatch for joint \"{0}\": {1} in this hierarchy, {2} in other joints",
						thisJointInfo.Name, thisJointInfo.ParentIndex, otherJointInfo.ParentIndex);
					Console.WriteLine (str);
					throw new Exception (str);
				}
			}
		}

		public void applySkeletalControllers(List<SSSkeletalChannelController> channelControllers)
		{
			foreach (int j in _topLevelJoints) {
				traverseWithControllers (_joints[j], channelControllers);
			}
		}

		private void traverseWithControllers(SSSkeletalJointRuntime joint, List<SSSkeletalChannelController> controllers)
		{
			joint.CurrentLocation = computeJointLocWithControllers (joint, controllers, controllers.Count - 1);

			foreach (var child in joint.Children) {
				traverseWithControllers (child, controllers);
			}

			#if false
			if (channelControllers != null) {
				foreach (var channel in channels) {
					if (channel.IsActive && channel.TopLevelActiveJoints.Contains (jointIdx)) {
						if (activeChannel != null) {
							fallbackActiveChannel = activeChannel;
						}
						activeChannel = channel;
					}
				}
			}

			SSSkeletalJointRuntime joint = _joints [jointIdx];
			int parentIdx = joint.BaseInfo.ParentIndex;

			if (activeChannel == null) {
				joint.CurrentLocation = joint.BaseInfo.BaseLocation;
				if (joint.BaseInfo.ParentIndex != -1) {
					joint.CurrentLocation.UndoPrecedingTransform (_joints [parentIdx].CurrentLocation);
				}
			} else {
				SSSkeletalJointLocation activeLoc = activeChannel.ComputeJointFrame (jointIdx);

				//activeLoc = activeChannel.ComputeJointFrame (jointIdx);
				if (activeChannel.InterChannelFade && activeChannel.InterChannelFadeIntensity < 1f) {
					// TODO smarter, multi layer fallback
					SSSkeletalJointLocation fallbackLoc;
					if (fallbackActiveChannel == null || fallbackActiveChannel.IsFadingOut) {
						// fall back to bind bose
						fallbackLoc = joint.BaseInfo.BaseLocation;
						if (joint.BaseInfo.ParentIndex != -1) {
							fallbackLoc.UndoPrecedingTransform (_joints [parentIdx].CurrentLocation);
						}
						GL.Color4 (Color4.LightGoldenrodYellow); // debugging
					} else {
						fallbackLoc = fallbackActiveChannel.ComputeJointFrame (jointIdx);
					}
					float activeChannelRatio = activeChannel.InterChannelFadeIntensity;
					GL.Color3(activeChannelRatio, activeChannelRatio, 1f - activeChannelRatio);
					joint.CurrentLocation = SSSkeletalJointLocation.Interpolate (
						fallbackLoc, activeLoc, activeChannelRatio);
				} else {
					joint.CurrentLocation = activeLoc;
				}
			}

			if (jointPositionOverride != null && jointPositionOverride.ContainsKey (jointIdx)) {
				joint.CurrentLocation.Position = jointPositionOverride [jointIdx];
			}
			if (jointOrientationOverride != null && jointOrientationOverride.ContainsKey (jointIdx)) {
				joint.CurrentLocation.Orientation = jointOrientationOverride [jointIdx];
			}

			if (parentIdx != -1) {
				joint.CurrentLocation.ApplyPrecedingTransform (_joints [parentIdx].CurrentLocation);
			}

			foreach (int child in joint.Children) {
				traverseWithChannels (child, channels, jointPositionOverride, jointOrientationOverride,
									  activeChannel, fallbackActiveChannel);
			}
			#endif
		}

		private SSSkeletalJointLocation computeJointLocWithControllers(
			SSSkeletalJointRuntime joint, List<SSSkeletalChannelController> controllers, int controllerIdx)
		{
			var channel = controllers [controllerIdx];
			if (channel.isActive(joint)) {
				var channelLoc = channel.computeJointLocation (joint);
				if (joint.Parent != null) {
					channelLoc.ApplyPrecedingTransform (joint.Parent.CurrentLocation);
				}
				if (!channel.interChannelFade 
					|| channel.interChannelFadeIndentisy() >= 1f 
					|| controllerIdx == 0) {
					return channelLoc;
				} else {
					var fallbackLoc 
					= computeJointLocWithControllers (joint, controllers, controllerIdx - 1);
					return SSSkeletalJointLocation.Interpolate (
						fallbackLoc, channelLoc, channel.interChannelFadeIndentisy());
				}
			} else {
				if (controllerIdx > 0) {
					return computeJointLocWithControllers (joint, controllers, controllerIdx - 1);
				} else {
					throw new Exception ("fell through without an active skeletal channel controller");
				}
			}

		}
	}
}

