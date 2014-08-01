using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSObjectSphere : SSObject
	{
	    public readonly float radius;


		public SSObjectSphere (float radius) : base() {
		    this.radius = radius;
		}
		

		public override void Render(ref SSRenderConfig renderConfig) {

		    base.Render(ref renderConfig);

			// mode setup
			GL.UseProgram(0); // disable GLSL
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);

			GL.LineWidth(1.0f);
			GL.Color3(1.0f,1.0f,1.0f);

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

