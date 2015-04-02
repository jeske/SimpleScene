using System.Collections.Generic;
using System;

namespace SimpleScene.Util
{
	public static class Interpolate {
        static public float Lerp(float start, float finish, float ammount)
        {
            return start + (finish - start) * ammount;
        }
    }

	public interface IInterpolater 
	{
		float compute (float start, float finish, float ammount);
	}

	public class LinearInterpolater : IInterpolater
	{
		public float compute(float start, float finish, float ammount)
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
		public float attackDuration = 1f;
		public float decayDuration = 1f;
		public float sustainDuration = 1f;
		public float releaseDuration = 1f;

		public float totalDuration {
			get { return attackDuration + decayDuration + sustainDuration + releaseDuration; }
		}

        IInterpolater attackInterpolater = new LinearInterpolater();
        IInterpolater decayInterpolater = new LinearInterpolater();
        IInterpolater releaseInterpolater = new LinearInterpolater();

		public float amplitude = 1f;
		public float sustainLevel = 0.5f;

		public float computeLevel(float time)
		{
            if (time < 0f) {
                return 0f;
            }

			if (time < attackDuration) {
				return attackInterpolater.compute (0f, amplitude, time / attackDuration);
			}
			time -= attackDuration;

			if (time < decayDuration) {
				return decayInterpolater.compute (amplitude, sustainLevel, time / decayDuration);
			}
			time -= decayDuration;

			if (time < sustainDuration) {
				return sustainLevel;
			}
			time -= sustainDuration;

			if (time < releaseDuration) {
				releaseInterpolater.compute (sustainLevel, 0f, time / sustainDuration);
			}
			return 0f;
		}
	}
}

