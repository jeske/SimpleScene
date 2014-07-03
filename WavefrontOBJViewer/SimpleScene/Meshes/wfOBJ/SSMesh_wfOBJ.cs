// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Util3d;

namespace WavefrontOBJViewer
{
    public class SSMesh_wfOBJ : SSAbstractMesh {
 
		protected List<SSMeshOBJSubsetData> geometrySubsets = new List<SSMeshOBJSubsetData>();
		SSAssetManagerContext ctx;
		public readonly string srcFilename;
		
		SSShaderProgram shaderPgm;
		
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
	        public UInt16[] indicies;
		}

		public override string ToString ()
		{
			return string.Format ("[SSMesh_FromOBJ:{0}]", this.srcFilename);
		}
		
#region Constructor
        public SSMesh_wfOBJ(SSAssetManagerContext ctx, string filename, bool mipmapped, SSShaderProgram shaderPgm = null) {
            this.srcFilename = filename;
            // this.mipmapped = mipmapped;
            this.ctx = ctx;
            this.shaderPgm = shaderPgm;

            WavefrontObjLoader wff_data = new WavefrontObjLoader(filename,
               delegate(string resource_name) { return ctx.getAsset(resource_name).Open(); });

			Console.WriteLine("wff vertex count = {0}",wff_data.positions.Count);
			Console.WriteLine("wff face count = {0}",wff_data.numFaces);

            _loadData(wff_data);
        }    
#endregion
        
		// TODO: extend GLSL to do wireframe in a geometry shader
		// http://www.lighthouse3d.com/tutorials/glsl-core-tutorial/geometry-shader/

		private void _renderSetupGLSL(SSMeshOBJSubsetData subset) {
			// Step 1: setup GL rendering modes...

			GL.Enable(EnableCap.CullFace);
			GL.Enable(EnableCap.Lighting);

			// GL.Enable(EnableCap.Blend);
			// GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

			// Step 2: setup our material mode and paramaters...

			GL.Color3(System.Drawing.Color.White);  // clear the vertex color to white..

			if (shaderPgm == null) {
				// fixed function single-texture
				GL.Enable(EnableCap.Texture2D);
				GL.BindTexture(TextureTarget.Texture2D, subset.diffuseTexture.TextureID);
			} else {
				// activate GLSL shader
				GL.UseProgram(shaderPgm.ProgramID);

				// bind our texture-images to GL texture-units 
				// http://adriangame.blogspot.com/2010/05/glsl-multitexture-checklist.html
				GL.ActiveTexture(TextureUnit.Texture0);
				GL.BindTexture(TextureTarget.Texture2D, subset.diffuseTexture.TextureID);
				GL.ActiveTexture(TextureUnit.Texture1);
				GL.BindTexture(TextureTarget.Texture2D, subset.specularTexture.TextureID);
				GL.ActiveTexture(TextureUnit.Texture2);
				GL.BindTexture(TextureTarget.Texture2D, subset.ambientTexture.TextureID);
				GL.ActiveTexture(TextureUnit.Texture3);
				GL.BindTexture(TextureTarget.Texture2D, subset.bumpTexture.TextureID);

				// get shader uniform variable handles (these are named variables in the shader)
				int h0 = GL.GetUniformLocation(shaderPgm.ProgramID, "diffTex");
				int h1 = GL.GetUniformLocation(shaderPgm.ProgramID, "specTex");
				int h2 = GL.GetUniformLocation(shaderPgm.ProgramID, "ambiTex");
				int h3 = GL.GetUniformLocation(shaderPgm.ProgramID, "bumpTex");

				// bind shader uniform variable handles to GL texture-unit numbers
				GL.Uniform1(h0,0);
				GL.Uniform1(h1,1);
				GL.Uniform1(h2,2);
				GL.Uniform1(h3,3);
			}
		}

