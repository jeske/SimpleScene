using System.Collections.Generic;

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
		public float delayDuration = 1f;
		public float sustainDuration = 1f;
		public float decayDuration = 1f;

		public InterpolationType AttackCurve = InterpolationType.Linear;
		public InterpolationType DecayCurve = InterpolationType.Linear;
		public InterpolationType ReleaseCurve = InterpolationType.Linear;

		public float amplitude = 1f;
		public float sustainLevel = 0.5f;


	}
}

