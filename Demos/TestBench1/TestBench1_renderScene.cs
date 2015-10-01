using System;

using SimpleScene.Demos;

namespace TestBench1
{
	partial class TestBench1 : TestBenchBootstrap
	{
		protected override void updateTextDisplay() 
		{
			base.updateTextDisplay ();

			string extraInfo = "\n\npress 'Q' to \"attack\"";
			textDisplay.Label += extraInfo;
		}
	}
}

