using System;
using System.Drawing;

using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.RasterizerScanline;
// using MatterHackers.VectorMath;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene {
    public abstract class SSObject2DSurface_AGG : SSObject {
        private SSTexture textureSurface;

        public bool Dirty = true;
        Vector2 gdiSize;
        Vector2 textureSize;
        public bool hasAlpha = false;

        public SSObject2DSurface_AGG() { 
			textureSurface = new SSTexture();
		}

        private int nextPowerOf2(int biggerThan) {
            int powof2 = 1;
            while (powof2 < biggerThan)
                powof2 <<= 1;
            return powof2;
        }

        internal Vector2 makeValidTextureSize(int w, int h) {
            if (false) {
                // if it requires power of two texture sizes
                return new Vector2(
                    nextPowerOf2(Math.Max(w, 64)),
                    nextPowerOf2(Math.Max(h, 64)));
            } else {
                return new Vector2(w, h);
            }
        }

        public void UpdateTexture() {
            if (!Dirty) return;
            Dirty = false;

            // using this method to software GDI+ render to a bitmap, and then copy to texture
            // http://florianblock.blogspot.com/2008/06/copying-dynamically-created-bitmap-to.html          

            ImageBuffer bitmap = this.RepaintAGG(out gdiSize);
			textureSize = new Vector2(bitmap.Width,bitmap.Height);

            // download bits into a texture...
            textureSurface.loadFromImageBuffer(bitmap);
        }

        public abstract ImageBuffer RepaintAGG(out Vector2 gdiSize);

        public override void Render(ref SSRenderConfig renderConfig) {            
            UpdateTexture();            

            base.Render(ref renderConfig);

            GL.UseProgram(0); // disable GLSL

            // mode setup
            // GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            // Step 2: setup our material mode and paramaters...
            GL.Disable(EnableCap.CullFace);
            
            GL.Disable(EnableCap.Lighting);
            if (hasAlpha) {
                GL.Enable(EnableCap.AlphaTest);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            } else {
                GL.Disable(EnableCap.AlphaTest);
                GL.Disable(EnableCap.Blend);
            }

            // fixed function single-texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, textureSurface.TextureID);

            // draw text rectangle...
            GL.Begin(BeginMode.Triangles);
            GL.Color3(Color.White);  // clear the vertex color to white..

            float w = gdiSize.X;
            float h = gdiSize.Y;

            if (gdiSize != textureSize) {
                // adjust texture coordinates
                throw new Exception("not implemented");
            }

            // upper-left
            GL.TexCoord2(0.0, 0.0); GL.Vertex3(0.0, 0.0, 0.0);
            GL.TexCoord2(1.0, 0.0); GL.Vertex3(w, 0.0, 0.0);
            GL.TexCoord2(0.0, 1.0); GL.Vertex3(0.0, h, 0.0);

            // lower-right
            GL.TexCoord2(0.0, 1.0); GL.Vertex3(0.0, h, 0.0);
            GL.TexCoord2(1.0, 0.0); GL.Vertex3(w, 0.0, 0.0);
            GL.TexCoord2(1.0, 1.0); GL.Vertex3(w, h, 0.0);

            GL.End();
        }

    }
}

