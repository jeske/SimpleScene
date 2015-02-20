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
	class ADSREnvelope
	{
		public float attackDuration = 1f;
		public float decayDuration = 1f;
		public float sustainDuration = 1f;
		public float releaseDuration = 1f;

		public InterpolationType attackCurve = InterpolationType.Linear;
		public InterpolationType decayCurve = InterpolationType.Linear;
		public InterpolationType releaseCurve = InterpolationType.Linear;

		public float amplitude = 1f;
		public float sustainLevel = 0.5f;

		public float ComputeLevel(float time)
		{
			if (time < attackDuration) {
				if (attackCurve != InterpolationType.Linear) {
					throw new NotImplementedException();
				}
				return Interpolate.Lerp(0f, amplitude, time/attackDuration);
			}
			time -= attackDuration;

			if (time < decayDuration) {
				if (decayCurve != InterpolationType.Linear) {
					throw new NotImplementedException();
				}
				return Interpolate.Lerp(amplitude, sustainLevel, time/decayDuration);
			}
			time -= decayDuration;

			if (time < sustainDuration) {
				return sustainLevel;
			}
			time -= sustainDuration;

			if (time < releaseDuration) {
				if (releaseCurve != InterpolationType.Linear) {
					throw new NotImplementedException();
				}
				return Interpolate.Lerp(sustainLevel, 0f, time/sustainDuration);
			}

			return 0f;
		}
	}
}

