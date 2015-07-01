using System;

using SimpleScene.Demos;

namespace TestBench1
{
	partial class TestBench1 : TestBenchBootstrap
	{
		protected override void updateWireframeDisplayText() 
		{
			base.updateWireframeDisplayText ();

			string extraInfo = "\n\npress 'Q' to \"attack\"";
			wireframeDisplay.Label += extraInfo;
		}
	}
}