		private void _renderSetupWireframe() {
			GL.UseProgram(0); // turn off GLSL
			GL.Enable(EnableCap.CullFace);
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);
		}

		private void _renderSendTriangles(SSMeshOBJSubsetData subset) {
			// Step 3: draw faces.. here we use the "old school" manual method of drawing

			//         note: programs written for modern OpenGL & D3D don't do this!
			//               instead, they hand the vertex-buffer and index-buffer to the
			//               GPU and let it do this..

			GL.Begin(BeginMode.Triangles);
			foreach(var idx in subset.indicies) {
				var vertex = subset.vertices[idx];  // retrieve the vertex

				// draw the vertex..
				GL.Color3(System.Drawing.Color.FromArgb(vertex.DiffuseColor));
				GL.TexCoord2(vertex.Tu,vertex.Tv);
				GL.Normal3(vertex.Normal);
				GL.Vertex3(vertex.Position);
			}
			GL.End();
		}
		private void _renderSendLines(SSMeshOBJSubsetData subset) {
			// Step 3: draw faces.. here we use the "old school" manual method of drawing

			//         note: programs written for modern OpenGL & D3D don't do this!
			//               instead, they hand the vertex-buffer and index-buffer to the
			//               GPU and let it do this..


			for(int i=2;i<subset.indicies.Length;i+=3) {
				var v1 = subset.vertices [subset.indicies[i - 2]];
				var v2 = subset.vertices [subset.indicies[i - 1]];
				var v3 = subset.vertices [subset.indicies[i]];

				// draw the vertex..
				GL.Color3(System.Drawing.Color.FromArgb(v1.DiffuseColor));

				GL.Begin(BeginMode.LineLoop);
				GL.Vertex3 (v1.Position);
				GL.Vertex3 (v2.Position);
				GL.Vertex3 (v3.Position);
				GL.End();
			}

		}


		public override void RenderMesh(ref SSRenderConfig renderConfig) {		
			foreach (SSMeshOBJSubsetData subset in this.geometrySubsets) {

				if (renderConfig.drawGLSL) {
					_renderSetupGLSL (subset);
					_renderSendTriangles (subset);
			
				}

				if (renderConfig.drawWireframes) {
					_renderSetupWireframe ();
					_renderSendLines (subset);
				}
			}
		}

#region Load Data
        private void _loadData(WavefrontObjLoader m) {
            foreach (var srcmat in m.materials) {
                if (srcmat.faces.Count != 0) {
                    this.geometrySubsets.Add(_loadMaterialSubset(m, srcmat));
                }
            }
        }
        
        private SSMeshOBJSubsetData _loadMaterialSubset(WavefrontObjLoader wff, WavefrontObjLoader.MaterialFromObj objMatSubset) {
            // create new mesh subset-data
            SSMeshOBJSubsetData subsetData = new SSMeshOBJSubsetData();            

            // setup the material...
            subsetData.material = new SSMaterial();

            // load and link every texture present 
            if (objMatSubset.diffuseTextureResourceName != null) {
                subsetData.diffuseTexture = new SSTexture_FromAsset(ctx.getAsset(objMatSubset.diffuseTextureResourceName));
            }
            if (objMatSubset.ambientTextureResourceName != null) {
                subsetData.ambientTexture = new SSTexture_FromAsset(ctx.getAsset(objMatSubset.ambientTextureResourceName));
            } 
            if (objMatSubset.bumpTextureResourceName != null) {
                subsetData.bumpTexture = new SSTexture_FromAsset(ctx.getAsset(objMatSubset.bumpTextureResourceName));
            }
            if (objMatSubset.specularTextureResourceName != null) {
                subsetData.specularTexture = new SSTexture_FromAsset(ctx.getAsset(objMatSubset.specularTextureResourceName));
            }

            // generate renderable geometry data...
            VertexSoup_VertexFormatBinder.generateDrawIndexBuffer(wff, out subsetData.indicies, out subsetData.vertices);           

			// TODO: setup VBO/IBO buffers
			// http://www.opentk.com/doc/graphics/geometry/vertex-buffer-objects

            return subsetData;
        }
#endregion
    }
}