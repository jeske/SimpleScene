using System;
using OpenTK;

namespace SimpleScene
{
	/// <summary>
	/// Define only a method by which a joint creates a transform
	/// </summary>
	public abstract class SSAbstractControlJoint
	{
		public SSSkeletalJointLocation baseOffset;

		public SSSkeletalJointLocation getTransform() {
			var value = _createTransform();
			value.ApplyPrecedingTransform (baseOffset);
			return value;
		}

		protected abstract SSSkeletalJointLocation _createTransform();
	}

	// ----------------------------------------------------------

	public class SSSimpleJointParameter<T>
	{
		public virtual T value { get; set; }
	}

	public class SSJointRangeParameter<T> : SSSimpleJointParameter<T> where T: IComparable
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

		public SSJointRangeParameter(T min, T max, T val)
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
	public class SSPolarControlJoint : SSAbstractControlJoint
	{
		SSJointRangeParameter<float> theta 
			= new SSJointRangeParameter<float>(-(float)Math.PI, +(float)Math.PI, 0f);
		SSJointRangeParameter<float> phi
			= new SSJointRangeParameter<float>(-(float)Math.PI/2f, +(float)Math.PI/2f, 0f);
		SSJointRangeParameter<float> r
			= new SSJointRangeParameter<float>(0f, 1f, 1f);

		protected override SSSkeletalJointLocation _createTransform ()
		{
			SSSkeletalJointLocation ret;
			ret.Position = new Vector3 (r.value, 0f, 0f);

			var thetaRot = Quaternion.FromAxisAngle (Vector3.UnitZ, theta.value);
			var phiRot = Quaternion.FromAxisAngle (Vector3.UnitY, phi.value);
			ret.Orientation = Quaternion.Multiply (phiRot, thetaRot);
			return ret;
		}
	}
}

