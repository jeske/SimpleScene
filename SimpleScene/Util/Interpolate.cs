using System.Collections.Generic;
using System;

namespace SimpleScene.Util
{
	public static class Interpolate {
        static public float Lerp(float start, float finish, float ammount)
        {
            // TODO: template this?
            return start + (finish - start) * ammount;
        }
    }

	public interface IInterpolater 
	{
		float Compute (float start, float finish, float ammount);
	}

	public class LerpInterpolater : IInterpolater
	{
		public float Compute(float start, float finish, float ammount)
		{
			return Interpolate.Lerp(start, finish, ammount);
		}
	}

	/// <summary>
	/// For modeling Attack-Delay-Sustain-Release levels of anyting
	/// https://en.wikipedia.org/wiki/Synthesizer#ADSR_envelope
	/// </summary>
	public class ADSREnvelope
	{
		public float AttackDuration = 1f;
		public float DecayDuration = 1f;
		public float SustainDuration = 1f;
		public float ReleaseDuration = 1f;

		public float TotalDuration {
			get { return AttackDuration + DecayDuration + SustainDuration + ReleaseDuration; }
		}

		IInterpolater AttackInterpolater = new LerpInterpolater();
		IInterpolater DecayInterpolater = new LerpInterpolater();
		IInterpolater ReleaseInterpolater = new LerpInterpolater();

		public float Amplitude = 1f;
		public float SustainLevel = 0.5f;

		public float ComputeLevel(float time)
		{
            if (time < 0f) {
                return 0f;
            }

			if (time < AttackDuration) {
				return AttackInterpolater.Compute (0f, Amplitude, time / AttackDuration);
			}
			time -= AttackDuration;

			if (time < DecayDuration) {
				return DecayInterpolater.Compute (Amplitude, SustainLevel, time / DecayDuration);
			}
			time -= DecayDuration;

			if (time < SustainDuration) {
				return SustainLevel;
			}
			time -= SustainDuration;

			if (time < ReleaseDuration) {
				ReleaseInterpolater.Compute (SustainLevel, 0f, time / SustainDuration);
			}
			return 0f;
		}
	}
}

