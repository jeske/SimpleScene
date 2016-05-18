using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene {
    public class SSObjectGDISurface_Text : SSObjectGDISurface {

        public Font font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 15.0f);

        private string _label;
        public string Label {
            get { return _label; }
            set {
                this._label = value;
                Dirty = true;
            }
        }

        public SSObjectGDISurface_Text() {
            renderState.frustumCulling = false;
        }


        public override Bitmap RepaintGDI(out Size gdiSize) 
        {

            // figure out the size of the label
            gdiSize = Graphics.FromImage(new Bitmap(1, 1)).MeasureString(_label, font).ToSize();
            if (gdiSize.Width <= 0 || gdiSize.Height <= 0) {
                return null;
            }

            // adjust labelSize to power of 2
            Size textureSize = makeValidTextureSize((int)gdiSize.Width, (int)gdiSize.Height);


            // draw the string onto a bitmap
            var bitmap = new Bitmap(textureSize.Width, textureSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			
            var gc = Graphics.FromImage(bitmap);
            gc.Clear(Color.Black);

            // gc.DrawLine(Pens.White,4,4,textureSize.Width-1,4);
            // gc.DrawRectangle(Pens.White,0,0,textureSize.Width-1,textureSize.Height-1);

            // gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
            gc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;	
            gc.DrawString(_label, font, Brushes.White, 0, 0);

            //gc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
			//gc.DrawLine(Pens.Red,4,4,textureSize.Width-1,4);

            gc.Flush();

            // Console.WriteLine("SSObjectGDIText: created texture size = {0} {1}", bitmap.Width, bitmap.Height);
			// DUMP_TEX_PIXELS(bitmap);

            return bitmap;
        }

		private void DUMP_TEX_PIXELS(Bitmap bitmap) {
			for (int i = 0; i < bitmap.Width; i++) {
				for (int j = 0; j < bitmap.Height; j++) {
					Console.Write ("{0:X} ", bitmap.GetPixel (i, j).ToArgb ());
				}
				Console.WriteLine ("-");
			}
		}


    }
}