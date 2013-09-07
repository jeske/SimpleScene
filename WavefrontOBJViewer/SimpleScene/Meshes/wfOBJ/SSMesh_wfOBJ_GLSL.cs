// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Util3d;

namespace WavefrontOBJViewer
{
	public class SSMesh_wfOBJ_GLSL : SSMesh {

		protected List<SSMeshOBJSubsetData> geometrySubsets = new List<SSMeshOBJSubsetData>();
		SSAssetManagerContext ctx;
		public readonly string srcFilename;

		// private string filename = "";
		// private bool mipmapped = false;

		public struct SSMeshOBJSubsetData {
			public SSTexture texture;
			public SSMaterial material;

			// face geometry
			public SSVertex_PosNormDiffTex1[] vertices;
			public UInt16[] indicies;
		}

		public override string ToString ()
		{
			return string.Format ("[SSMesh_FromOBJ_GLSL:{0}]", this.srcFilename);
		}

		#region Constructor
		public SSMesh_wfOBJ_GLSL(SSAssetManagerContext ctx, string filename, bool mipmapped) {
			this.srcFilename = filename;
			// this.mipmapped = mipmapped;
			this.ctx = ctx;

			WavefrontObjLoader wff_data = new WavefrontObjLoader(filename,
			                                                     delegate(string resource_name) { return ctx.openResource(resource_name); });

			_makeData(wff_data);
		}    
		#endregion

		public override void Render(){			
			throw new NotImplementedException ();
		}


		private void _makeData(WavefrontObjLoader m) {
			foreach (var srcmat in m.materials) {
				if (srcmat.faces.Count != 0) {
					this.geometrySubsets.Add(_makeMaterialSubset(m, srcmat));
				}
			}
		}

		private SSMeshOBJSubsetData _makeMaterialSubset(WavefrontObjLoader wff, WavefrontObjLoader.MaterialFromObj objMatSubset) {
			// create new mesh subset-data
			SSMeshOBJSubsetData subsetData = new SSMeshOBJSubsetData();            

			// setup the material...
			subsetData.material = new SSMaterial();
			// assign diffuse, ambient, etc...
			// load-link the texture...
			if (objMatSubset.diffuseTextureResourceName != null) {
				subsetData.texture = new SSTexture(ctx.fullHandlePathForResource(objMatSubset.diffuseTextureResourceName));
			} else if (objMatSubset.ambientTextureResourceName != null) {
				subsetData.texture = new SSTexture(ctx.fullHandlePathForResource(objMatSubset.ambientTextureResourceName));
			}

			// generate renderable geometry data...
			VertexSoup_VertexFormatBinder.generateDrawIndexBuffer(wff, out subsetData.indicies, out subsetData.vertices);           

			// TODO: setup VBO/IBO buffers
			// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

			return subsetData;
		}
	}
}