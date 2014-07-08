using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace WavefrontOBJViewer
{
	public class SSMesh_Starfield : SSAbstractMesh
	{
		SSVertex_PosNormDiff[] vertices = null;
		int numstars;

		public SSMesh_Starfield (int numstars) {
		    // generate the stars
		    this.numstars = numstars;
		    vertices = new SSVertex_PosNormDiff[numstars];


			Random rgen = new Random();

			for (int i = 0; i < numstars; i++) {
                // generate a random position
                vertices[i].Position = new Vector3(
                    (float)rgen.NextDouble() * 20.0f - 10.0f,
                    (float)rgen.NextDouble() * 20.0f - 10.0f,
                    (float)rgen.NextDouble() * 20.0f - 10.0f);

                // the normals are simplified
                vertices[i].Normal = vertices[i].Position;
                int intensity = rgen.Next(150);
                vertices[i].DiffuseColor = Color.FromArgb(255,
                    rgen.Next(40) + intensity,
                    rgen.Next(25) + intensity,
                    rgen.Next(25)+ intensity).ToArgb();
            }
		}

		public override void RenderMesh(ref SSRenderConfig renderConfig) {
			GL.UseProgram(0);

			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Lighting);	

			GL.Enable(EnableCap.PointSmooth);
			GL.PointSize(2.0f);
			GL.Begin(BeginMode.Points);
			for (int i = 0; i < this.numstars; i++) {			   

			   GL.Color3(Color.FromArgb(vertices[i].DiffuseColor));
			   GL.Normal3(vertices[i].Normal);
			   GL.Vertex3(vertices[i].Position);
			}
			GL.End();


		}	

	}
}

