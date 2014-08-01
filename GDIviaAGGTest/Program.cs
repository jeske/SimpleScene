using System;

using System.Drawing;
using System.Drawing.Imaging;

using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.RasterizerScanline;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.Font;

namespace GDIviaAGGTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// FillPie GDI Reference
			{
				var bitmap = new System.Drawing.Bitmap (200, 200, PixelFormat.Format32bppArgb);
				var gc = System.Drawing.Graphics.FromImage (bitmap);	
				gc.FillPie (new SolidBrush (Color.White), new Rectangle (0, 0, 200, 200), 0, 30);
				gc.Flush ();
				bitmap.Save (@"C:\tmp\pie1_gdi.bmp", ImageFormat.Bmp);
			}
	
			// FillPie GDIviaAGG test
			{
				var bitmap = new UG.Bitmap(200,200,PixelFormat.Format32bppArgb);
				var gc = UG.Graphics.FromImage(bitmap);
				gc.FillPie(new SolidBrush(Color.White),new Rectangle(0,0,200,200),0,30);
				gc.Flush();
				bitmap.Save(@"C:\tmp\pie1_agg.bmp");
			}
			
		}

	}

	public static class Extensions {
		public static void Save (this UG.Bitmap bitmap,string filename)
		{
			int hash = 0;
			var buf = bitmap.buffer;
			var bounds = buf.GetBounds ();
			System.Drawing.Bitmap wbmp = new System.Drawing.Bitmap (bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
			for (int x = 0; x < bounds.Width; x++) {
				for (int y = 0; y < bounds.Height; y++) {
					var pcolor = buf.GetPixel (x, y);
					var wcolor = Color.FromArgb (pcolor.alpha, pcolor.red, pcolor.green, pcolor.green);

					hash += wcolor.ToArgb () ^ 0x1f2f019f;
					hash <<= 1;
					
					wbmp.SetPixel (x, y, wcolor);
				}	
			}

			wbmp.Save (filename, ImageFormat.Bmp);
		}
	}
}
