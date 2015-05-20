using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL; // debug hacks

namespace SimpleScene
{
	public class SSSkeletalJointRuntime
	{
		public List<int> Children = new List<int>();
		public SSSkeletalJointLocation CurrentLocation;

		protected SSSkeletalJoint _baseInfo;

		public SSSkeletalJoint BaseInfo {
			get { return _baseInfo; }
		}

		public SSSkeletalJointRuntime(SSSkeletalJoint baseInfo)
		{
			_baseInfo = baseInfo;
			CurrentLocation = _baseInfo.BaseLocation;
		}
	}

	public class SSSkeletalHierarchyRuntime
	{
		protected readonly SSSkeletalJointRuntime[] _joints = null;
		protected readonly List<int> _topLevelJoints = new List<int> ();

		public SSSkeletalJointRuntime[] Joints {
			get { return _joints; }
		}

		public int NumJoints {
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
					_joints [parentIdx].Children.Add (j);
				}
			}
		}

		public int JointIndex(string jointName)
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

		public SSSkeletalJointLocation JointLocation(int jointIdx) 
		{
			return _joints [jointIdx].CurrentLocation;
		}

		public int[] TopLevelJoints {
			get { return _topLevelJoints.ToArray (); }
		}

		public void VerifyAnimation(SSSkeletalAnimation animation)
		{
			if (this.NumJoints != animation.NumJoints) {
				string str = string.Format (
					"Joint number mismatch: {0} in md5mesh, {1} in md5anim",
					this.NumJoints, animation.NumJoints);
				Console.WriteLine (str);
				throw new Exception (str);
			}
			for (int j = 0; j < NumJoints; ++j) {
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

		public void VerifyJoints(SSSkeletalJoint[] joints)
		{
			if (this.NumJoints != joints.Length) {
				string str = string.Format (
					"Joint number mismatch: {0} in this hierarchy, {1} in other joints",
					this.NumJoints, joints.Length);
				Console.WriteLine (str);
				throw new Exception (str);
			}
			for (int j = 0; j < NumJoints; ++j) {
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

		public void ApplyAnimationChannels(List<SSSkeletalAnimationChannelRuntime> channels)
		{
			foreach (int j in _topLevelJoints) {
				traverseWithChannels (j, channels, null, null);
			}
		}

		/// <summary>
		/// Updating all joints manually requires settings all of the joints and in order
		/// </summary>
		public void SetJointLocation(int jointIdx, Vector3 pos, Quaternion orient)
		{
			var joint = _joints [jointIdx];
			joint.CurrentLocation.Position = pos;
			joint.CurrentLocation.Orientation = orient;
			int parentIdx = joint.BaseInfo.ParentIndex;
			if (parentIdx != -1) {
				joint.CurrentLocation.ApplyParentTransform (_joints [parentIdx].CurrentLocation);
			}
		}

		private void traverseWithChannels(int jointIdx, 
			List<SSSkeletalAnimationChannelRuntime> channels,
			SSSkeletalAnimationChannelRuntime activeChannel,
			SSSkeletalAnimationChannelRuntime fallbackActiveChannel)
		{
			foreach (var channel in channels) {
				if (channel.IsActive && channel.TopLevelActiveJoints.Contains (jointIdx)) {
					if (activeChannel != null) {
						fallbackActiveChannel = activeChannel;
					}
					activeChannel = channel;
				}
			}
			SSSkeletalJointRuntime joint = _joints [jointIdx];

			if (activeChannel == null) {
				joint.CurrentLocation = joint.BaseInfo.BaseLocation;
				//GL.Color4 (Color4.LightSkyBlue); // debugging
			} else {
				int parentIdx = joint.BaseInfo.ParentIndex;
				SSSkeletalJointLocation activeLoc = activeChannel.ComputeJointFrame (jointIdx);
				if (joint.BaseInfo.ParentIndex != -1) {
					activeLoc.ApplyParentTransform (_joints [parentIdx].CurrentLocation);
				}
				//activeLoc = activeChannel.ComputeJointFrame (jointIdx);
				if (activeChannel.InterChannelFade && activeChannel.InterChannelFadeIntensity < 1f) {
					// TODO smarter, multi layer fallback
					SSSkeletalJointLocation fallbackLoc;
					if (fallbackActiveChannel == null || fallbackActiveChannel.IsFadingOut) {
						// fall back to bind bose
						fallbackLoc = joint.BaseInfo.BaseLocation;
						GL.Color4 (Color4.LightGoldenrodYellow); // debugging
					} else {
						fallbackLoc = fallbackActiveChannel.ComputeJointFrame (jointIdx);
						fallbackLoc.ApplyParentTransform (_joints [parentIdx].CurrentLocation);
					}
					float activeChannelRatio = activeChannel.InterChannelFadeIntensity;
					GL.Color3(activeChannelRatio, activeChannelRatio, 1f - activeChannelRatio);
					joint.CurrentLocation = SSSkeletalJointLocation.Interpolate (
						fallbackLoc, activeLoc, activeChannelRatio);
				} else {
					joint.CurrentLocation = activeLoc;
				}
			}

			foreach (int child in joint.Children) {
				traverseWithChannels (child, channels, activeChannel, fallbackActiveChannel);
			}
		}
	}
}

