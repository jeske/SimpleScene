// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectTriangle : SSObject
    {
		public override void Render(SSRenderConfig renderConfig) {
			base.Render (renderConfig);

			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL
			GL.Disable(EnableCap.Texture2D);

			// triangle draw...
			GL.Begin(PrimitiveType.Triangles);

            GL.Color3(1.0f, 1.0f, 0.0f); GL.Vertex3(-1.0f, -1.0f, 0.0f);
            GL.Color3(1.0f, 0.0f, 0.0f); GL.Vertex3(1.0f, -1.0f, 0.0f);
            GL.Color3(0.2f, 0.9f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.0f);

            GL.End();
        }
        public SSObjectTriangle () : base() {
			this.localBoundingSphereCenter = Vector3.Zero;
			this.localBoundingSphereRadius = 1f;
            this.renderState.lighted = false;
            this.renderState.alphaBlendingOn = false;
        }
    }
}

