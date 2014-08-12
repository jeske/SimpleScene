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

		public SSMesh_SphereICO (int divisions, SSShaderProgram shaderPgm)
		{
			this.shaderPgm = shaderPgm;
			this._Create(divisions);
		}

		private void _Create(int divisions) {
			var icoSphereCreator = new IcoSphereCreator();
			geom = icoSphereCreator.Create(divisions);

			num_vertices = geom.Positions.Count;
			num_indicies = geom.MeshIndicies.Count * 3;

			SSVertex_PosNormDiffTex1[] vertices = new SSVertex_PosNormDiffTex1[num_vertices];
			UInt16[] indicies = new UInt16[num_indicies];

			// populate the arrays

			vbo = new SSVertexBuffer<SSVertex_PosNormDiffTex1>(vertices);
			ibo = new SSIndexBuffer<UInt16> (indicies, sizeof(UInt16));	
		}


		public override void RenderMesh(ref SSRenderConfig renderConfig) {	
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

