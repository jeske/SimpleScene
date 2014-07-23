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

		static int STALL_TIMEOUT = 60 * 5;  // 5 minute stall timeout

		public FPSCalculator () { 
		}

		public void newFrame (double afterTimeElapsedSeconds)
		{
			DateTime now = DateTime.Now;

			// if there is a stall, don't count it against the frames per second calculation
			// 10 fps = 100ms per frame, anything less we call a "stall"... 
			// TODO: make this dynamic based on the FPS...
			if (afterTimeElapsedSeconds > (100.0 /1000.0)) {
				// looks like a stall! 				
				_stalls++;
				if (_stallPeriodBegin == default(DateTime)) {
					_stallPeriodBegin = now;
				}
				Console.WriteLine("*** Render Stall *** {0} seconds",afterTimeElapsedSeconds);
				return;
			} else {
				if ((now - _stallPeriodBegin).TotalMinutes > STALL_TIMEOUT) {
					_stalls = 0;
					_stallPeriodBegin = default(DateTime);
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

