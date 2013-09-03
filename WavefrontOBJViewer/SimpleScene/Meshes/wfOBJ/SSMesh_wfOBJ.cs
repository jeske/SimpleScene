// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Util3d;

namespace WavefrontOBJViewer
{
    public class SSMesh_wfOBJ : SSMesh {
 
		protected List<SSMeshOBJSubsetData> geometrySubsets = new List<SSMeshOBJSubsetData>();
		SSAssetManagerContext ctx;
		public readonly string srcFilename;
		
		// private string filename = "";
        // private bool mipmapped = false;

	    public struct SSMeshOBJSubsetData {
	   		public SSTexture diffuseTexture;
	   		public SSTexture specularTexture;
	   		public SSTexture ambientTexture;
	   		public SSTexture bumpTexture;

			public SSMaterial material;
	
			// face geometry
			public SSVertex_PosNormDiffTex1[] vertices;
	        public Int16[] indicies;
		}

		public override string ToString ()
		{
			return string.Format ("[SSMesh_FromOBJ:{0}]", this.srcFilename);
		}
		
#region Constructor
        public SSMesh_wfOBJ(SSAssetManagerContext ctx, string filename, bool mipmapped) {
            this.srcFilename = filename;
            // this.mipmapped = mipmapped;
            this.ctx = ctx;

            WavefrontObjLoader wff_data = new WavefrontObjLoader(filename,
               delegate(string resource_name) { return ctx.openResource(resource_name); });

            _makeData(wff_data);
        }    
#endregion
        
		public override void Render(){			
			foreach (SSMeshOBJSubsetData subset in this.geometrySubsets) {
				// setup texture rendering (should only do this if it's not already done)
				GL.Enable(EnableCap.CullFace);
                GL.Enable(EnableCap.Texture2D);
                // GL.Enable(EnableCap.Blend);
                // GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
				
				// set material
				GL.BindTexture(TextureTarget.Texture2D, subset.diffuseTexture.TextureID);
				GL.Color3(System.Drawing.Color.White);  // clear the vertex color to white..

				
				// draw faces
				GL.Begin(BeginMode.Triangles);
				foreach(var idx in subset.indicies) {
					var vertex = subset.vertices[idx];
					GL.Color3(vertex.DiffuseColor);
					GL.TexCoord2(vertex.Tu,vertex.Tv);
					GL.Normal3(vertex.Normal);
					GL.Vertex3(vertex.Position);
                }
                GL.End();
			}
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
                subsetData.diffuseTexture = new SSTexture(ctx.fullHandlePathForResource(objMatSubset.diffuseTextureResourceName));
            } else if (objMatSubset.ambientTextureResourceName != null) {
                subsetData.ambientTexture = new SSTexture(ctx.fullHandlePathForResource(objMatSubset.ambientTextureResourceName));
            } else if (objMatSubset.bumpTextureResourceName != null) {
                subsetData.bumpTexture = new SSTexture(ctx.fullHandlePathForResource(objMatSubset.bumpTextureResourceName));
            } else if (objMatSubset.specularTextureResourceName != null) {
                subsetData.specularTexture = new SSTexture(ctx.fullHandlePathForResource(objMatSubset.specularTextureResourceName));
            }

            // generate renderable geometry data...
            VertexSoup_VertexFormatBinder.generateDrawIndexBuffer(wff, out subsetData.indicies, out subsetData.vertices);           

			// TODO: setup VBO/IBO buffers
			// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

            return subsetData;
        }
    }
}