using System;
using System.Collections.Generic;
using SimpleScene.Util;
using OpenTK;

namespace SimpleScene
{
    public abstract class SSParticleEffector
    {
		protected static Random s_rand = new Random ();

		/// <summary>
		/// Convenience function.
		/// </summary>
		static protected float nextFloat()
		{
			return (float)s_rand.NextDouble();
		}

		public byte EffectorMask = new SSParticle().EffectorMask; // initialize to a default

		public void Simulate (SSParticle particle, float deltaT)
		{
			if ((particle.EffectorMask & this.EffectorMask) != 0) {
				simulateEffector (particle, deltaT);
			}
		}

		protected abstract void simulateEffector (SSParticle particle, float deltaT);
        // For example, particle.Vel += new Vector3(1f, 0f, 0f) * dT simulates 
        // acceleration on the X axis. Multiple effectors will combine their 
        // acceleration effect to determine the final velocity of the particle.

        public virtual void Reset() { }
    }

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
		protected int m_activationsCount;

		public override void Reset()
		{
			m_initialDelay = EffectDelay;
			m_timeSinceLastEffect = float.PositiveInfinity;
			m_activationsCount = 0;
			m_nextEffect = 0f;
		}

		protected override sealed void simulateEffector(SSParticle particle, float deltaT)
		{
			if (m_initialDelay > 0f) {
				// if initial delay is needed
				m_initialDelay -= deltaT;
				if (m_initialDelay > 0f) {
					return;
				}
			}

			if (ActivationsCount > 0 && m_activationsCount < ActivationsCount) {
				return;
			}

			m_timeSinceLastEffect += deltaT;
			if (m_timeSinceLastEffect > m_nextEffect) {
				activatePeriodic ();
				++ActivationsCount;
				m_timeSinceLastEffect = 0f;
				m_nextEffect = Interpolate.Lerp(EffectIntervalMin, EffectIntervalMax, 
					(float)s_rand.NextDouble());
			}

			simulatePeriodic (particle, deltaT);
		}

		protected virtual void simulatePeriodic (SSParticle particle, float deltaT) { }

		protected abstract void activatePeriodic();
	}


	public class SSExpolosionsEffector : SSPeriodicEffector
    {
		public float ExplosiveForceMin = 0.2f;
		public float ExplosiveForceMax = 0.2f;
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

		public SSExpolosionsEffector()
		{
			m_blasts = new List<BlastInfo> ();

			m_adsr.Amplitude = 1f;
			m_adsr.SustainLevel = 0.5f;

			m_adsr.AttackDuration = 0.01f;
			m_adsr.DecayDuration = 0.01f;
			m_adsr.SustainDuration = 0.05f;
			m_adsr.ReleaseDuration = 0.01f;
		}

		protected override void simulatePeriodic(SSParticle particle, float timeDelta)
		{
			for (int i = 0; i < m_blasts.Count; ++i) {
				BlastInfo bi = m_blasts [i];

				float acc = m_adsr.ComputeLevel(bi.TimeElapsed) * bi.MaxForceMagnitude / particle.Mass;
				Vector3 dir = (particle.Pos - bi.Center).Normalized ();
				particle.Vel += (acc * dir);

				bi.TimeElapsed += timeDelta;
				if (bi.TimeElapsed > m_adsr.TotalDuration) {
					m_blasts.RemoveAt (i);
					i--;
				}
			}
		}

		protected override void activatePeriodic ()
		{
			BlastInfo bi = new BlastInfo ();
			bi.Center.X = Interpolate.Lerp (CenterMin.X, CenterMax.X, nextFloat ());
			bi.Center.Y = Interpolate.Lerp (CenterMin.Y, CenterMax.Y, nextFloat ());
			bi.Center.Z = Interpolate.Lerp (CenterMin.Z, CenterMax.Z, nextFloat ());
			bi.MaxForceMagnitude = Interpolate.Lerp (ExplosiveForceMin, ExplosiveForceMax, nextFloat ());
			bi.TimeElapsed = 0f;
			m_blasts.Add (bi);
		}

		protected class BlastInfo {
			public Vector3 Center;
			public float MaxForceMagnitude;
			public float TimeElapsed;
		}
    }

}

