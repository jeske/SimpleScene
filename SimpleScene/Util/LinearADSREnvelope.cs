using System;

namespace SimpleScene.Util
{
	/// <summary>
	/// For modeling Attack-Delay-Sustain-Release levels of anyting
	/// https://en.wikipedia.org/wiki/Synthesizer#ADSR_envelope
	/// </summary>
	public class LinearADSREnvelope
	{
		public float attackDuration;
		public float decayDuration;
		public float sustainDuration;
		public float releaseDuration;

		public float totalDuration {
			get { return attackDuration + decayDuration + sustainDuration + releaseDuration; }
		}

        LinearInterpolater attackInterpolater = new LinearInterpolater();
        LinearInterpolater decayInterpolater = new LinearInterpolater();
        LinearInterpolater releaseInterpolater = new LinearInterpolater();

		public float peakLevel;
		public float sustainLevel;

		public LinearADSREnvelope(
			float attackDuration = 1f, float decayDuration = 1f, 
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

        public LinearADSREnvelope()
        {
            this.attackDuration = 1f;
            this.decayDuration = 1f;
            this.sustainDuration = 1f;
            this.releaseDuration = 1f;
            this.peakLevel = 1f;
            this.sustainLevel = 0.5f;
        }

		public LinearADSREnvelope Clone()
		{
			return new LinearADSREnvelope (
				this.attackDuration, this.decayDuration,
				this.sustainDuration, this.releaseDuration,
				this.peakLevel, this.sustainLevel);
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
				return releaseInterpolater.compute (sustainLevel, 0f, time / releaseDuration);
			}
			return 0f;
		}
	}
}

