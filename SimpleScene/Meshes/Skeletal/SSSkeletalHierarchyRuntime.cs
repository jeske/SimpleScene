using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL; // debug hacks

namespace SimpleScene
{
	public class SSSkeletalJointRuntime
	{
		public List<SSSkeletalJointRuntime> children = new List<SSSkeletalJointRuntime>();
		public SSSkeletalJointRuntime parent = null;

		public SSSkeletalJointLocation currentLocation;

		protected SSSkeletalJoint _baseInfo;

		public SSSkeletalJoint baseInfo {
			get { return _baseInfo; }
		}

		public SSSkeletalJointRuntime(SSSkeletalJoint baseInfo)
		{
			_baseInfo = baseInfo;
			currentLocation = _baseInfo.bindPoseLocation;
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
				int parentIdx = jointInput.parentIndex;
				if (parentIdx < 0) {
					_topLevelJoints.Add (j);
				} else {
					_joints [j].parent = _joints [parentIdx];
					_joints [parentIdx].children.Add (_joints[j]);
				}
			}
		}

		public int jointIndex(string jointName)
		{
			if (String.Compare (jointName, "all", true) == 0) {
				return -1;
			}

			for (int j = 0; j < _joints.Length; ++j) {
				if (_joints [j].baseInfo.name == jointName) {
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
			return _joints [jointIdx].currentLocation;
		}

		public int[] topLevelJoints {
			get { return _topLevelJoints.ToArray (); }
		}

		public void verifyAnimation(SSSkeletalAnimation animation)
		{
			if (this.numJoints != animation.numJoints) {
				string str = string.Format (
					"Joint number mismatch: {0} in md5mesh, {1} in md5anim",
					this.numJoints, animation.numJoints);
				Console.WriteLine (str);
				throw new Exception (str);
			}
			for (int j = 0; j < numJoints; ++j) {
				SSSkeletalJoint thisJointInfo = this._joints [j].baseInfo;
				SSSkeletalJoint animJointInfo = animation.jointHierarchy [j];
				if (thisJointInfo.name != animJointInfo.name) {
					string str = string.Format (
						"Joint name mismatch: {0} in md5mesh, {1} in md5anim",
						thisJointInfo.name, animJointInfo.name);
					Console.WriteLine (str);
					throw new Exception (str);
				}
				if (thisJointInfo.parentIndex != animJointInfo.parentIndex) {
					string str = string.Format (
						"Hierarchy parent mismatch for joint \"{0}\": {1} in md5mesh, {2} in md5anim",
						thisJointInfo.name, thisJointInfo.parentIndex, animJointInfo.parentIndex);
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
				SSSkeletalJoint thisJointInfo = this._joints [j].baseInfo;
				SSSkeletalJoint otherJointInfo = joints [j];
				if (thisJointInfo.name != otherJointInfo.name) {
					string str = string.Format (
						"Joint name mismatch: {0} in this hierarchy, {1} in other joints",
						thisJointInfo.name, otherJointInfo.name);
					Console.WriteLine (str);
					throw new Exception (str);
				}
				if (thisJointInfo.parentIndex != otherJointInfo.parentIndex) {
					string str = string.Format (
						"Hierarchy parent mismatch for joint \"{0}\": {1} in this hierarchy, {2} in other joints",
						thisJointInfo.name, thisJointInfo.parentIndex, otherJointInfo.parentIndex);
					Console.WriteLine (str);
					throw new Exception (str);
				}
			}
		}

		public void applySkeletalControllers(List<SSSkeletalChannelController> channelControllers)
		{
			foreach (int j in _topLevelJoints) {
				_traverseWithControllers (_joints[j], channelControllers);
			}
		}

		private void _traverseWithControllers(SSSkeletalJointRuntime joint, List<SSSkeletalChannelController> controllers)
		{
			joint.currentLocation = _computeJointLocWithControllers (joint, controllers, controllers.Count - 1);

			foreach (var child in joint.children) {
				_traverseWithControllers (child, controllers);
			}
		}

		private SSSkeletalJointLocation _computeJointLocWithControllers(
			SSSkeletalJointRuntime joint, List<SSSkeletalChannelController> controllers, int controllerIdx)
		{
			var channel = controllers [controllerIdx];
			if (channel.isActive(joint)) {
				var channelLoc = channel.computeJointLocation (joint);
				if (joint.parent != null) {
					channelLoc.applyPrecedingTransform (joint.parent.currentLocation);
				}
				if (!channel.interChannelFade 
					|| channel.interChannelFadeIndentisy() >= 1f 
					|| controllerIdx == 0) {
					return channelLoc;
				} else {
					var fallbackLoc 
					= _computeJointLocWithControllers (joint, controllers, controllerIdx - 1);
					return SSSkeletalJointLocation.interpolate (
						fallbackLoc, channelLoc, channel.interChannelFadeIndentisy());
				}
			} else {
				if (controllerIdx > 0) {
					return _computeJointLocWithControllers (joint, controllers, controllerIdx - 1);
				} else {
					throw new Exception ("fell through without an active skeletal channel controller");
				}
			}

		}
	}
}

