// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectCube : SSObject
    {

        private void drawQuadFace(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
            
			GL.Normal3(Vector3.Cross(p1-p0,p2-p0).Normalized());
            GL.Vertex3(p0);
            GL.Vertex3(p1);
            GL.Vertex3(p2);

            GL.Normal3(Vector3.Cross(p2-p0,p3-p0).Normalized());
            GL.Vertex3(p0);
            GL.Vertex3(p2);
            GL.Vertex3(p3);
        }

		public override void Render(ref SSRenderConfig renderConfig) {
            base.Render (ref renderConfig);

			var p0 = new Vector3 (-1, -1,  1);  
			var p1 = new Vector3 ( 1, -1,  1);
			var p2 = new Vector3 ( 1,  1,  1);  
			var p3 = new Vector3 (-1,  1,  1);
			var p4 = new Vector3 (-1, -1, -1);
			var p5 = new Vector3 ( 1, -1, -1);
			var p6 = new Vector3 ( 1,  1, -1);
			var p7 = new Vector3 (-1,  1, -1);

			GL.Begin(BeginMode.Triangles);
            GL.Color3(0.5f, 0.5f, 0.5f);
            
            drawQuadFace(p0, p1, p2, p3);            
            drawQuadFace(p7, p6, p5, p4);
            drawQuadFace(p1, p0, p4, p5);
            drawQuadFace(p2, p1, p5, p6);
            drawQuadFace(p3, p2, p6, p7);
            drawQuadFace(p0, p3, p7, p4);

			GL.End();
		}
		public SSObjectCube () : base()
		{
		}
	}
}

