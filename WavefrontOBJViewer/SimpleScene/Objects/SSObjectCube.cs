// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
    public class SSObjectCube : SSObject
    {
		public override void Render(ref SSRenderConfig renderConfig) {
            base.Render (ref renderConfig);
            GL.Begin(BeginMode.Triangles);

            GL.Color3(0.5f, 0.5f, 0.5f); 


			var P0 = new Vector3 (0, 0, 0);  // 0 origin
			var P1 = new Vector3 (0, 1, 1);  // 1 corner

			var P2 = new Vector3 (0, 0, 1);  
			var P3 = new Vector3 (0, 1, 0);

			var P4 = new Vector3 (1, 1, 1);
			var P5 = new Vector3 (1, 0, 0);

			var P6 = new Vector3 (1, 0, 1);
			var P7 = new Vector3 (1, 1, 0);

			
			// x-axis faces @ 0
			GL.Vertex3(P0); GL.Vertex3(P2); GL.Vertex3(P1);
			GL.Vertex3(P0); GL.Vertex3(P1); GL.Vertex3(P3);

			// x-axis faces @ 1
			GL.Vertex3(P4); GL.Vertex3(P5); GL.Vertex3(P6);
			GL.Vertex3(P4); GL.Vertex3(P7); GL.Vertex3(P5);

			// y-axis faces @ 0


			GL.End();
		}
		public SSObjectCube () : base()
		{
		}
	}
}

