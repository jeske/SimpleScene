// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectTriangle : SSObject
    {
		public override void Render(ref SSRenderConfig renderConfig) {
			base.Render (ref renderConfig);

			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);

			// triangle draw...
			GL.Begin(PrimitiveType.Triangles);

            GL.Color3(1.0f, 1.0f, 0.0f); GL.Vertex3(-1.0f, -1.0f, 0.0f);
            GL.Color3(1.0f, 0.0f, 0.0f); GL.Vertex3(1.0f, -1.0f, 0.0f);
            GL.Color3(0.2f, 0.9f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.0f);

            GL.End();
        }
        public SSObjectTriangle () : base() {
			this.boundingSphere = new SSObjectSphere(1.0f);
        }
    }
}

