using System;
using System.Collections.Generic;
using OpenTK;

namespace SimpleScene
{
	/// <summary>
	/// Define only a method by which a joint creates a transform
	/// </summary>
	public class SSParametricJointsController : SSSkeletalChannelController
	{
		protected Dictionary<int, SSParametricJoint> _joints = new Dictionary<int, SSParametricJoint>();

		public SSParametricJointsController()
		{
		}

		public void addJoint(int jointIdx, SSParametricJoint joint)
		{
			_joints [jointIdx] = joint;
		}

		public void removeJoint(int jointIdx)
		{
			_joints.Remove (jointIdx);
		}

		public override bool isActive (SSSkeletalJointRuntime joint)
		{
			return _joints.ContainsKey(joint.BaseInfo.JointIndex);
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			var jointLoc = _joints [joint.BaseInfo.JointIndex].computeJointLocation ();
			if (joint.Parent != null) {
				jointLoc.ApplyPrecedingTransform (joint.Parent.CurrentLocation);
			}
			return jointLoc;
		}
	}

	// ----------------------------------------------------------

	public abstract class SSParametricJoint 
	{
		/// <summary>
		/// In joint-local coordinates, as defined by individual parametric joints
		/// </summary>
		public abstract SSSkeletalJointLocation computeJointLocation ();
	}

	public class SSSimpleJointParameter<T>
	{
		public virtual T value { get; set; }
	}

	public class SSComparableJointParameter<T> : SSSimpleJointParameter<T> where T: IComparable
	{
		protected T _min;
		protected T _max;

		public T min {
			get { return _min; }
			set {
				_min = value;
				if (base.value.CompareTo(_min) < 0) {
					base.value = _min;
				}
			}
		}

		public T max {
			get { return _max; }
			set {
				_max = value;
				if (base.value.CompareTo(_max) > 0) {
					base.value = _max;
				}
			}
		}

		public override T value {
			get { return base.value; }
			set {
				if (value.CompareTo(_max) > 0 || value.CompareTo(_min) < 0) {
					var errMsg = string.Format (
			             "Joint parameter out of range: {0}; allowed range is [{1}:{2}]", 
			             value, _min, _max);
					System.Console.WriteLine (errMsg);
					throw new Exception (errMsg);
				}
				base.value = value;
			}
		}

		public SSComparableJointParameter(T min, T max, T val)
		{
			this.min = min;
			this.max = max;
			base.value = val;
		}
	}

	// ----------------------------------------------------------

	/// <summary>
	/// See Common Robot Configurations: http://nptel.ac.in/courses/112103174/module7/lec5/3.html
	/// </summary>
	public class SSPolarJoint : SSParametricJoint
	{
		public Vector3 thetaAxis = Vector3.UnitY;
		public Vector3 phiAxis = Vector3.UnitZ;
		public Vector3 positionOffset = Vector3.Zero; 

		public SSComparableJointParameter<float> theta 
			= new SSComparableJointParameter<float>(float.NegativeInfinity, float.PositiveInfinity, 0f);
		public SSComparableJointParameter<float> phi
			= new SSComparableJointParameter<float>(float.NegativeInfinity, float.PositiveInfinity, 0f);

		public override SSSkeletalJointLocation computeJointLocation ()
		{
			SSSkeletalJointLocation ret;
			ret.Position = positionOffset;

			var thetaRot = Quaternion.FromAxisAngle (thetaAxis, theta.value);
			var phiRot = Quaternion.FromAxisAngle (phiAxis, phi.value);
			ret.Orientation = Quaternion.Multiply (thetaRot, phiRot);
			return ret;
		}
	}
}

