// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Util.IcoSphere;

namespace SimpleScene
{
	public class SSObjectSphere : SSObject
	{
	    public float radius;
		public bool renderSolid = false;

		Vector3[] icoSphereVertices;
		TriangleIndices[] icoSphereFaces;

		public SSObjectSphere (float radius) : base() {
		    this.radius = radius;
		}
		
		public override void Render(ref SSRenderConfig renderConfig) {

		    base.Render(ref renderConfig);

			if (renderSolid) {
				this._RenderSolid(ref renderConfig);
			} else {
				// this._RenderLines_UV(ref renderConfig);
				this._RenderLines_ICO(ref renderConfig);
			}
		}

		private void _RenderSolid(ref SSRenderConfig renderConfig) {
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


		private void _RenderLines_ICO(ref SSRenderConfig renderConfig) {
			// mode setup
			SSShaderProgram.DeactivateAll(); // disable GLSL
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);
	
			if (icoSphereVertices == null) {
				var icoSphereCreator = new IcoSphereCreator();
				var icoSphereGeometry = icoSphereCreator.Create(3);
				icoSphereVertices = icoSphereGeometry.Positions.ToArray();
				icoSphereFaces = icoSphereGeometry.Faces.ToArray();
			}

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

		private void _RenderLines_UV(ref SSRenderConfig renderConfig) {

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

		public override bool Intersect (ref SSRay worldSpaceRay, out float distanceAlongRay)
		{
			// transform the ray into object space
			float localDistanceAlongRay;

			SSRay localRay = worldSpaceRay.Transformed(this.worldMat.Inverted());

			float distanceToSphereOrigin = OpenTKHelper.DistanceToLine(localRay,Vector3.Zero, out localDistanceAlongRay);
			distanceAlongRay = localDistanceAlongRay * this.Scale.LengthFast;
            bool result = distanceToSphereOrigin <= this.radius;
#if false
			Console.WriteLine("_____________________________");
			Console.WriteLine("sphere intersect test {0}   vs radius {1}",distanceToSphereOrigin,radius);
			Console.WriteLine("worldray {0}",worldSpaceRay);
			Console.WriteLine("localray {0}",localRay);
			Console.WriteLine("objectPos {0}",this.Pos);

			if (result) {
				Console.WriteLine("     ----> hit <-----");
				Console.WriteLine("----------------------------");			    
			} else {
			    Console.WriteLine("          miss");
				Console.WriteLine("----------------------------");				
			}
#endif

            return result;
		}
	}
}

