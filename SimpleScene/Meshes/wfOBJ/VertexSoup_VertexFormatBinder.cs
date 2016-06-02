// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;

using SimpleScene.Util3d;
using OpenTK;

namespace SimpleScene
{
	// this class is the binder between the WavefrontObjLoader types and the
	// OpenTK SimpleScene vertex formats

	public static class VertexSoup_VertexFormatBinder
	{

		// convert wavefrontobjloader vector formats, to our OpenTK Vector3 format
		// generateDrawIndexBuffer(..)
		//
		// Walks the wavefront faces, feeds pre-configured verticies to the VertexSoup, 
		// and returns a new index-buffer pointing to the new VertexSoup.verticies indicies.

		public static void generateDrawIndexBuffer(
			WavefrontObjLoader wff, 
            WavefrontObjLoader.MaterialInfoWithFaces objMatSubset,
			out UInt16[] indicies_return, 
			out SSVertex_PosNormTex[] verticies_return) 
		{
			const bool shouldDedup = true; // this lets us turn on/of vertex-soup deduping

			var soup = new VertexSoup<SSVertex_PosNormTex>(deDup:shouldDedup);
			List<UInt16> draw_indicies = new List<UInt16>();

			// (0) go throu`gh the materials and faces, DENORMALIZE from WF-OBJ into fully-configured verticies

			// load indexes
			var m = objMatSubset;

			// wavefrontOBJ stores color in CIE-XYZ color space. Convert this to Alpha-RGB
			var materialDiffuseColor = WavefrontObjLoader.CIEXYZtoColor(m.mtl.vDiffuse).ToArgb();

			foreach (var face in m.faces) {

				// iterate over the vericies of a wave-front FACE...

				// DEREFERENCE each .obj vertex paramater (position, normal, texture coordinate)
				SSVertex_PosNormTex[] vertex_list = new SSVertex_PosNormTex[face.v_idx.Length];                    
				for (int facevertex = 0; facevertex < face.v_idx.Length; facevertex++) {     

					// position
					vertex_list[facevertex].Position = wff.positions[face.v_idx[facevertex]].Xyz;

					// normal
					int normal_idx = face.n_idx[facevertex];
					if (normal_idx != -1) {
						vertex_list[facevertex].Normal = wff.normals[normal_idx]; 
					}

					// texture coordinate
					int tex_index = face.tex_idx[facevertex];
					if (tex_index != -1 ) {
						vertex_list[facevertex].Tu = wff.texCoords[tex_index].X; 
						vertex_list[facevertex].Tv = 1- wff.texCoords[tex_index].Y;
					}
				}

				// turn them into indicies in the vertex soup..
				//   .. we hand the soup a set of fully configured verticies. It
				//   .. dedups and accumulates them, and hands us back indicies
				//   .. relative to it's growing list of deduped verticies. 
				UInt16[] soup_indicies = soup.digestVerticies(vertex_list);

				// now we add these indicies to the draw-list. Right now we assume
				// draw is using GL_TRIANGLE, so we convert NGONS into triange lists
				if (soup_indicies.Length == 3) { // triangle
					draw_indicies.Add(soup_indicies[0]);
					draw_indicies.Add(soup_indicies[1]);
					draw_indicies.Add(soup_indicies[2]);
				} else if (soup_indicies.Length == 4) { // quad
					draw_indicies.Add(soup_indicies[0]);
					draw_indicies.Add(soup_indicies[1]);
					draw_indicies.Add(soup_indicies[2]);

					draw_indicies.Add(soup_indicies[0]);
					draw_indicies.Add(soup_indicies[2]);
					draw_indicies.Add(soup_indicies[3]);
				} else {
					// This n-gon algorithm only works if the n-gon is coplanar and convex,
					// which Wavefront OBJ says they must be. 
					//  .. to tesselate concave ngons, one must tesselate using a more complex method, see
					//    http://en.wikipedia.org/wiki/Polygon_triangulation#Ear_clipping_method
						
					// manually generate a triangle-fan
					for (int x = 1; x < (soup_indicies.Length-1); x++) {
						draw_indicies.Add(soup_indicies[0]);
						draw_indicies.Add(soup_indicies[x]);
						draw_indicies.Add(soup_indicies[x+1]);
					}
					// throw new NotImplementedException("unhandled face size: " + newindicies.Length);                    
				}
			}


			// convert the linked-lists into arrays and return
			indicies_return = draw_indicies.ToArray();
			verticies_return = soup.verticies.ToArray();

			Console.WriteLine ("VertexSoup_VertexFormatBinder:generateDrawIndexBuffer : \r\n   {0} verticies, {1} indicies.  Dedup = {2}",
			                  verticies_return.Length, indicies_return.Length,
			                  shouldDedup ? "YES" : "NO");

		}
		
		
		
	}
}

