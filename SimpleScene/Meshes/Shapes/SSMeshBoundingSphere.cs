using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Util.IcoSphere;

namespace SimpleScene
{
    public class SSMeshBoundingSphere : SSAbstractMesh
	{
		// TODO make derived from SSIndexedMesh

		public float radius;

		protected static Vector3[] icoSphereVertices;
		protected static TriangleIndices[] icoSphereFaces;

		static SSMeshBoundingSphere()
		{
			var icoSphereCreator = new IcoSphereCreator();
			var icoSphereGeometry = icoSphereCreator.Create(3);
			icoSphereVertices = icoSphereGeometry.Positions.ToArray();
			icoSphereFaces = icoSphereGeometry.Faces.ToArray();
		}

		public SSMeshBoundingSphere (float radius = 0f) : base() 
		{
			this.radius = radius;
		}

		public override void renderMesh(SSRenderConfig renderConfig) 
		{

			base.renderMesh(renderConfig);

			if (renderConfig.renderBoundingSpheresSolid) {
				this._RenderSolid(renderConfig);
			}
			if (renderConfig.renderBoundingSpheresLines) {
				this._RenderLines_ICO(renderConfig);
			}
			// this._RenderLines_UV(ref renderConfig);
		}

		private void _RenderSolid(SSRenderConfig renderConfig) {
			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Enable(EnableCap.Lighting);

			// should generate these from screen-space size approximation
			float rings = 10.0f;
			float segments = 10.0f;

			// Establish constants used in sphere generation
			float fDeltaRingAngle = ((float)Math.PI / rings);
			float fDeltaSegAngle = (2.0f * (float)Math.PI / segments);

			float r0;
			float y0;
			float x0;
			float z0;

			GL.Begin(PrimitiveType.Triangles);

			GL.LineWidth(1.0f);

			for (int ring = 0; ring < rings + 1; ring++)
			{
				r0 = (float)Math.Sin(ring * fDeltaRingAngle);
				y0 = (float)Math.Cos(ring * fDeltaRingAngle);

				for (int seg = 0; seg < segments + 1; seg++)
				{
					x0 = r0 * (float)Math.Sin(seg * fDeltaSegAngle);
					z0 = r0 * (float)Math.Cos(seg * fDeltaSegAngle);

					// add first vertex
					var pos = new Vector3(x0 * radius, y0 * radius, z0 * radius);
					GL.Normal3(pos);
					GL.Vertex3(pos);
				}  
			}
			GL.End();
		}


		private void _RenderLines_ICO(SSRenderConfig renderConfig) {
			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);

			GL.Begin(PrimitiveType.Lines);
			GL.LineWidth(1.0f);

			foreach (var face in icoSphereFaces) {
				var v1 = icoSphereVertices[face.v1] * radius;
				var v2 = icoSphereVertices[face.v2] * radius;
				var v3 = icoSphereVertices[face.v3] * radius;

				GL.Vertex3(v1);  GL.Vertex3(v2);
				GL.Vertex3(v2);  GL.Vertex3(v3);
				GL.Vertex3(v1);  GL.Vertex3(v3);
			}
			GL.End();
		}

		private void _RenderLines_UV(SSRenderConfig renderConfig) {

			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);

			// should generate these from screen-space size approximation
			float rings = 10.0f;
			float segments = 10.0f; 

			// Establish constants used in sphere generation
			float fDeltaRingAngle = ((float)Math.PI / rings);
			float fDeltaSegAngle = (2.0f * (float)Math.PI / segments);

			float r0;
			float y0;
			float x0;
			float z0;

			GL.Begin(PrimitiveType.LineStrip);

			GL.LineWidth(1.0f);

			for (int ring = 0; ring < rings + 1; ring++)
			{
				r0 = (float)Math.Sin(ring * fDeltaRingAngle);
				y0 = (float)Math.Cos(ring * fDeltaRingAngle);

				for (int seg = 0; seg < segments + 1; seg++)
				{
					x0 = r0 * (float)Math.Sin(seg * fDeltaSegAngle);
					z0 = r0 * (float)Math.Cos(seg * fDeltaSegAngle);

					// add first vertex
					var pos = new Vector3(x0 * radius, y0 * radius, z0 * radius);
					GL.Normal3(pos);
					GL.Vertex3(pos);
				}  
			}
			GL.End();
		}
	}
}

