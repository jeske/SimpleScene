// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

namespace WavefrontOBJViewer 
{
	public abstract class SSMesh {
		public abstract void RenderMesh (ref SSRenderConfig renderConfig);
	}
}

