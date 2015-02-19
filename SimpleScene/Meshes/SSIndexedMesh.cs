using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSIndexedMesh<V> : SSAbstractMesh, ISSInstancable
        where V : struct, ISSVertexLayout
    {
        protected SSVertexBuffer<V> m_vbo;
        protected SSIndexBuffer m_ibo;

        /// <summary>
        /// Initialize based on buffer usage. Default to dynamic draw.
        /// </summary>
        public SSIndexedMesh (BufferUsageHint vertUsage = BufferUsageHint.DynamicDraw, 
                              BufferUsageHint indexUsage = BufferUsageHint.DynamicDraw)
        {
            m_vbo = new SSVertexBuffer<V> (vertUsage);
            m_ibo = new SSIndexBuffer (m_vbo, indexUsage);
        }

        /// <summary>
        /// Initialize given arrays of vertices and/or indices.
        /// </summary>
        public SSIndexedMesh(V[] vertices,
                              UInt16[] indices)
        {
            if (vertices == null) {
                m_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
            } else {
                m_vbo = new SSVertexBuffer<V> (vertices);
            }

            if (indices == null) {
                m_ibo = new SSIndexBuffer (m_vbo, BufferUsageHint.DynamicDraw);
            } else {
                m_ibo = new SSIndexBuffer (indices, m_vbo);
            }
        }

        public override void RenderMesh(ref SSRenderConfig renderConfig)
        {
            m_ibo.DrawElements(PrimitiveType.Triangles);
        }

        public void RenderInstanced(int instanceCount, PrimitiveType primType = PrimitiveType.Triangles)
        {
            m_ibo.RenderInstanced(instanceCount, primType);
        }

        public void UpdateVertices (V[] vertices)
        {
            m_vbo.UpdateBufferData(vertices);
        }

        public void UpdateIndices (UInt16[] indices)
        {
            m_ibo.UpdateBufferData(indices);
        }
    }
}

