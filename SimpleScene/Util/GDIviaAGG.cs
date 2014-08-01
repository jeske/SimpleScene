// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

// This is a small partial windows-GDI System.Drawing.Graphics emulation via agg-sharp.
// I wrote it because Mono's GDI impelemenation doesn't handle clip-paths properly.
//
// You may (or may not) need my private agg-sharp fork...
//
//   https://github.com/jeske/agg-sharp
//
// So far I havn't put much time into making the rendering output closely match GDI.
// It currently only supports SolidBrush, and it ignores Fonts and uses the AGG internal font.
// The text-rendering is always the minimal "GenericTypographic" style with no padding.

using System;
using System.Collections.Generic;
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
		public ImageBuffer buffer;

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


    // -------------------------------------------------------------------------------------------------

	public class GraphicsPath {
		internal PathStorage path = new PathStorage();

        public void AddPie (System.Drawing.Rectangle rect, float startAngleDeg, float endAngleDeg)
		{
			// throw new NotImplementedException();
			double originX = (rect.Left + rect.Right) / 2.0;
			double originY = (rect.Top + rect.Bottom) / 2.0;
			double radiusX = (rect.Height) / 2.0;
			double radiusY = (rect.Width) / 2.0;
			
			IVertexSource arcpath;

			if (Math.Abs (endAngleDeg - startAngleDeg) >= 360.0) {
				// if it's a full circle, we don't need to connect to the origin
				path.concat_path(new arc(originX,originY,radiusX,radiusY,
					Graphics.DegreesToRadians(startAngleDeg),
					Graphics.DegreesToRadians(endAngleDeg),moveToStart:true));
			} else {
				// if it's a partial arc, we need to connect to the origin
				path.MoveTo(originX,originY);
				path.concat_path(new arc(originX,originY,radiusX,radiusY,
					Graphics.DegreesToRadians(startAngleDeg),
					Graphics.DegreesToRadians(endAngleDeg),moveToStart:false));
				path.LineTo(originX,originY);				
			}
		}

	}


    // -------------------------------------------------------------------------------------------------
	public class GraphicsState {
		Graphics context;
		IVertexSource pipeTail;
		RectangleDouble clipRect;
		Affine transform;
		ImageBuffer clipBuffer;

		internal GraphicsState (Graphics context) {
			this.context = context;
			this.clipRect = context.aggGc.GetClippingRect();
			this.transform = context.aggGc.GetTransform();
			this.clipBuffer = context._clipBuffer;
		}
		internal void Restore() {
			context.aggGc.SetClippingRect(this.clipRect);
			context.aggGc.PushTransform();
			context.aggGc.SetTransform(this.transform);
			context._clipBuffer = clipBuffer;
		}
	}

	public class DeferredVertexSource : IVertexSource {
		public IVertexSource source;
		private void checkSource() {
			if (source == null) {
				throw new Exception("DeferredVertexSource: no source attached");
			}
		}

		public IEnumerable<VertexData> Vertices ()
		{
			checkSource();
			return source.Vertices();
		}
		public void rewind (int pathId)
		{
			checkSource();
			source.rewind(pathId);
		}
		public ShapePath.FlagsAndCommand vertex (out double x, out double y)
		{
			checkSource();
			return source.vertex(out x, out y);
		}

		public DeferredVertexSource () { }
	}

	// -------------------------------[  GDI Graphics work-alike  ] ------------------------------------



	public class Graphics {
		public ImageBuffer imb;
		public Graphics2D aggGc;
		public System.Drawing.Drawing2D.SmoothingMode SmoothingMode;
		public System.Drawing.Text.TextRenderingHint TextRenderingHint;		
		internal ImageBuffer _clipBuffer;

		Stack<GraphicsState> restoreStack = new Stack<GraphicsState>();

		static internal double DegreesToRadians (float angleDeg)
		{
			return (angleDeg / 180.0 * Math.PI);
		}

		public Graphics (ImageBuffer imb, Graphics2D aggGc) {
			this.aggGc = aggGc;
			this.imb = imb;

			// this makes whole-numbers fall in the middle of pixels...
			// TODO: fix the AGG coordinates so this isn't necessary?
			this.aggGc.PushTransform();
			this.aggGc.SetTransform(Affine.NewTranslation(0.5,0.5));			
		}

		public static Graphics FromImage (Bitmap image) {			
			return new Graphics(image.buffer,image.buffer.NewGraphics2D());
		}

		public static Graphics FromImage (ImageBuffer image) {
			return new Graphics(image,image.NewGraphics2D());
		}

		public void Flush () { }

		public void Clear (System.Drawing.Color color) {			
			aggGc.Clear(new RGBA_Bytes((uint)color.ToArgb()));
		}		

		public void DrawString (string text, Font font, Brush brush, PointF curPoint, StringFormat fmt) {
			this.DrawString(text,font,brush,curPoint.X,curPoint.Y);
		}

		public void DrawString (string text, Font font, Brush brush, float x, float y)
		{
			// TODO: handle different brushes
			// TODO: emulate GDI "bordering" of text?
			SolidBrush colorBrush = brush as SolidBrush;
			var s1 = new TypeFacePrinter (text, font.SizeInPoints, new MatterHackers.VectorMath.Vector2 (0, 0), Justification.Left, Baseline.BoundsTop);	
			var s2 = new VertexSourceApplyTransform (s1, Affine.NewScaling (1, -1));
			if (x != 0.0f || y != 0.0f) {
				s2 = new VertexSourceApplyTransform (s2, Affine.NewTranslation (x, y));
			}
			
			_InternalRender(s2, new RGBA_Bytes((uint)colorBrush.Color.ToArgb()) );
		}		

		// TODO: adjust measure string to handle "StringFormat.GenericTypographic" differently than normal padding
		public SizeF MeasureString(string text, Font font) {
			// TODO: teach agg-sharp to render windows fonts
			var tfp = new TypeFacePrinter(text, font.SizeInPoints);
			var bounds = tfp.LocalBounds;
			return new SizeF((float)bounds.Width,(float)bounds.Height);
		}

		public SizeF MeasureString (string text, Font font, PointF origin, StringFormat format) {
			var tfp = new TypeFacePrinter(text, font.SizeInPoints);
			var bounds = tfp.LocalBounds;
			return new SizeF((float)bounds.Width,(float)bounds.Height);
		}

		public void FillRectangle (Brush brush, float x, float y, float width, float height) {
			SolidBrush solidBrush = brush as SolidBrush;

			var path = new PathStorage();
			path.MoveTo(x,y);
			path.LineTo(x,y+height);
			path.LineTo(x+width,y+height);
			path.LineTo(x+width,y);
			path.LineTo(x,y);			
			
			_InternalRender(path, new RGBA_Bytes((uint)solidBrush.Color.ToArgb()) );
		}


		public void DrawRectangle (Pen pen, float x, float y, float width, float height) {
			var path = new PathStorage();
			path.MoveTo(x,y);
			path.LineTo(x,y+height);
			path.LineTo(x+width,y+height);
			path.LineTo(x+width,y);
			path.LineTo(x,y);			
			var stroke = new Stroke(path, (double)pen.Width);

			_InternalRender(stroke, new RGBA_Bytes((uint)pen.Color.ToArgb()));
		}
	
		public void DrawArc (Pen pen, System.Drawing.Rectangle rect, float startAngleDeg, float endAngleDeg) {
			// throw new NotImplementedException();
			double originX = (rect.Left + rect.Right) / 2.0;
			double originY = (rect.Top + rect.Bottom) / 2.0;
			double radiusX = (rect.Height) / 2.0;
			double radiusY = (rect.Width) / 2.0;
			
			var arcshape = new arc(
				originX,originY,
				radiusX,radiusY,
				DegreesToRadians(startAngleDeg),DegreesToRadians(endAngleDeg)
				);
			var stroke = new Stroke(arcshape,pen.Width);			
			
			_InternalRender(stroke, new RGBA_Bytes((uint)pen.Color.ToArgb()) );
		}

		public void DrawLine (Pen pen, int x1, int y1, int x2, int y2) {
			var path = new PathStorage();
			path.MoveTo(x1,y1);
			path.LineTo(x2,y2);
			var stroke = new Stroke(path, (double)pen.Width);
			
			_InternalRender(stroke, new RGBA_Bytes((uint)pen.Color.ToArgb()) );
		}
		
		public void SetClip (UG.GraphicsPath path, System.Drawing.Drawing2D.CombineMode mode)
		{
			// to mask we make a mask bitmap and draw the shapes to it as an alpha mask.
			RGBA_Bytes shapeMaskColor;
			RGBA_Bytes backgroundMaskColor;
			switch (mode) {
			case System.Drawing.Drawing2D.CombineMode.Exclude:
				shapeMaskColor = new RGBA_Bytes (0,0,0,255);
				backgroundMaskColor = new RGBA_Bytes (255,255,255,255);
				break;
			default:
				throw new NotImplementedException ();
			}			

			// setup the clip buffer
			var bounds = aggGc.DestImage.GetBounds ();

			_clipBuffer = new ImageBuffer (bounds.Width, bounds.Height, 8, new blender_gray (1));				
			var clipGC = _clipBuffer.NewGraphics2D();
			clipGC.Clear(backgroundMaskColor);
			clipGC.SetTransform(aggGc.GetTransform()); // apply our transform to the clipper
			clipGC.Render(path.path,shapeMaskColor);

			// ImageClippingProxy clippingProxy = new ImageClippingProxy (_clipBuffer);
			//clippingProxy.clear (backgroundMaskColor);
						
			// render our shapes to the clipbuf
			// ScanlineCachePacked8 sl = new ScanlineCachePacked8 ();
			// ScanlineRenderer scanlineRenderer = new ScanlineRenderer ();
			// ScanlineRasterizer rasterizer = new ScanlineRasterizer ();

			// rasterizer.add_path (path.path);
			// scanlineRenderer.render_scanlines_aa_solid (clippingProxy, rasterizer, sl, shapeMaskColor);

			// DEBUG_saveImageBuffer(_clipBuffer);			
		}


		private void DEBUG_saveImageBuffer (ImageBuffer buf)
		{
			int hash = 0;
			var bounds = buf.GetBounds ();
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap (bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
			for (int x = 0; x < bounds.Width; x++) {
				for (int y = 0; y < bounds.Height; y++) {
					var pcolor = buf.GetPixel (x, y);
					var wcolor = Color.FromArgb (pcolor.alpha, pcolor.red, pcolor.green, pcolor.green);

					hash += wcolor.ToArgb () ^ 0x1f2f019f;
					hash <<= 1;
					
					bitmap.SetPixel (x, y, wcolor);
				}	
			}
			string filename = String.Format (@"C:\tmp\masktest-{0}.bmp", hash);
			if (!System.IO.File.Exists (filename)) {
				bitmap.Save (filename, ImageFormat.Bmp);
			}
		}

		public void SetClip(System.Drawing.Rectangle rect) {
			// this is just a simple rectangular clip

			// make sure it doesn't go outside the bounds of the image itself..
			var bounds = aggGc.DestImage.GetBounds();

			aggGc.SetClippingRect(
				new RectangleDouble(
					Math.Min(rect.Left,bounds.Width),
					Math.Min(rect.Bottom,bounds.Height),
					Math.Min(rect.Right,bounds.Width),
					Math.Min(rect.Top,bounds.Height)));
		}

		public void RotateTransform (float angleDeg)
		{
			aggGc.PushTransform();
			aggGc.SetTransform( Affine.NewRotation(DegreesToRadians(angleDeg)) * aggGc.GetTransform() );			
		}
                            

		public void TranslateTransform (double x, double y) {
			aggGc.PushTransform();
			aggGc.SetTransform( Affine.NewTranslation(x,y) * aggGc.GetTransform());			
		}

        public void FillPie (Brush brush, System.Drawing.Rectangle rect, float startAngleDeg, float endAngleDeg)
		{
			SolidBrush solidBrush = brush as SolidBrush;
			// throw new NotImplementedException();
			double originX = (rect.Left + rect.Right) / 2.0;
			double originY = (rect.Top + rect.Bottom) / 2.0;
			double radiusX = (rect.Height) / 2.0;
			double radiusY = (rect.Width) / 2.0;
			
			IVertexSource arcpath;

			if (Math.Abs (endAngleDeg - startAngleDeg) >= 360.0) {
				// if it's a full circle, we don't need to connect to the origin
				arcpath = new arc(originX,originY,radiusX,radiusY,DegreesToRadians(startAngleDeg),DegreesToRadians(endAngleDeg),moveToStart:true);
			} else {
				// if it's a partial arc, we need to connect to the origin
				var path = new PathStorage();
				path.MoveTo(originX,originY);
				path.concat_path(new arc(originX,originY,radiusX,radiusY,DegreesToRadians(startAngleDeg),DegreesToRadians(endAngleDeg),moveToStart:false));
				path.LineTo(originX,originY);
				arcpath = path;
			}
						
			_InternalRender(arcpath, new RGBA_Bytes((uint)solidBrush.Color.ToArgb()));
		}


		public GraphicsState Save () {
			var currentState = new GraphicsState(this);
			restoreStack.Push(currentState);
			return currentState;
		}
	
		public void Restore (GraphicsState state)
		{
			var restoreState = restoreStack.Pop();
			if (state != restoreState) {
				throw new Exception("UG.Graphics Restore state match failure");
			}
			
			restoreState.Restore();
		}


		public void ResetClip () {
			_clipBuffer = null;			
			aggGc.SetClippingRect(new RectangleDouble(aggGc.DestImage.GetBounds()));
		}

		private void _InternalRender (IVertexSource vertexSource, RGBA_Bytes color)
		{

			if (_clipBuffer != null) {
				// DEBUG_saveImageBuffer(_clipBuffer);
				// DEBUG_saveImageBuffer(this.imb);

				IAlphaMask alphaMask = new AlphaMaskByteClipped (_clipBuffer, 1, 0);
				AlphaMaskAdaptor imageAlphaMaskAdaptor = new AlphaMaskAdaptor (aggGc.DestImage, alphaMask);
				ImageClippingProxy alphaMaskClippingProxy = new ImageClippingProxy (imageAlphaMaskAdaptor);
				
				var scanlineRenderer = new ScanlineRenderer ();
				var rasterizer = new ScanlineRasterizer ();
				var scanlineCache = new ScanlineCachePacked8();

				
				VertexSourceApplyTransform trans = new VertexSourceApplyTransform(vertexSource, aggGc.GetTransform());
				rasterizer.add_path(trans);

				scanlineRenderer.render_scanlines_aa_solid(alphaMaskClippingProxy,rasterizer,scanlineCache,color);
				aggGc.DestImage.MarkImageChanged();				
			} else {
				aggGc.Render (vertexSource, color);
			}
		}

	} // class Graphics
}

