// Copyright(C) David W. Jeske, 2014
// Released to the public domain.

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Util.IcoSphere;

namespace SimpleScene
{
	public class SSMesh_SphereICO : SSAbstractMesh
	{
		SSShaderProgram shaderPgm;

		MeshGeometry3D geom;

		SSVertexBuffer<SSVertex_PosNormDiffTex1> vbo;
		SSIndexBuffer<UInt16> ibo;
		int num_indicies;
		int num_vertices;

		SSTexture texture;

		public SSMesh_SphereICO (int divisions, SSShaderProgram shaderPgm, SSTexture texture)
		{
			this.shaderPgm = shaderPgm;
			this.texture = texture;

			this._Create(divisions);
		}

		private void _computeEquirectangularUVForSpherePoint(Vector3 p, out float u, out float v) {
			// http://paulbourke.net/geometry/transformationprojection/

			// this is the unit surface normal (and surface point)
			Vector3 vpn = p.Normalized();

			// compute latitute and longitute values from the vector to the normalized sphere surface position
			// using Y-up coordinates
			
			float latitude = (float) Math.Atan2(vpn.Y, Math.Sqrt( vpn.X * vpn.X + vpn.Z * vpn.Z) );
			float longitude = (float) Math.Atan2(vpn.X,vpn.Z);

			u = (float) ( longitude / Math.PI ) ;
			v = (float) ( Math.Log((1.0 + Math.Sin(latitude))/(1.0 - Math.Sin(latitude))) / (4.0 * Math.PI) );
		}

		private void _Create(int divisions) {
			var icoSphereCreator = new IcoSphereCreator();
			geom = icoSphereCreator.Create(divisions);

			num_vertices = geom.Positions.Count;
			num_indicies = geom.MeshIndicies.Count;
			var geom_verts = geom.Positions.ToArray();
			var geom_indicies = geom.MeshIndicies.ToArray();

			SSVertex_PosNormDiffTex1[] vertices = new SSVertex_PosNormDiffTex1[num_vertices];
			UInt16[] indicies = new UInt16[num_indicies];

			// populate vertex array
			for(int vi = 0; vi < num_vertices ; vi++ ) {
				vertices[vi].Position = geom_verts[vi];
				vertices[vi].Normal = geom_verts[vi].Normalized(); // surface normal is the unit vector
				vertices[vi].DiffuseColor = System.Drawing.Color.White.ToArgb();

				_computeEquirectangularUVForSpherePoint(
					vertices[vi].Normal,
					out vertices[vi].Tu,
					out vertices[vi].Tv);
			}
			// populate index array
			for(int ii=0; ii < num_indicies ; ii++) {
				indicies[ii] = (UInt16) geom_indicies[ii];
			}

			// upload to GL

			vbo = new SSVertexBuffer<SSVertex_PosNormDiffTex1>(vertices);
			ibo = new SSIndexBuffer<UInt16> (indicies, sizeof(UInt16));	
		}


		public override void RenderMesh(ref SSRenderConfig renderConfig) {	
			GL.UseProgram(shaderPgm.ProgramID);

			GL.ActiveTexture(TextureUnit.Texture0);
			if (texture != null) {
				GL.BindTexture(TextureTarget.Texture2D, texture.TextureID);
			} else {
				GL.BindTexture(TextureTarget.Texture2D, 0);
			}
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			vbo.bind (this.shaderPgm);
			ibo.bind ();

			//GL.DrawArrays (PrimitiveType.Triangles, 0, 6);
			GL.DrawElements (PrimitiveType.Triangles, num_indicies, DrawElementsType.UnsignedShort, IntPtr.Zero);
			ibo.unbind ();
			vbo.unbind ();	
		}


		public override IEnumerable<Vector3> EnumeratePoints ()
		{
			foreach (var point in geom.Positions) {
				yield return point;
			}
		}

	}
}

