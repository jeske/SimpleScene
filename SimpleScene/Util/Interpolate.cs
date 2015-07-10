using System.Collections.Generic;
using System;

namespace SimpleScene.Util
{
	public static class Interpolate {
        static public float Lerp(float start, float finish, float ammount)
        {
            return start + (finish - start) * ammount;
        }

        static public float LerpClamped(float start, float finish, float ammount)
        {
            if (ammount <= 0f) {
                return start;
            } else if (ammount >= 1f) {
                return finish;
            } else {
                return start + (finish - start) * ammount;
            }
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
		public float attackDuration;
		public float decayDuration;
		public float sustainDuration;
		public float releaseDuration;

		public float totalDuration {
			get { return attackDuration + decayDuration + sustainDuration + releaseDuration; }
		}

        IInterpolater attackInterpolater = new LinearInterpolater();
        IInterpolater decayInterpolater = new LinearInterpolater();
        IInterpolater releaseInterpolater = new LinearInterpolater();

		public float peakLevel;
		public float sustainLevel;

		public ADSREnvelope(float attackDuration = 1f, float decayDuration = 1f, 
							float sustainDuration = 1f, float releaseDuration = 1f,
							float peakLevel = 1f, float sustainLevel = 0.5f)
		{
			this.attackDuration = attackDuration;
			this.decayDuration = decayDuration;
			this.sustainDuration = sustainDuration;
			this.releaseDuration = releaseDuration;
			this.peakLevel = peakLevel;
			this.sustainLevel = sustainLevel;
		}

		public float computeLevel(float time)
		{
            if (time < 0f) {
                return 0f;
            }

			if (time < attackDuration) {
				return attackInterpolater.compute (0f, peakLevel, time / attackDuration);
			}
			time -= attackDuration;

			if (time < decayDuration) {
				return decayInterpolater.compute (peakLevel, sustainLevel, time / decayDuration);
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

