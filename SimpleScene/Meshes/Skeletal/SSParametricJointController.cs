using System;
using OpenTK;

namespace SimpleScene
{
	/// <summary>
	/// Define only a method by which a joint creates a transform
	/// </summary>
	public abstract class SSParametricJointController : SSSkeletalChannelController
	{
		public SSSkeletalJointLocation baseOffset = SSSkeletalJointLocation.Identity;
		protected readonly int _jointIdx;

		public SSParametricJointController(int jointIdx)
		{
			_jointIdx = jointIdx;
		}

		public override bool isActive (SSSkeletalJointRuntime joint)
		{
			return joint.BaseInfo.JointIndex == _jointIdx;
		}

		public override SSSkeletalJointLocation computeJointLocation (SSSkeletalJointRuntime joint)
		{
			if (joint.BaseInfo.JointIndex != _jointIdx) {
				throw new Exception ("computeJointLocation() called for a wrong joint");
			}
			var jointLoc = _createTransform ();
			jointLoc.ApplyPrecedingTransform (baseOffset);
			if (joint.Parent != null) {
				jointLoc.ApplyPrecedingTransform (joint.Parent.CurrentLocation);
			}
			return jointLoc;
		}

		protected abstract SSSkeletalJointLocation _createTransform();
	}

	// ----------------------------------------------------------

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
	public class SSPolarJointController : SSParametricJointController
	{
		public SSComparableJointParameter<float> theta 
			= new SSComparableJointParameter<float>(float.NegativeInfinity, float.PositiveInfinity, 0f);
		public SSComparableJointParameter<float> phi
			= new SSComparableJointParameter<float>(float.NegativeInfinity, float.PositiveInfinity, 0f);
		public SSComparableJointParameter<float> r
			= new SSComparableJointParameter<float>(float.NegativeInfinity, float.PositiveInfinity, 0f);

		public SSPolarJointController(int jointIdx)
			: base(jointIdx) { }

		public SSPolarJointController(SSSkeletalHierarchyRuntime hierarchy, string jointName)
			: this(hierarchy.jointIndex(jointName)) { }

		protected override SSSkeletalJointLocation _createTransform ()
		{
			SSSkeletalJointLocation ret;
			ret.Position = new Vector3 (r.value, 0f, 0f);

			var thetaRot = Quaternion.FromAxisAngle (Vector3.UnitZ, theta.value);
			var phiRot = Quaternion.FromAxisAngle (Vector3.UnitY, phi.value);
			//ret.Orientation = Quaternion.Multiply (phiRot, thetaRot);
			ret.Orientation = Quaternion.Multiply (thetaRot, phiRot);
			return ret;
		}
	}
}

