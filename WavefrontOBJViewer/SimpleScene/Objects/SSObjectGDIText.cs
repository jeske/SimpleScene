using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	public class SSObjectGDIText : SSObject
	{
		public SSObjectGDIText () {}

		public Font font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif,15.0f);
		SizeF labelSize;

		private SSTexture textureSurface;

		private string _label;
		public string Label { 
			get { return _label; }
			set {
				this._label = value;
				_repaint ();
			}
		}
					
		private int nextPowerOf2(int biggerThan) {
			int powof2 = 1;
			while (powof2 < biggerThan)
				powof2 <<= 1;
			return powof2;
		}

		private Size makeValidTextureSize(int w, int h) {
			if (false) {
				// if it requires power of two texture sizes
				return new Size (
					nextPowerOf2 (Math.Max (w, 64)),
					nextPowerOf2 (Math.Max (h, 64)));
			} else {
				return new Size (w, h);
			}
		}

		private void _repaint() {
			// figure out the size of the label
			labelSize = Graphics.FromImage(new Bitmap(1,1)).MeasureString (_label, font);

			// adjust labelSize to power of 2
			Size textureSize = makeValidTextureSize((int)labelSize.Width, (int)labelSize.Height);

			// draw the string onto a bitmap
			var bitmap = new Bitmap (textureSize.Width,textureSize.Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			var gc = Graphics.FromImage (bitmap);
			// gc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
			gc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
			// gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
			gc.Clear (Color.Black);
			gc.DrawLine(Pens.White,4,4,textureSize.Width-1,4);
			// gc.DrawRectangle(Pens.White,0,0,textureSize.Width-1,textureSize.Height-1);

			gc.DrawString (_label, font, Brushes.White, 0, 0);
			gc.Flush ();


			Console.WriteLine("SSObjectGDIText: created texture size = {0} {1}",bitmap.Width,bitmap.Height);
			#if false
			for (int i = 0; i < bitmap.Width; i++) {
				for (int j = 0; j < bitmap.Height; j++) {
					Console.Write ("{0:X} ", bitmap.GetPixel (i, j).ToArgb ());
				}
				Console.WriteLine ("-");
			}
			#endif

			// allocate the texture and copy the bits...
			textureSurface = new SSTexture ();
			textureSurface.createFromBitmap (bitmap);
		}

		public override void Render (ref SSRenderConfig renderConfig)
		{
			if (textureSurface == null)
				return;

			base.Render (ref renderConfig);

			GL.UseProgram(0); // disable GLSL

			// mode setup
			// GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

			// Step 2: setup our material mode and paramaters...
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);

			// fixed function single-texture
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, textureSurface.TextureID);

			// draw text rectangle...
			GL.Begin(BeginMode.Triangles);
			GL.Color3(Color.White);  // clear the vertex color to white..

			float w = labelSize.Width;
			float h = labelSize.Height;

			// upper-left
			GL.TexCoord2(0.0,0.0); GL.Vertex3(0.0, 0.0, 0.0);
			GL.TexCoord2(1.0,0.0); GL.Vertex3(w, 0.0, 0.0);
			GL.TexCoord2(0.0,1.0); GL.Vertex3(0.0, h, 0.0);

			// lower-right
			GL.TexCoord2(0.0,1.0); GL.Vertex3(0.0, h, 0.0);
			GL.TexCoord2(1.0,0.0); GL.Vertex3(w, 0.0, 0.0);
			GL.TexCoord2(1.0,1.0); GL.Vertex3(w, h, 0.0);

			GL.End();
		}

	}
}

