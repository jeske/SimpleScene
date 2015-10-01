// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectRay : SSObject
    {
        public SSRay ray; 

		public override void Render(SSRenderConfig renderConfig) {
			base.Render (renderConfig);

			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL
			GL.Disable(EnableCap.Texture2D);

			GL.LineWidth(5.0f);

			GL.Begin(PrimitiveType.Lines);
			GL.Color3(1.0f,1f,1f);   GL.Vertex3(0,0,0);
            GL.Color3(1.0f,0.5f,0.5f);   GL.Vertex3(this.ray.dir * 10.0f);
            GL.End();
        }
        public SSObjectRay (SSRay ray) : base() {
            this.ray = ray;
            this.Pos = ray.pos;
            this.renderState.alphaBlendingOn = false;
            this.renderState.lighted = false;

            // the ray is in world-space, so we adjust our object pos, and then save the ray (so we have the world-space ray.dir)
            // NOTE: technically the ray.pos is still the world-space pos...
        }
    }
}

	