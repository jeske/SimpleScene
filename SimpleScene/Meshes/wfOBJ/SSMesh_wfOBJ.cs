// Copyright(C) David W. Jeske, 2013
// Released to the public domain.

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using Util3d;

namespace SimpleScene
{
    public class SSMesh_wfOBJ : SSAbstractMesh {
 
		protected List<SSMeshOBJSubsetData> geometrySubsets = new List<SSMeshOBJSubsetData>();
		SSAssetManagerContext ctx;
		public readonly string srcFilename;
		
	    public struct SSMeshOBJSubsetData {
	   		public SSTexture diffuseTexture;
	   		public SSTexture specularTexture;
	   		public SSTexture ambientTexture;
	   		public SSTexture bumpTexture;			
	
			// raw geometry
			public SSVertex_PosNormDiffTex1[] vertices;
			public UInt16[] indicies;
			public UInt16[] wireframe_indicies;

			// handles to OpenTK/OpenGL Vertex-buffer and index-buffer objects
			// these are buffers stored on the videocard for higher performance rendering
			public SSVertexBuffer<SSVertex_PosNormDiffTex1> vbo;	        
			public SSIndexBuffer ibo;
			public SSIndexBuffer ibo_wireframe;
		}

		public override string ToString ()
		{
			return string.Format ("[SSMesh_FromOBJ:{0}]", this.srcFilename);
		}
		
#region Constructor
        public SSMesh_wfOBJ(SSAssetManagerContext ctx, string filename) {
            this.srcFilename = filename;            
            this.ctx = ctx;

            Console.WriteLine("SSMesh_wfOBJ: loading wff {0}",filename);
            WavefrontObjLoader wff_data = new WavefrontObjLoader(filename,
               delegate(string resource_name) { return ctx.getAsset(resource_name).Open(); });


			Console.WriteLine("wff vertex count = {0}",wff_data.positions.Count);
			Console.WriteLine("wff face count = {0}",wff_data.numFaces);

            _loadData(wff_data);
        }    
#endregion

		private void _renderSetupGLSL(ref SSRenderConfig renderConfig, SSMeshOBJSubsetData subset) {
			// Step 1: setup GL rendering modes...

			GL.Enable(EnableCap.CullFace);
			// GL.Enable(EnableCap.Lighting);

			// GL.Enable(EnableCap.Blend);
			// GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			// GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

			// Step 2: setup our material mode and paramaters...

			GL.Color3(System.Drawing.Color.White);  // clear the vertex color to white..

            SSShaderProgram_Main shaderPgm = renderConfig.BaseShader;

			if (shaderPgm == null) {
				GL.UseProgram(0);
				GL.Disable(EnableCap.CullFace);

				// fixed function single-texture
				GL.Disable(EnableCap.Texture2D);
				if (subset.diffuseTexture != null) {
					GL.ActiveTexture(TextureUnit.Texture0);
					GL.Enable(EnableCap.Texture2D);
					GL.BindTexture(TextureTarget.Texture2D, subset.diffuseTexture.TextureID);
				}
			} else {
				// activate GLSL shader
				GL.UseProgram(shaderPgm.ProgramID);

				// bind our texture-images to GL texture-units 
				// http://adriangame.blogspot.com/2010/05/glsl-multitexture-checklist.html

				// these texture-unit assignments are hard-coded in the shader setup

				GL.ActiveTexture(TextureUnit.Texture0);
				if (subset.diffuseTexture != null) {
					GL.BindTexture(TextureTarget.Texture2D, subset.diffuseTexture.TextureID);
					GL.Uniform1(shaderPgm.u_diffTexEnabled,(int)1); 

				} else {
					GL.BindTexture(TextureTarget.Texture2D, 0);
					GL.Uniform1(shaderPgm.u_diffTexEnabled,(int)0);
				}
				GL.ActiveTexture(TextureUnit.Texture1);
				if (subset.specularTexture != null) {
					GL.BindTexture(TextureTarget.Texture2D, subset.specularTexture.TextureID);
					GL.Uniform1(shaderPgm.u_specTexEnabled,(int)1); 
				} else {
					GL.BindTexture(TextureTarget.Texture2D, 0);
					GL.Uniform1(shaderPgm.u_specTexEnabled,(int)0); 
				}
				GL.ActiveTexture(TextureUnit.Texture2);
				if (subset.ambientTexture != null) {
					GL.BindTexture(TextureTarget.Texture2D, subset.ambientTexture.TextureID);
					GL.Uniform1(shaderPgm.u_ambiTexEnabled,(int)1); 
				} else {
					GL.BindTexture(TextureTarget.Texture2D, 0);
					GL.Uniform1(shaderPgm.u_ambiTexEnabled,(int)0); 
				}
				GL.ActiveTexture(TextureUnit.Texture3);
				if (subset.bumpTexture != null) {
					GL.BindTexture(TextureTarget.Texture2D, subset.bumpTexture.TextureID);
					GL.Uniform1(shaderPgm.u_bumpTexEnabled,(int)1); 
				} else {
					GL.BindTexture(TextureTarget.Texture2D, 0);
					GL.Uniform1(shaderPgm.u_ambiTexEnabled,(int)0); 
				}

				// reset to texture-unit 0 to be friendly..
				GL.ActiveTexture(TextureUnit.Texture0);				
			}
		}

