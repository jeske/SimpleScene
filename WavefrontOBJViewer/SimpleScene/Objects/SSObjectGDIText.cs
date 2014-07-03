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

		public Font font = System.Drawing.SystemFonts.DefaultFont;
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
					
		private void _repaint() {
			// figure out the size of the label
			labelSize = Graphics.FromImage(new Bitmap(1,1)).MeasureString (_label, font);

			// draw the string onto a bitmap
			var bitmap = new Bitmap ((int)labelSize.Width,(int)labelSize.Height,System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			var gc = Graphics.FromImage (bitmap);
			//gc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
			//gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
			gc.Clear (Color.Green);
			gc.DrawLine (Pens.White, 0, 0, labelSize.Width, labelSize.Height);
			gc.DrawString (_label, font, Brushes.White, 0, labelSize.Height);
			// gc.Flush ();

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

			// fixed function single-texture
			GL.Enable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, textureSurface.TextureID);

			// mode setup
			// GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

			// Step 2: setup our material mode and paramaters...
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);

			// draw text rectangle...
			GL.Begin(BeginMode.Triangles);
			GL.Color3(System.Drawing.Color.White);  // clear the vertex color to white..

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

