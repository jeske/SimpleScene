﻿// Copyright(C) David W. Jeske, 2014
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
		SSShaderProgram_Main shaderPgm;

		MeshGeometry3D geom;

		SSVertexBuffer<SSVertex_PosNormDiffTex1> vbo;
		SSIndexBuffer ibo;
		int num_indicies;
		int num_vertices;

		SSTexture texture;

		public SSMesh_SphereICO (int divisions, SSShaderProgram_Main shaderPgm, SSTexture texture)
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
			
			// compute azimuth (x texture coord)
			float longitude = (float) Math.Atan2(vpn.X,vpn.Z);       // azimuth [-PI  to PI]   			
			u = (float) (( longitude / (2.0 * Math.PI) ) + 0.5);     // [0 to 1]

			// compute altitude (y texture coord)
			float latitude = (float) Math.Acos(vpn.Y);               // altitude [0 to PI]
			v = (float) (latitude / Math.PI);                        // [0 to 1]

			// v = (float) ( Math.Log((1.0 + Math.Sin(latitude))/(1.0 - Math.Sin(latitude))) / (4.0 * Math.PI) );
		}

		private void _Create(int divisions) {
			var icoSphereCreator = new IcoSphereCreator();
			geom = icoSphereCreator.Create(divisions);
			var positions = geom.Positions.ToArray();

			var vertexSoup = new Util3d.VertexSoup<SSVertex_PosNormDiffTex1>();
			var indexList = new List<UInt16>();

			// we have to process each face in the IcoSphere, so we can
			// properly "wrap" the texture-coordinates that fall across the left/right border
			foreach (TriangleIndices face in geom.Faces) {
				var vp1 = positions[face.v1];
				var vp2 = positions[face.v2];
				var vp3 = positions[face.v3];

				var normal1 = vp1.Normalized();
				var normal2 = vp2.Normalized();
				var normal3 = vp3.Normalized();

				float s1,s2,s3,t1,t2,t3;

			    _computeEquirectangularUVForSpherePoint(normal1,out s1, out t1);
				_computeEquirectangularUVForSpherePoint(normal2,out s2, out t2);
				_computeEquirectangularUVForSpherePoint(normal3,out s3, out t3);

				// configure verticies

				var v1 = new SSVertex_PosNormDiffTex1();
				v1.Position = vp1;
				v1.Normal = normal1;
				v1.Tu = s1;
				v1.Tv = t1;

				var v2 = new SSVertex_PosNormDiffTex1();
				v2.Position = vp2;
				v2.Normal = normal2;
				v2.Tu = s2;
				v2.Tv = t2;

				var v3 = new SSVertex_PosNormDiffTex1();
				v3.Position = vp3;
				v3.Normal = normal3;
				v3.Tu = s3;
				v3.Tv = t3;

				// if a triangle spans the left/right seam where U transitions from 1 to -1
				bool v1_left = vp1.X < 0.0f;
				bool v2_left = vp2.X < 0.0f;
				bool v3_left = vp3.X < 0.0f;
				if (vp1.Z < 0.0f && vp2.Z < 0.0f && vp3.Z < 0.0f &&
                    ((v2_left != v1_left) || (v3_left != v1_left))) {
					// we need to "wrap" texture coordinates
					if (v1.Tu < 0.5f) { v1.Tu += 1.0f; }
					if (v2.Tu < 0.5f) { v2.Tu += 1.0f; }
					if (v3.Tu < 0.5f) { v3.Tu += 1.0f; }
				}

				// add configured verticies to mesh..

				UInt16 idx1 = vertexSoup.digestVertex(ref v1);
				UInt16 idx2 = vertexSoup.digestVertex(ref v2);
				UInt16 idx3 = vertexSoup.digestVertex(ref v3);

				indexList.Add(idx1);
				indexList.Add(idx2);
				indexList.Add(idx3);
			}

			var vertexArr = vertexSoup.verticies.ToArray();
			var idxArr = indexList.ToArray();

			num_vertices = vertexArr.Length;
			num_indicies = idxArr.Length;

			// upload to GL
			vbo = new SSVertexBuffer<SSVertex_PosNormDiffTex1>(vertexArr);
			ibo = new SSIndexBuffer (idxArr, vbo);
		}


		public override void RenderMesh(ref SSRenderConfig renderConfig) {	
			GL.UseProgram(shaderPgm.ProgramID);

			// turn off other texture layers
			GL.Uniform1(shaderPgm.u_specTexEnabled,(int)0); 
			GL.Uniform1(shaderPgm.u_ambiTexEnabled,(int)0); 
			GL.Uniform1(shaderPgm.u_bumpTexEnabled,(int)0); 


			GL.ActiveTexture(TextureUnit.Texture0);
			if (texture != null) {
				GL.BindTexture(TextureTarget.Texture2D, texture.TextureID);
				GL.Uniform1(shaderPgm.u_diffTexEnabled,(int)1); 
			} else {
				GL.BindTexture(TextureTarget.Texture2D, 0);
				GL.Uniform1(shaderPgm.u_diffTexEnabled,(int)0); 
			}
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            ibo.DrawElements(PrimitiveType.Triangles, this.shaderPgm);
		}


		public override IEnumerable<Vector3> EnumeratePoints ()
		{
			foreach (var point in geom.Positions) {
				yield return point;
			}
		}

	}
}

