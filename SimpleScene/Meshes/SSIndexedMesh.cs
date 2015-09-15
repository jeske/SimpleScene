using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSIndexedMesh<V> : SSAbstractMesh, ISSInstancable
        where V : struct, ISSVertexLayout
    {
        protected SSVertexBuffer<V> _vbo;
        protected SSIndexBuffer _ibo;

        /// <summary>
        /// Initialize based on buffer usage. Default to dynamic draw.
        /// </summary>
        public SSIndexedMesh (BufferUsageHint vertUsage = BufferUsageHint.DynamicDraw, 
                              BufferUsageHint indexUsage = BufferUsageHint.DynamicDraw)
        {
            _vbo = new SSVertexBuffer<V> (vertUsage);
            _ibo = new SSIndexBuffer (_vbo, indexUsage);
        }

        /// <summary>
        /// Initialize given arrays of vertices and/or indices.
        /// </summary>
        public SSIndexedMesh(V[] vertices, UInt16[] indices)
			: base()
        {
            if (vertices == null) {
                _vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
            } else {
                _vbo = new SSVertexBuffer<V> (vertices);
            }

            if (indices == null) {
                _ibo = new SSIndexBuffer (_vbo, BufferUsageHint.DynamicDraw);
            } else {
                _ibo = new SSIndexBuffer (indices, _vbo);
            }
        }

		public SSIndexedMesh(SSVertexBuffer<V> vbo, SSIndexBuffer ibo)
		{
			if (vbo == null) {
				_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				_vbo = vbo;
			}

			if (ibo == null) {
				_ibo = new SSIndexBuffer (vbo, BufferUsageHint.DynamicDraw);
			} else {
				_ibo = ibo;
			}
		}

        public override void renderMesh(SSRenderConfig renderConfig)
        {
			base.renderMesh (renderConfig);
			_ibo.DrawElements (renderConfig, PrimitiveType.Triangles);
        }

		public void drawInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType)
        {
			base.renderMesh (renderConfig);
			_ibo.drawInstanced (renderConfig, instanceCount, primType);
        }

        public void drawSingle(SSRenderConfig renderConfig, PrimitiveType primType)
        {
            base.renderMesh (renderConfig);
            _ibo.DrawElements (renderConfig, primType);
        }

        public void UpdateVertices (V[] vertices)
        {
            _vbo.UpdateBufferData(vertices);
        }

        public void UpdateIndices (UInt16[] indices)
        {
            _ibo.UpdateBufferData(indices);
        }
    }
}

