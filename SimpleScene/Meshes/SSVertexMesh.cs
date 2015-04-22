using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSVertexMesh<V> : SSAbstractMesh, ISSInstancable
		where V : struct, ISSVertexLayout
	{
		public SSTexture diffuseTexture = null;
		public SSTexture specularTexture = null;
		public SSTexture ambientTexture = null;
		public SSTexture bumpMapTexture = null;

		protected SSVertexBuffer<V> m_vbo;

		/// <summary>
		/// Initialize based on buffer usage. Default to dynamic draw.
		/// </summary>
		public SSVertexMesh (BufferUsageHint vertUsage = BufferUsageHint.DynamicDraw)
		{
			m_vbo = new SSVertexBuffer<V> (vertUsage);
		}

		/// <summary>
		/// Initialize given arrays of vertices and/or indices.
		/// </summary>
		public SSVertexMesh(V[] vertices)
		{
			if (vertices == null) {
				m_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				m_vbo = new SSVertexBuffer<V> (vertices);
			}
		}

		public SSVertexMesh(SSVertexBuffer<V> vbo)
		{
			if (vbo == null) {
				m_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				m_vbo = vbo;
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
			m_vbo.DrawArrays (ref renderConfig, PrimitiveType.Triangles);
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

			m_vbo.RenderInstanced (ref renderConfig, instanceCount, primType);
		}

		public void computeVertices (V[] vertices)
		{
			m_vbo.UpdateBufferData(vertices);
		}
	}
}

