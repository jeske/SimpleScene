using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSIndexedMesh<V> : SSAbstractMesh, ISSInstancable
        where V : struct, ISSVertexLayout
    {
        protected SSVertexBuffer<V> vbo;
        protected SSIndexBuffer ibo;

        /// <summary>
        /// Initialize based on buffer usage. Default to dynamic draw.
        /// </summary>
        public SSIndexedMesh (BufferUsageHint vertUsage = BufferUsageHint.DynamicDraw, 
                              BufferUsageHint indexUsage = BufferUsageHint.DynamicDraw)
        {
            vbo = new SSVertexBuffer<V> (vertUsage);
            ibo = new SSIndexBuffer (vbo, indexUsage);
        }

        /// <summary>
        /// Initialize given arrays of vertices and/or indices.
        /// </summary>
        public SSIndexedMesh(V[] vertices, UInt16[] indices)
			: base()
        {
            if (vertices == null) {
                vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
            } else {
                vbo = new SSVertexBuffer<V> (vertices);
            }

            if (indices == null) {
                ibo = new SSIndexBuffer (vbo, BufferUsageHint.DynamicDraw);
            } else {
                ibo = new SSIndexBuffer (indices, vbo);
            }
        }

		public SSIndexedMesh(SSVertexBuffer<V> vbo, SSIndexBuffer ibo)
		{
			if (vbo == null) {
				vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				vbo = vbo;
			}

			if (ibo == null) {
				ibo = new SSIndexBuffer (vbo, BufferUsageHint.DynamicDraw);
			} else {
				ibo = ibo;
			}
		}

        public override void RenderMesh(ref SSRenderConfig renderConfig)
        {
			base.RenderMesh (ref renderConfig);
			ibo.DrawElements (ref renderConfig, PrimitiveType.Triangles);
        }

		public void RenderInstanced(ref SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType = PrimitiveType.Triangles)
        {
			base.RenderMesh (ref renderConfig);
			ibo.RenderInstanced (ref renderConfig, instanceCount, primType);
        }

        public void UpdateVertices (V[] vertices)
        {
            vbo.UpdateBufferData(vertices);
        }

        public void UpdateIndices (UInt16[] indices)
        {
            ibo.UpdateBufferData(indices);
        }
    }
}

