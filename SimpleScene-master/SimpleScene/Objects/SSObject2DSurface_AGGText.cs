// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Drawing;

using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.RasterizerScanline;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.Font;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using UG;

namespace SimpleScene {
    public class SSObject2DSurface_AGGText : SSObject2DSurface_AGG {

        public Font font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 15.0f);
        SizeF labelSize;
		public Color textColor = Color.White;
		public Color backgroundColor = Color.Black;

        private string _label;
        public string Label {
            get { return _label; }
            set {
                this._label = value;
                Dirty = true;
            }
        }

        public SSObject2DSurface_AGGText() {
        }

		public override UG.Bitmap RepaintAGG(out Size gdiSize) {
            // figure out the size of the label
			gdiSize = UG.Graphics.FromImage(new UG.Bitmap(1, 1)).MeasureString(_label, font).ToSize();

            // adjust labelSize to power of 2
            Size textureSize = makeValidTextureSize((int)gdiSize.Width, (int)gdiSize.Height);			

            // draw the string onto a bitmap
            var bitmap = new UG.Bitmap(textureSize.Width, textureSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			
            var gc = UG.Graphics.FromImage(bitmap);
			gc.Clear(backgroundColor);            

            gc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;			
            // gc.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
			gc.DrawString(_label, font, new SolidBrush(textColor), 0, 0);

            // gc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;						
			// gc.DrawLine(Pens.Red,4,4,textureSize.Width-1,4);
            // gc.DrawRectangle(Pens.White,0,0,textureSize.Width-1,textureSize.Height-1);

            gc.Flush();

            // Console.WriteLine("SSObjectGDIText: created texture size = {0} {1}", bitmap.Width, bitmap.Height);
			// DUMP_TEX_PIXELS(bitmap);

            return bitmap;
        }

		private void DUMP_TEX_PIXELS(System.Drawing.Bitmap bitmap) {
			for (int i = 0; i < bitmap.Width; i++) {
				for (int j = 0; j < bitmap.Height; j++) {
					Console.Write ("{0:X} ", bitmap.GetPixel (i, j).ToArgb ());
				}
				Console.WriteLine ("-");
			}
		}


    }
}