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
}

