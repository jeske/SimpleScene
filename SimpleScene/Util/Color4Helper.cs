using System;
using OpenTK.Graphics;

namespace SimpleScene
{
	public static class Color4Helper
	{
		public static readonly Color4 Zero = new Color4 (0f, 0f, 0f, 0f);

		public static Color4[] DebugPresets = {
			Color4.Red,
			Color4.Green,
			Color4.Blue,
			Color4.Magenta,
			Color4.Lime,
			Color4.Yellow,
			Color4.White,
			Color4.Cyan
		};

		public static Color4 Add (ref Color4 left, ref Color4 right) 
		{
			return new Color4 (
				left.R + right.R,
				left.G + right.G,
				left.B + right.B,
				left.A + right.A
			);
		}

		/// <summary>
		/// Convenience function for picking a debug color
		/// </summary>
		public static Color4 RandomDebugColor()
		{
			int idx = OpenTKHelper.s_debugRandom.Next (0, DebugPresets.Length);
			return DebugPresets [idx];
		}

		/// <summary>
		/// Used when packing into 4-byte formats that gets unpacked and normalized into vec4 for the shader
		/// </summary>
		public static UInt32 ToUInt32(Color4 color)
		{
			const float maxByteF = (float)byte.MaxValue;
			color.R = color.R * maxByteF;
			color.G = color.G * maxByteF;
			color.B = color.B * maxByteF;
			color.A = color.A * maxByteF;
			return (UInt32)color.A << 24 
				| (UInt32)color.B << 16 
				| (UInt32)color.G << 8 
				| (UInt32)color.R;
		}

		public static Color4 FromUInt32(UInt32 rgba)
		{
			return new Color4 (
				(byte)((rgba & 0xFF)),              // R
				(byte)((rgba & 0xFF00) >> 8),       // G
				(byte)((rgba & 0xFF0000) >> 16),    // B
				(byte)((rgba & 0xFF000000) >> 24)   // A
			);
		}
	}
}

