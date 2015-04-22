using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSIndexedMesh<V> : SSAbstractMesh, ISSInstancable
        where V : struct, ISSVertexLayout
    {
		public SSTexture diffuseTexture = null;
		public SSTexture specularTexture = null;
		public SSTexture ambientTexture = null;
		public SSTexture bumpMapTexture = null;

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
        public SSIndexedMesh(V[] vertices, UInt16[] indices)
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

		public SSIndexedMesh(SSVertexBuffer<V> vbo, SSIndexBuffer ibo)
		{
			if (vbo == null) {
				m_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				m_vbo = vbo;
			}

			if (ibo == null) {
				m_ibo = new SSIndexBuffer (m_vbo, BufferUsageHint.DynamicDraw);
			} else {
				m_ibo = ibo;
			}
		}

        public override void RenderMesh(ref SSRenderConfig renderConfig)
        {
			if (diffuseTexture != null || specularTexture != null
			 || ambientTexture != null || bumpMapTexture != null) {
				renderConfig.InstanceShader.SetupTextures (
					diffuseTexture,
					specularTexture,
					ambientTexture,
					bumpMapTexture
				);
			}
			m_ibo.DrawElements (ref renderConfig, PrimitiveType.Triangles);
        }

		public void RenderInstanced(ref SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType = PrimitiveType.Triangles)
        {
			if (diffuseTexture != null || specularTexture != null
			 || ambientTexture != null || bumpMapTexture != null) {
				renderConfig.InstanceShader.SetupTextures (
					diffuseTexture,
					specularTexture,
					ambientTexture,
					bumpMapTexture
				);
			}

			m_ibo.RenderInstanced (ref renderConfig, instanceCount, primType);
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

