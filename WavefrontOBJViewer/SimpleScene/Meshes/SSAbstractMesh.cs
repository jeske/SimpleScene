// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using OpenTK;

namespace WavefrontOBJViewer 
{
	public abstract class SSAbstractMesh {
		public abstract void RenderMesh (ref SSRenderConfig renderConfig);
		public abstract IEnumerable<Vector3> EnumeratePoints();
	}
}

