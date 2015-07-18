using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSVertexMesh<V> : SSAbstractMesh, ISSInstancable
		where V : struct, ISSVertexLayout
	{
		protected SSVertexBuffer<V> vbo;

		/// <summary>
		/// Initialize based on buffer usage. Default to dynamic draw.
		/// </summary>
		public SSVertexMesh (BufferUsageHint vertUsage = BufferUsageHint.DynamicDraw)
		{
			vbo = new SSVertexBuffer<V> (vertUsage);
		}

		/// <summary>
		/// Initialize given arrays of vertices and/or indices.
		/// </summary>
		public SSVertexMesh(V[] vertices)
		{
			if (vertices == null) {
				vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				vbo = new SSVertexBuffer<V> (vertices);
			}
		}

		public SSVertexMesh(SSVertexBuffer<V> vbo)
		{
			if (vbo == null) {
				vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				vbo = vbo;
			}
		}

		public override void renderMesh(SSRenderConfig renderConfig)
		{
			base.renderMesh (renderConfig);
			vbo.DrawArrays (renderConfig, PrimitiveType.Triangles);
		}

		public void renderInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType = PrimitiveType.Triangles)
		{
			base.renderMesh (renderConfig);
			vbo.renderInstanced (renderConfig, instanceCount, primType);
		}

		public void computeVertices (V[] vertices)
		{
			vbo.UpdateBufferData(vertices);
		}
	}
}

