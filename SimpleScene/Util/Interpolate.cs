using System.Collections.Generic;
using System;

namespace SimpleScene.Util
{
	public enum InterpolationType { Linear }; // TODO add quadratic etc

    class Interpolate {
        static public float Lerp(float start, float finish, float ammount)
        {
            // TODO: template this?
            return start + (finish - start) * ammount;
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

		public InterpolationType AttackCurve = InterpolationType.Linear;
		public InterpolationType DecayCurve = InterpolationType.Linear;
		public InterpolationType ReleaseCurve = InterpolationType.Linear;

		public float Amplitude = 1f;
		public float SustainLevel = 0.5f;

		public float ComputeLevel(float time)
		{
            if (time < 0f) {
                return 0f;
            }

			if (time < AttackDuration) {
				if (AttackCurve != InterpolationType.Linear) {
					throw new NotImplementedException();
				}
				return Interpolate.Lerp(0f, Amplitude, time/AttackDuration);
			}
			time -= AttackDuration;

			if (time < DecayDuration) {
				if (DecayCurve != InterpolationType.Linear) {
					throw new NotImplementedException();
				}
				return Interpolate.Lerp(Amplitude, SustainLevel, time/DecayDuration);
			}
			time -= DecayDuration;

			if (time < SustainDuration) {
				return SustainLevel;
			}
			time -= SustainDuration;

			if (time < ReleaseDuration) {
				if (ReleaseCurve != InterpolationType.Linear) {
					throw new NotImplementedException();
				}
				return Interpolate.Lerp(SustainLevel, 0f, time/SustainDuration);
			}

			return 0f;
		}
	}
}

