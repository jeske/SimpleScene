// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;

namespace SimpleScene
{
	public class FPSCalculator
	{
		int FPS_frames;
		double FPS_time;

		double _framesPerSecond;
		public double AvgFramesPerSecond {
			get { return _framesPerSecond; }
		}

		int _stalls;
		DateTime _stallPeriodBegin;

		static float STALL_TIMEOUT_SECONDS = 0.9f;

		public FPSCalculator () { 
		}

		public void newFrame (double afterTimeElapsedSeconds)
		{
			DateTime now = DateTime.Now;

			// if there is a stall, don't count it against the frames per second calculation
			// 10 fps = 100ms per frame, anything less we call a "stall"... 
			if (afterTimeElapsedSeconds > STALL_TIMEOUT_SECONDS) {
				// looks like a stall! 				
				_stalls++;
				if (_stallPeriodBegin == default(DateTime)) {
					_stallPeriodBegin = now;
					_stalls++;
				}
				return;
			} else {
				// the stall ended..
				if (_stallPeriodBegin != default(DateTime)) {								
					_stallPeriodBegin = default(DateTime);
					Console.WriteLine("*** Render Stall *** {0} seconds",afterTimeElapsedSeconds);
				}
			}

			// frames per second
			FPS_frames++;
			FPS_time += afterTimeElapsedSeconds;
			if (FPS_time > 2.0) {
				_framesPerSecond = (double)FPS_frames / FPS_time;
				FPS_frames = 0;
				FPS_time = 0.0;
			}
		}
	}
}