		private void _renderSetupWireframe() {
			GL.UseProgram(0); // turn off GLSL
			GL.Enable(EnableCap.CullFace);
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);
		}

		public override IEnumerable<Vector3> EnumeratePoints ()
		{
		    foreach (var subset in geometrySubsets) {
				foreach (var vtx in subset.vertices) {
				    yield return vtx.Position;
				}
			}
		}
		public override bool TraverseTriangles<T>(T state, traverseFn<T> fn) {
			foreach(var subset in geometrySubsets) {
				for(int idx=0;idx < subset.indicies.Length;idx+=3) {
					var V1 = subset.vertices[subset.indicies[idx]].Position;
					var V2 = subset.vertices[subset.indicies[idx+1]].Position;
					var V3 = subset.vertices[subset.indicies[idx+2]].Position;
					bool finished = fn(state, V1, V2, V3);
					if (finished) { 
						return true; 
					}
				}
			}
			return false;
		}



		private void _renderSendVBOTriangles(ref SSRenderConfig renderConfig, SSMeshOBJSubsetData subset) {
            subset.ibo.DrawElements(PrimitiveType.Triangles, renderConfig.BaseShader);
		}

		private void _renderSendTriangles(ref SSRenderConfig renderConfig, SSMeshOBJSubsetData subset) {
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
			
		private void _renderSendVBOLines(ref SSRenderConfig renderConfig, SSMeshOBJSubsetData subset) {
			// TODO: this currently has classic problems with z-fighting between the model and the wireframe
			//     it is customary to "bump" the wireframe slightly towards the camera to prevent this. 
            GL.LineWidth(1.5f);
            GL.Color4(0.8f, 0.5f, 0.5f, 0.5f);		
            subset.ibo.DrawElements(PrimitiveType.Lines, null);
		}

		private void _renderSendLines(ref SSRenderConfig renderConfig, SSMeshOBJSubsetData subset) {
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
					_renderSetupGLSL (ref renderConfig, subset);
					if (renderConfig.useVBO && renderConfig.BaseShader != null) {
						_renderSendVBOTriangles (ref renderConfig, subset);
					} else {
						_renderSendTriangles (ref renderConfig, subset);
					}
			
				}

				if (renderConfig.drawWireframeMode == WireframeMode.GL_Lines) {
					_renderSetupWireframe ();
					if (renderConfig.useVBO && renderConfig.BaseShader != null) {
						_renderSendVBOLines (ref renderConfig, subset);
					} else {
						_renderSendLines (ref renderConfig, subset);
					}
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

			subsetData.wireframe_indicies = OpenTKHelper.generateLineIndicies (subsetData.indicies);

			subsetData.vbo = new SSVertexBuffer<SSVertex_PosNormDiffTex1>(subsetData.vertices);
			subsetData.ibo = new SSIndexBuffer(subsetData.indicies, subsetData.vbo);		
			subsetData.ibo_wireframe = new SSIndexBuffer (subsetData.wireframe_indicies, subsetData.vbo);

            return subsetData;
        }
#endregion
    }
}