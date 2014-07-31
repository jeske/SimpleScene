using System;
using System.Drawing;
using System.Drawing.Imaging;

using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.RasterizerScanline;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.Font;

namespace UG
{

	public struct Bitmap {
		internal ImageBuffer buffer;

		/// <summary>
		/// Creates a bitmap with 32bit A-rgb (BGRA) PixelFormat
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public Bitmap (int width, int height) {
			this.buffer = new ImageBuffer(width,height,32, new BlenderBGRA());
		}

		public System.Drawing.Size Size {
			get { return new System.Drawing.Size (buffer.Width, buffer.Height); }
		}

		public Bitmap (int width, int height, PixelFormat format)
		{
			IRecieveBlenderByte byteBlender;
			int bpp;

			switch (format) {
			case PixelFormat.Format32bppArgb: 
				bpp = 32;
				byteBlender = new BlenderBGRA();
				break;
			case PixelFormat.Format32bppRgb:
				bpp = 32;
				byteBlender = new BlenderBGR();
				break;
			case PixelFormat.Format24bppRgb:
				bpp = 24;
				byteBlender = new BlenderBGR();
				break;
			default:
				throw new NotImplementedException(String.Format("UG.Bitmap unsupported format {0}",format));
			}
			this.buffer = new ImageBuffer(width,height,bpp,byteBlender);
		}

		public static implicit operator ImageBuffer(Bitmap bitmap) {
			return bitmap.buffer;
		}
	} // struct Bitmap

	public class Graphics {
		public Graphics2D aggGc;
		public System.Drawing.Drawing2D.SmoothingMode SmoothingMode;
		public System.Drawing.Text.TextRenderingHint TextRenderingHint;

		public Graphics (Graphics2D aggGc) {
			this.aggGc = aggGc;
		}

		public static Graphics FromImage (Bitmap image) {
			return new Graphics(image.buffer.NewGraphics2D());
		}

		public static Graphics FromImage (ImageBuffer image) {
			return new Graphics(image.NewGraphics2D());
		}
	
		public void Clear (System.Drawing.Color color) {			
			aggGc.Clear(new RGBA_Bytes((uint)color.ToArgb()));
		}		

		public void DrawString (string text, Font font, Brush brush, int x, int y) {
			// TODO: handle different brushes
			// TODO: emulate GDI "bordering" of text?

			SolidBrush colorBrush = brush as SolidBrush;
			aggGc.Render(
				new VertexSourceApplyTransform(
					new TypeFacePrinter(
						text,
						font.SizeInPoints,
						new MatterHackers.VectorMath.Vector2(x,y),
						Justification.Left,Baseline.BoundsTop),
					Affine.NewScaling(1,-1))
				,new MatterHackers.Agg.RGBA_Bytes((uint)colorBrush.Color.ToArgb()));
		}

		public void Flush () { }

		public SizeF MeasureString(string text, Font font) {
			// TODO: teach agg-sharp to render windows fonts
			var tfp = new TypeFacePrinter(text, font.SizeInPoints);
			var bounds = tfp.LocalBounds;
			return new SizeF((float)bounds.Width,(float)bounds.Height);
		}
	}
}

