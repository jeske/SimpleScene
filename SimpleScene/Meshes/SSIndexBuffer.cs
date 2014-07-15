using System;

using OpenTK.Graphics.OpenGL;
namespace WavefrontOBJViewer
{
	public class SSIndexBuffer<IB> where IB : struct
	{
		int IBOid;
		public unsafe SSIndexBuffer (IB[] indicies, int sizeOf)
		{
			IBOid = GL.GenBuffer ();
			try {
				GL.BindBuffer (BufferTarget.ElementArrayBuffer, IBOid);
				GL.BufferData (BufferTarget.ElementArrayBuffer, (IntPtr) (indicies.Length * sizeOf), indicies, BufferUsageHint.StaticDraw);
			} finally {
				GL.BindBuffer (BufferTarget.ElementArrayBuffer, 0);
			}
		}
		public void Delete() {
			GL.DeleteBuffer (IBOid);
			IBOid = 0;
		}

		public void bind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, IBOid);
		}
		public void unbind() {
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, 0);
		}
	}
}

