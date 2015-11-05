using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSVertexMesh<V> : SSAbstractMesh, ISSInstancable
		where V : struct, ISSVertexLayout
	{
        public PrimitiveType defaultPrimType = PrimitiveType.Triangles;
		protected SSVertexBuffer<V> _vbo;

		/// <summary>
		/// Initialize based on buffer usage. Default to dynamic draw.
		/// </summary>
		public SSVertexMesh (BufferUsageHint vertUsage = BufferUsageHint.DynamicDraw)
		{
			_vbo = new SSVertexBuffer<V> (vertUsage);
		}

		/// <summary>
		/// Initialize given arrays of vertices and/or indices.
		/// </summary>
        public SSVertexMesh(V[] vertices, PrimitiveType primType = PrimitiveType.Triangles)
		{
			if (vertices == null) {
				_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				_vbo = new SSVertexBuffer<V> (vertices);
			}
            defaultPrimType = primType;
		}

		public SSVertexMesh(SSVertexBuffer<V> vbo)
		{
			if (vbo == null) {
				_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				_vbo = vbo;
			}
		}

		public override void renderMesh(SSRenderConfig renderConfig)
		{
			base.renderMesh (renderConfig);
            _vbo.DrawArrays (renderConfig, defaultPrimType);
		}

		public void drawInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType)
		{
			base.renderMesh (renderConfig);
			_vbo.drawInstanced (renderConfig, instanceCount, primType);
		}

        public void drawSingle(SSRenderConfig renderConfig, PrimitiveType primType)
        {
            base.renderMesh (renderConfig);
            _vbo.DrawArrays (renderConfig, primType);
        }

		public void computeVertices (V[] vertices)
		{
			_vbo.UpdateBufferData(vertices);
		}
	}
}

