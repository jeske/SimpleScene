using System;
using System.Drawing;

using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.RasterizerScanline;
using MatterHackers.Agg.Font;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene {
    public class SSObject2DSurface_AGGText : SSObject2DSurface_AGG {

        public Font font = new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 15.0f);
        SizeF labelSize;

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


        public override ImageBuffer RepaintAGG(out Vector2 gdiSize) {
            // figure out the size of the label			
			
			ImageBuffer bitmap = new ImageBuffer(200,50,32, new BlenderBGRA());
			var gc = bitmap.NewGraphics2D();			
			gc.Line(0,10,200,50, new MatterHackers.Agg.RGBA_Bytes(200,0,0));	
			gc.Circle(20,20,10,new MatterHackers.Agg.RGBA_Bytes(0,200,0));		
			gc.DrawString(_label,10,10,14,color: new MatterHackers.Agg.RGBA_Bytes(255,255,255));		
			
			gdiSize = new Vector2(bitmap.Width,bitmap.Height);

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