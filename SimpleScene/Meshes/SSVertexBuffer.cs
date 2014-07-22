using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public interface ISSVertexLayout {
		int sizeOf();
		void bindGLAttributes(SSShaderProgram shader);
	}

	// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

	public class  SSVertexBuffer<VB> where VB : struct, ISSVertexLayout {
		VB[] vb;
		int VBOid;
		public unsafe SSVertexBuffer (VB[] vertexBufferArray) {
			vb = vertexBufferArray;

			VBOid = GL.GenBuffer ();

			try {
				GL.BindBuffer (BufferTarget.ArrayBuffer, VBOid);
				GL.BufferData (BufferTarget.ArrayBuffer, (IntPtr) (vertexBufferArray.Length * vertexBufferArray[0].sizeOf()), vertexBufferArray, BufferUsageHint.StaticDraw);
			} finally {
				GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
			}
		}

		public void Delete() {
			GL.DeleteBuffer (VBOid);
			VBOid = 0;
		}

		public void bind(SSShaderProgram shaderPgm) {
			if (shaderPgm != null) {
				GL.UseProgram (shaderPgm.ProgramID);
			} else {
				GL.UseProgram (0);
			}
			GL.BindBuffer (BufferTarget.ArrayBuffer, VBOid);
			vb [0].bindGLAttributes (shaderPgm);
		}
		public void unbind() {
			GL.BindBuffer (BufferTarget.ArrayBuffer, 0);
			//GL.DisableClientState (EnableCap.VertexArray);
			//GL.DisableClientState (EnableCap.NormalArray);
			//GL.DisableClientState (EnableCap.TextureCoordArray);
		}

	}
}

