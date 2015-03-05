using System;
using System.Collections.Generic;
using SimpleScene.Util;
using OpenTK;
using OpenTK.Graphics;

namespace SimpleScene
{
    public abstract class SSParticleEffector
    {
		public enum MatchFunction { And, Equals };

		protected static Random s_rand = new Random ();

		/// <summary>
		/// Convenience function.
		/// </summary>
		static protected float nextFloat()
		{
			return (float)s_rand.NextDouble();
		}

		public ushort EffectorMask = ushort.MaxValue; // initialize to a default
		public MatchFunction MaskMathFunction = MatchFunction.And;

		protected float m_timeSinceReset = 0f;

		public float TimeSinceReset {
			get { return m_timeSinceReset; }
		}

		/// <summary>
		/// Allows effector to do some housekeeping, once per frame
		/// </summary>
		public virtual void SimulateSelf (float deltaT) 
		{ 
			m_timeSinceReset += deltaT;
		}


		/// <summary>
		/// Essentially wrapper for effectParticle() with some masking logic for advanced particle-effector
		/// relantionships
		/// 
		/// Override with caution.
		/// </summary>
		public void SimulateParticleEffect (SSParticle particle, float deltaT)
		{
			bool match;
			if (MaskMathFunction == MatchFunction.And) {
				match = ((particle.EffectorMask & this.EffectorMask) != 0);
			} else { // Equals
				match = (particle.EffectorMask == this.EffectorMask);
			}
			if (match) {
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
        public virtual void Reset() 
		{ 
			m_timeSinceReset = 0f;
		}
    }

	public abstract class SSKeyframesEffector<T> : SSParticleEffector
	{
		/// <summary>
		/// When not NaN will be used to adjust keyframe control to individual particles' Lifes instead
		/// of time since last reset
		/// </summary>
		public float ParticleLifetime = float.NaN;

		/// <summary>
		/// Pairs of time elapsed matched to keyframe values
		/// </summary>
		public SortedList<float,T> Keyframes = new SortedList<float, T> ();

		/// <summary>
		/// Used to interpolate values between keyframes. If there are not enough of these to cover all
		/// keyframe inbetweens the last interpolater will be used repeatedly.
		/// </summary>
		public IInterpolater[] Interpolaters = { new LinearInterpolater() }; // default to using LERP for everything

		protected override sealed void effectParticle(SSParticle particle, float deltaT)
		{
			m_timeSinceReset += deltaT;
			float timeElapsed = float.IsNaN (ParticleLifetime) ? m_timeSinceReset
															   : ParticleLifetime - particle.Life;
			float lastKey = Keyframes.Keys [Keyframes.Count - 1];
			if (timeElapsed > lastKey) {
				applyValue(particle, Keyframes [lastKey]);
			} else {
				float prevKey = Keyframes.Keys [0];
				for (int i = 1; i < Keyframes.Keys.Count; ++i) {
					float key = Keyframes.Keys [i];
					if (timeElapsed < key) {
						IInterpolater interpolater = 
							i < Interpolaters.Length ? Interpolaters [i] 
													 : Interpolaters[Interpolaters.Length - 1];
						float progression = (timeElapsed - prevKey) / (key - prevKey);
						T value = computeValue (interpolater, Keyframes [prevKey], Keyframes [key], progression);
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
		public float Amplification = 1.0f;

		protected override float computeValue (IInterpolater interpolater, 
											   float prevKeyframe, float nextKeyframe, float ammount)
		{
			return interpolater.Compute (prevKeyframe, nextKeyframe, ammount);

		}

		protected override void applyValue(SSParticle particle, float value)
		{
			particle.MasterScale = Amplification * value;
		}
	};

	public class SSColorKeyframesEffector : SSKeyframesEffector<Color4>
	{
		public Color4 ColorMask = new Color4 (1.0f, 1.0f, 1.0f, 1.0f);

		protected override Color4 computeValue (IInterpolater interpolater, Color4 prevKeyframe, Color4 nextKeyframe, float ammount)
		{
			return new Color4 (
				interpolater.Compute (prevKeyframe.R, nextKeyframe.R, ammount),
				interpolater.Compute (prevKeyframe.G, nextKeyframe.G, ammount),
				interpolater.Compute (prevKeyframe.B, nextKeyframe.B, ammount),
				interpolater.Compute (prevKeyframe.A, nextKeyframe.A, ammount)
			);
		}

		protected override void applyValue (SSParticle particle, Color4 value)
		{
			value.R *= ColorMask.R;
			value.G *= ColorMask.G;
			value.B *= ColorMask.B;
			value.A *= ColorMask.A;
			particle.Color = value;
		}
	};

	public abstract class SSPeriodicEffector : SSParticleEffector
	{
		public float EffectDelay = 0f;

		public float EffectIntervalMin = 1f;
		public float EffectIntervalMax = 1f;
		public float EffectInterval {
			set { EffectIntervalMin = EffectIntervalMax = value; }
		}

		public int ActivationsCount = 0; // 0 or less means infinite periodic activation

		protected float m_initialDelay;
		protected float m_timeSinceLastEffect;
		protected float m_nextEffect;
		protected int m_activationsDone;

		public override void Reset()
		{
			base.Reset ();
			m_initialDelay = EffectDelay;
			m_timeSinceLastEffect = float.PositiveInfinity;
			m_activationsDone = 0;
			m_nextEffect = 0f;
			resetPeriodic ();
		}

		public override sealed void SimulateSelf(float deltaT)
		{
			base.SimulateSelf (deltaT);

			if (m_initialDelay > 0f) {
				// if initial delay is needed
				m_initialDelay -= deltaT;
				if (m_initialDelay > 0f) {
					return;
				}
			}

			if (ActivationsCount > 0 && m_activationsDone > ActivationsCount) {
				return;
			}

			m_timeSinceLastEffect += deltaT;
			if (m_timeSinceLastEffect > m_nextEffect) {
				activatePeriodic ();
				++m_activationsDone;
				m_timeSinceLastEffect = 0f;
				m_nextEffect = Interpolate.Lerp(EffectIntervalMin, EffectIntervalMax, 
					(float)s_rand.NextDouble());
			}

			simulateSelfPeriodic (deltaT);
		}

		protected override sealed void effectParticle(SSParticle particle, float deltaT)
		{
			if (m_initialDelay > 0f) {
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

		public ExplosiveHandlerType ExplosionEventHandlers = null;

		public float ExplosiveForceMin = 0.1f;
		public float ExplosiveForceMax = 0.1f;
		public float ExplosiveForce {
			set { ExplosiveForceMin = ExplosiveForceMax = value; }
		}

		public Vector3 CenterMin = new Vector3(0f);
		public Vector3 CenterMax = new Vector3(0f);
		public Vector3 Center {
			set { CenterMin = CenterMax = value; }
		}

		protected ADSREnvelope m_adsr;
		protected List<BlastInfo> m_blasts;

		public SSPeriodicExplosiveForceEffector()
		{
			m_adsr = new ADSREnvelope ();
			m_adsr.Amplitude = 1f;
			m_adsr.SustainLevel = 0.5f;

			m_adsr.AttackDuration = 0.01f;
			m_adsr.DecayDuration = 0.01f;
			m_adsr.SustainDuration = 0.05f;
			m_adsr.ReleaseDuration = 0.01f;
		}

		protected override void resetPeriodic()
		{
			m_blasts = new List<BlastInfo> ();
		}

		protected override void simulateSelfPeriodic(float timeDelta)
		{
			for (int i = 0; i < m_blasts.Count; ++i) {
				BlastInfo bi = m_blasts [i];

				bi.TimeElapsed += timeDelta;
				if (bi.TimeElapsed > m_adsr.TotalDuration) {
					m_blasts.RemoveAt (i);
					i--;
				}
			}
		}

		protected override void effectParticlePeriodic(SSParticle particle, float timeDelta)
		{
			for (int i = 0; i < m_blasts.Count; ++i) {
				BlastInfo bi = m_blasts [i];

				// TODO inverse square law or something similar
				Vector3 dist = particle.Pos - bi.Center;
				if (dist != Vector3.Zero) {
					float acc = m_adsr.ComputeLevel (bi.TimeElapsed) * bi.ForceMagnitude 
							  / dist.LengthSquared / particle.Mass;
					Vector3 dir = (particle.Pos - bi.Center).Normalized ();
					particle.Vel += (acc * dir);
				}
			}
		}

		protected override void activatePeriodic ()
		{
			BlastInfo bi = new BlastInfo ();
			bi.Center.X = Interpolate.Lerp (CenterMin.X, CenterMax.X, nextFloat ());
			bi.Center.Y = Interpolate.Lerp (CenterMin.Y, CenterMax.Y, nextFloat ());
			bi.Center.Z = Interpolate.Lerp (CenterMin.Z, CenterMax.Z, nextFloat ());
			bi.ForceMagnitude = Interpolate.Lerp (ExplosiveForceMin, ExplosiveForceMax, nextFloat ());
			bi.TimeElapsed = 0f;
			m_blasts.Add (bi);
			ExplosionEventHandlers (bi.Center, bi.ForceMagnitude);
		}

		protected class BlastInfo {
			public Vector3 Center;
			public float ForceMagnitude;
			public float TimeElapsed;
		}
    }

}

