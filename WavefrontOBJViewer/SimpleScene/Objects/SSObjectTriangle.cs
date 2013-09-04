// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
    public class SSObjectTriangle : SSObject
    {
        public override void Render() {
            base.Render ();

			// mode setup
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);

			// triangle draw...
            GL.Begin(BeginMode.Triangles);

            GL.Color3(1.0f, 1.0f, 0.0f); GL.Vertex3(-1.0f, -1.0f, 4.0f);
            GL.Color3(1.0f, 0.0f, 0.0f); GL.Vertex3(1.0f, -1.0f, 4.0f);
            GL.Color3(0.2f, 0.9f, 1.0f); GL.Vertex3(0.0f, 1.0f, 4.0f);

            GL.End();
        }
        public SSObjectTriangle () : base() {
        }
    }
}

