using System;
using System.Collections.Generic;
using SimpleScene.Util;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{
	[Serializable]
	public struct SSKeyFrame<T>
	{
		public float key;
		public T value;

		public SSKeyFrame(float k, T v) {
			key = k;
			value = v;
		}
	}

    public abstract class SSParticleEffector
    {
		public enum MatchFunction { And, Equals };

        public delegate void EffectorCallback(SSParticle particle);

		protected static Random _rand = new Random ();

		/// <summary>
		/// Convenience function.
		/// </summary>
		static protected float nextFloat()
		{
			return (float)_rand.NextDouble();
		}

		public ushort effectorMask = ushort.MaxValue; // initialize to a default
		public MatchFunction maskMatchFunction = MatchFunction.And;
        public EffectorCallback preRemoveHook = null;
        public EffectorCallback preAddHook = null;

		protected float _timeSinceReset = 0f;

		public float timeSinceReset {
			get { return _timeSinceReset; }
		}

        /// <summary>
        /// Defines the condition for effecting a particle
        /// </summary>
        public bool effectorMaskCheck(ushort mask)
        {
            bool match;
            if (maskMatchFunction == MatchFunction.And) {
                match = ((mask & this.effectorMask) != 0);
            } else { // Equals
                match = (mask == this.effectorMask);
            }
            return match;
        }

		/// <summary>
		/// Allows effector to do some housekeeping, once per frame
		/// </summary>
		public virtual void simulateSelf (float deltaT) 
		{ 
			_timeSinceReset += deltaT;
		}


		/// <summary>
		/// Essentially wrapper for effectParticle() with some masking logic for advanced particle-effector
		/// relantionships
		/// 
		/// Override with caution.
		/// </summary>
		public void simulateParticleEffect (SSParticle particle, float deltaT)
		{
            if (effectorMaskCheck(particle.effectorMask)) {
				effectParticle (particle, deltaT);
			}
		}

		/// <summary>
		/// Where the logic for changing particle actually takes place
		/// 
		/// For example, particle.Vel += new Vector3(1f, 0f, 0f) * dT simulates 
		/// acceleration on the X axis. Multiple effectors will combine their 
		/// acceleration effect to determine the final velocity of the particle.
		//
		/// </summary>
		protected abstract void effectParticle (SSParticle particle, float deltaT);

		/// <summary>
		/// Effectors should initialize/reset their variables here
		/// </summary>
        public virtual void reset() 
		{ 
			_timeSinceReset = 0f;
		}
    }

	public abstract class SSKeyframesEffector<T> : SSParticleEffector
	{
		/// <summary>
		/// When not NaN will be used to adjust keyframe control to individual particles' Lifes instead
		/// of time since last reset
		/// </summary>
		public float particleLifetime = float.NaN;

		/// <summary>
		/// Pairs of time elapsed matched to keyframe values
		/// </summary>
		public SortedList<float,T> keyframes = new SortedList<float, T> ();

		/// <summary>
		/// Used to interpolate values between keyframes. If there are not enough of these to cover all
		/// keyframe inbetweens the last interpolater will be used repeatedly.
		/// </summary>
		public IInterpolater[] interpolaters = { new LinearInterpolater() }; // default to using LERP for everything

		public SSKeyframesEffector()
		{
		}

		public SSKeyframesEffector(List<SSKeyFrame<T>> kframes)
		{
			foreach (var kvp in kframes) {
				keyframes.Add (kvp.key, kvp.value);
			}
		}

		protected override sealed void effectParticle(SSParticle particle, float deltaT)
		{
			_timeSinceReset += deltaT;
			float timeElapsed = float.IsNaN (particleLifetime) ? _timeSinceReset
                : (1f - particle.life / particleLifetime);
			float lastKey = keyframes.Keys [keyframes.Count - 1];
			if (timeElapsed > lastKey) {
				applyValue(particle, keyframes [lastKey]);
			} else {
				float prevKey = keyframes.Keys [0];
				for (int i = 1; i < keyframes.Keys.Count; ++i) {
					float key = keyframes.Keys [i];
					if (timeElapsed < key) {
						IInterpolater interpolater = 
							i < interpolaters.Length ? interpolaters [i] 
													 : interpolaters[interpolaters.Length - 1];
						float progression = (timeElapsed - prevKey) / (key - prevKey);
						T value = computeValue (interpolater, keyframes [prevKey], keyframes [key], progression);
						applyValue(particle, value);
						break;
					}
					prevKey = key;
				}
			}
		}

		protected abstract T computeValue (IInterpolater interpolater, 
										   T prevFrame, T nextKeyframe, float ammount);

		protected abstract void applyValue (SSParticle particle, T value);
	}

	public class SSMasterScaleKeyframesEffector : SSKeyframesEffector<float>
	{
		public float amplification = 1.0f;

		protected override float computeValue (IInterpolater interpolater, 
											   float prevKeyframe, float nextKeyframe, float ammount)
		{
			return interpolater.compute (prevKeyframe, nextKeyframe, ammount);

		}

		protected override void applyValue(SSParticle particle, float value)
		{
			particle.masterScale = amplification * value;
		}
	};

	public class SSComponentScaleKeyframeEffector : SSKeyframesEffector<Vector3>
	{
		public Vector3 amplification = Vector3.One;
		public Vector3 baseOffset = Vector3.Zero;

		protected override Vector3 computeValue (IInterpolater interpolater, 
												 Vector3 prevKeyframe, Vector3 nextKeyframe, float ammount)
		{
			return new Vector3 (
				interpolater.compute (prevKeyframe.X, nextKeyframe.X, ammount),
				interpolater.compute (prevKeyframe.Y, nextKeyframe.Y, ammount),
				interpolater.compute (prevKeyframe.Z, nextKeyframe.Z, ammount)
			);
		}

		protected override void applyValue(SSParticle particle, Vector3 value)
		{
			particle.componentScale = baseOffset + amplification * value;
		}
	}

	public class SSColorKeyframesEffector : SSKeyframesEffector<Color4>
	{
		public Color4 colorMask = new Color4 (1.0f, 1.0f, 1.0f, 1.0f);

		public SSColorKeyframesEffector() {	}

		public SSColorKeyframesEffector(List<SSKeyFrame<Color4>> kframes) : base(kframes) {	}

		protected override Color4 computeValue (IInterpolater interpolater, Color4 prevKeyframe, Color4 nextKeyframe, float ammount)
		{
			return new Color4 (
				interpolater.compute (prevKeyframe.R, nextKeyframe.R, ammount),
				interpolater.compute (prevKeyframe.G, nextKeyframe.G, ammount),
				interpolater.compute (prevKeyframe.B, nextKeyframe.B, ammount),
				interpolater.compute (prevKeyframe.A, nextKeyframe.A, ammount)
			);
		}

		protected override void applyValue (SSParticle particle, Color4 value)
		{
			value.R *= colorMask.R;
			value.G *= colorMask.G;
			value.B *= colorMask.B;
			value.A *= colorMask.A;
			particle.color = value;
		}
	};

	public abstract class SSPeriodicEffector : SSParticleEffector
	{
		public float effectDelay = 0f;

		public float effectIntervalMin = 1f;
		public float effectIntervalMax = 1f;
		public float effectInterval {
			set { effectIntervalMin = effectIntervalMax = value; }
		}

		public int activationsCount = 0; // 0 or less means infinite periodic activation

		protected float _initialDelay;
		protected float _timeSinceLastEffect;
		protected float _nextEffect;
		protected int _activationsDone;

		public override void reset()
		{
			base.reset ();
			_initialDelay = effectDelay;
			_timeSinceLastEffect = float.PositiveInfinity;
			_activationsDone = 0;
			_nextEffect = 0f;
			resetPeriodic ();
		}

		public override sealed void simulateSelf(float deltaT)
		{
			base.simulateSelf (deltaT);

			if (_initialDelay > 0f) {
				// if initial delay is needed
				_initialDelay -= deltaT;
				if (_initialDelay > 0f) {
					return;
				}
			}

			if (activationsCount > 0 && _activationsDone > activationsCount) {
				return;
			}

			_timeSinceLastEffect += deltaT;
			if (_timeSinceLastEffect > _nextEffect) {
				activatePeriodic ();
				++_activationsDone;
				_timeSinceLastEffect = 0f;
				_nextEffect = Interpolate.Lerp(effectIntervalMin, effectIntervalMax, 
					(float)_rand.NextDouble());
			}

			simulateSelfPeriodic (deltaT);
		}

		protected override sealed void effectParticle(SSParticle particle, float deltaT)
		{
			if (_initialDelay > 0f) {
				return;
			}

			effectParticlePeriodic (particle, deltaT);
		}

		protected virtual void resetPeriodic() { }
		protected virtual void simulateSelfPeriodic(float deltaT) { }
		protected virtual void effectParticlePeriodic (SSParticle particle, float deltaT) { }

		protected abstract void activatePeriodic();
	}

	public class SSPeriodicExplosiveForceEffector : SSPeriodicEffector
    {
		public delegate void ExplosiveHandlerType (Vector3 position, float explosiveForce);

		public ExplosiveHandlerType explosionEventHandlers = null;

		public float explosiveForceMin = 0.1f;
		public float explosiveForceMax = 0.1f;
		public float explosiveForce {
			set { explosiveForceMin = explosiveForceMax = value; }
		}

		public Vector3 centerMin = new Vector3(0f);
		public Vector3 centerMax = new Vector3(0f);
		public Vector3 center {
			set { centerMin = centerMax = value; }
		}

		protected LinearADSREnvelope _adsr;
		protected List<BlastInfo> _blasts;

		public SSPeriodicExplosiveForceEffector()
		{
			_adsr = new LinearADSREnvelope ();
			_adsr.peakLevel = 1f;
			_adsr.sustainLevel = 0.5f;

			_adsr.attackDuration = 0.01f;
			_adsr.decayDuration = 0.01f;
			_adsr.sustainDuration = 0.05f;
			_adsr.releaseDuration = 0.01f;
		}

		protected override void resetPeriodic()
		{
			_blasts = new List<BlastInfo> ();
		}

		protected override void simulateSelfPeriodic(float timeDelta)
		{
			for (int i = 0; i < _blasts.Count; ++i) {
				BlastInfo bi = _blasts [i];

				bi.timeElapsed += timeDelta;
				if (bi.timeElapsed > _adsr.totalDuration) {
					_blasts.RemoveAt (i);
					i--;
				}
			}
		}

		protected override void effectParticlePeriodic(SSParticle particle, float timeDelta)
		{
			for (int i = 0; i < _blasts.Count; ++i) {
				BlastInfo bi = _blasts [i];

				// TODO inverse square law or something similar
				Vector3 dist = particle.pos - bi.center;
				if (dist != Vector3.Zero) {
					float acc = _adsr.computeLevel (bi.timeElapsed) * bi.forceMagnitude 
							  / dist.LengthSquared / particle.mass;
					Vector3 dir = (particle.pos - bi.center).Normalized ();
					particle.vel += (acc * dir);
				}
			}
		}

		protected override void activatePeriodic ()
		{
			BlastInfo bi = new BlastInfo ();
			bi.center.X = Interpolate.Lerp (centerMin.X, centerMax.X, nextFloat ());
			bi.center.Y = Interpolate.Lerp (centerMin.Y, centerMax.Y, nextFloat ());
			bi.center.Z = Interpolate.Lerp (centerMin.Z, centerMax.Z, nextFloat ());
			bi.forceMagnitude = Interpolate.Lerp (explosiveForceMin, explosiveForceMax, nextFloat ());
			bi.timeElapsed = 0f;
			_blasts.Add (bi);
			if (explosionEventHandlers != null) {
				explosionEventHandlers (bi.center, bi.forceMagnitude);
			}
		}

		protected class BlastInfo {
			public Vector3 center;
			public float forceMagnitude;
			public float timeElapsed;
		}
    }

    public class SRadialBillboardOrientator : SSParticleEffector
    {
        protected float _orientationX = 0f;

        /// <summary>
        /// Compute orientation around X once per frame to orient the sprites towards the viewer
        /// </summary>
        public void updateModelView(ref Matrix4 modelViewMatrix)
        {
            Quaternion quat = modelViewMatrix.ExtractRotation();
            // x-orient
            Vector3 test1 = new Vector3(0f, 1f, 0f);
            Vector3 test2 = Vector3.Transform(test1, quat);
            float dot = Vector3.Dot(test1, test2);
            float angle = (float)Math.Acos(dot);
            if (test2.Z < 0f) {
                angle = -angle;
            } 
            _orientationX = -angle;
        }

        protected override void effectParticle (SSParticle particle, float deltaT)
        {
            Vector3 dir = particle.vel;

            // orient to look right
            float x = dir.X;
            float y = dir.Y;
            float z = dir.Z;
            float xy = dir.Xy.Length;
            float phi = (float)Math.Atan (z / xy);
            float theta = (float)Math.Atan2 (y, x);

            particle.orientation.Y = -phi;
            particle.orientation.Z = theta;
            particle.orientation.X = -_orientationX;
        }
    }
}

