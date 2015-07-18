// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectHUDQuad : SSObject
    {
		int GLu_textureID;

		public override void Render(SSRenderConfig renderConfig) {
			base.Render (renderConfig);

			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL

			GL.ActiveTexture(TextureUnit.Texture0);
			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, 0);  // reset first
            GL.BindTexture(TextureTarget.Texture2D, GLu_textureID);  // now bind the shadowmap texture id

			// GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
			// GL.TexParameter (TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);

			// draw quad...
			GL.Begin(PrimitiveType.Triangles);

			float w=500;
			float h=500;

			// upper-left
            GL.TexCoord2(0.0, 0.0); GL.Vertex3(0.0, 0.0, 0.0);
            GL.TexCoord2(0.0, 1.0); GL.Vertex3(0.0, h, 0.0);
            GL.TexCoord2(1.0, 0.0); GL.Vertex3(w, 0.0, 0.0);

            // lower-right
            GL.TexCoord2(0.0, 1.0); GL.Vertex3(0.0, h, 0.0);
            GL.TexCoord2(1.0, 1.0); GL.Vertex3(w, h, 0.0);
            GL.TexCoord2(1.0, 0.0); GL.Vertex3(w, 0.0, 0.0);
                        
            GL.End();
        }
        public SSObjectHUDQuad (int GLu_textureID) : base() {
			this.GLu_textureID = GLu_textureID;
            this.Pos = new Vector3(0,0,0);
            this.renderState.alphaBlendingOn = false;
            this.renderState.lighted = false;
            this.renderState.depthTest = false;
            this.renderState.depthWrite = false;
        }
    }
}

