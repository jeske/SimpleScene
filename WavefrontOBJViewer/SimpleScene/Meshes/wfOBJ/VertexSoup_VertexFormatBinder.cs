// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;

using Util3d;
using OpenTK;

namespace WavefrontOBJViewer
{
	// this class is the binder between the WavefrontObjLoader types and the
	// OpenTK SimpleScene vertex formats

	public static class VertexSoup_VertexFormatBinder
	{

		// convert wavefrontobjloader vector formats, to our OpenTK Vector3 format
		private static Vector3 CV(WavefrontObjLoader.Vector_XYZW dxv) { return new Vector3(dxv.X, dxv.Y, dxv.Z); }
		private static Vector3 CV(WavefrontObjLoader.Vector_XYZ dxv) { return new Vector3(dxv.X, dxv.Y, dxv.Z); }

		// generateDrawIndexBuffer(..)
		//
		// Walks the wavefront faces, feeds pre-configured verticies to the VertexSoup, 
		// and returns a new index-buffer pointing to the new VertexSoup.verticies indicies.

		public static void generateDrawIndexBuffer(
			WavefrontObjLoader wff, 
			out Int16[] indicies_return, 
			out SSVertex_PosNormDiffTex1[] verticies_return) 
		{
			var soup = new VertexSoup<SSVertex_PosNormDiffTex1>();
			List<Int16> indicies = new List<Int16>();

			// (0) go throu`gh the materials and faces

			// load indexes
			foreach (var mtl in wff.materials) {
				foreach (var face in mtl.faces) {

					// extract the unique verticies for a face...
					SSVertex_PosNormDiffTex1[] vertex_list = new SSVertex_PosNormDiffTex1[face.v_idx.Length];                    
					for (int facevertex = 0; facevertex < face.v_idx.Length; facevertex++) {                        
						vertex_list[facevertex].Position = CV(wff.positions[face.v_idx[facevertex]]);
						{
							int normal_idx = face.n_idx[facevertex];
							if (normal_idx != -1) {
								vertex_list[facevertex].Normal = CV(wff.normals[normal_idx]); 
							}
						}
						{
							int tex_index = face.tex_idx[facevertex];
							if (tex_index != -1 ) {
								vertex_list[facevertex].Tu = wff.texCoords[tex_index].U; 
								vertex_list[facevertex].Tv = 1- wff.texCoords[tex_index].V;
							}
						}

						vertex_list[facevertex].DiffuseColor = WavefrontObjLoader.CIEXYZtoRGB(mtl.vDiffuse);
					}

					// turn them into indicies in the vertex soup..
					Int16[] newindicies = soup.digestVerticies(vertex_list);
					if (newindicies.Length == 3) { // triangle
						indicies.Add(newindicies[0]);
						indicies.Add(newindicies[1]);
						indicies.Add(newindicies[2]);
					} else if (newindicies.Length == 4) { // quad
						indicies.Add(newindicies[0]);
						indicies.Add(newindicies[1]);
						indicies.Add(newindicies[2]);

						indicies.Add(newindicies[0]);
						indicies.Add(newindicies[2]);
						indicies.Add(newindicies[3]);
					} else {
						// this is a pretty bad ngon algorithm, because it's going to make thin
						// fanstrips. It would be better to consume from both sides of the index 
						// list to generate triangles.
						for (int x = 1; x < (newindicies.Length - 2); x++) {
							indicies.Add(newindicies[0]);
							indicies.Add(newindicies[x]);
							indicies.Add(newindicies[x+1]);
						}
						// throw new NotImplementedException("unhandled face size: " + newindicies.Length);                    
					}
				}
			}
			indicies_return = indicies.ToArray();
			verticies_return = soup.verticies.ToArray();
		}
	}
}

