// Copyright(C) David W. Jeske, 2013
// Released to the public domain.

using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

using SimpleScene.Util3d;


namespace SimpleScene
{
    public class SSMesh_wfOBJ : SSAbstractMesh, ISSInstancable {
 
		protected List<SSMeshOBJSubsetData> geometrySubsets = new List<SSMeshOBJSubsetData>();
		SSAssetManager.Context ctx;
		public readonly string srcFilename;
		
	    public class SSMeshOBJSubsetData {
			public SSTextureMaterial TextureMaterial = null;

            public readonly SSIndexedMesh<SSVertex_PosNormTexDiff> triangleMesh;
            public readonly SSIndexedMesh<SSVertex_PosNormTexDiff> wireframeMesh;

            public SSMeshOBJSubsetData(SSVertex_PosNormTexDiff[] vertices, 
                                       UInt16[] indices, UInt16[] wireframeIndices)
            {
                SSVertexBuffer<SSVertex_PosNormTexDiff> vbo
                    = new SSVertexBuffer<SSVertex_PosNormTexDiff>(vertices, BufferUsageHint.StaticDraw);
                SSIndexBuffer triIbo = new SSIndexBuffer(indices, vbo, BufferUsageHint.StaticDraw);
                SSIndexBuffer wireframeIbo = new SSIndexBuffer(wireframeIndices, vbo, BufferUsageHint.StaticDraw);
                this.triangleMesh = new SSIndexedMesh<SSVertex_PosNormTexDiff>(vbo, triIbo);
                this.wireframeMesh = new SSIndexedMesh<SSVertex_PosNormTexDiff>(vbo, wireframeIbo);
            }
		}

		public override string ToString ()
		{
			return string.Format ("[SSMesh_FromOBJ:{0}]", this.srcFilename);
		}

#region Constructor
        public SSMesh_wfOBJ(SSAssetManager.Context ctx, string filename) {
            this.srcFilename = filename;            
            this.ctx = ctx;


            Console.WriteLine("SSMesh_wfOBJ: loading wff {0}",filename);
			WavefrontObjLoader wff_data = new WavefrontObjLoader(ctx, filename);

			Console.WriteLine("wff vertex count = {0}",wff_data.positions.Count);
			Console.WriteLine("wff face count = {0}",wff_data.numFaces);

            _loadData(ctx, wff_data);
        }
#endregion

		public void drawInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType)
		{
			base.renderMesh (renderConfig);
			foreach (SSMeshOBJSubsetData subset in this.geometrySubsets) {
				_renderSetupGLSL(renderConfig, renderConfig.instanceShader, subset);
                subset.triangleMesh.drawInstanced (renderConfig, instanceCount, primType);
			}
		}

        public void drawSingle(SSRenderConfig renderConfig, PrimitiveType primType)
        {
            renderMesh(renderConfig);
        }

		private void _renderSetupGLSL(SSRenderConfig renderConfig, SSMainShaderProgram shaderPgm, SSMeshOBJSubsetData subset) {
			// Step 1: setup GL rendering modes...

			// GL.Enable(EnableCap.Lighting);

			// GL.Enable(EnableCap.Blend);
			// GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			// GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

			// Step 2: setup our material mode and paramaters...

            if (renderConfig.drawingShadowMap) { 
                // assume SSObject.Render has setup our materials properly for the shadowmap Pass               
                // TODO: find a better way to do this! 
                return;
            }

 			if (shaderPgm == null) {
				SSShaderProgram.DeactivateAll ();

				// fixed function single-texture
				if (subset.TextureMaterial.diffuseTex != null) {
					GL.ActiveTexture(TextureUnit.Texture0);
					GL.Enable(EnableCap.Texture2D);
					GL.BindTexture(TextureTarget.Texture2D, subset.TextureMaterial.diffuseTex.TextureID);
				}
			} else {
                shaderPgm.Activate();
				shaderPgm.SetupTextures(subset.TextureMaterial);		
			}
		}

		private void _renderSetupWireframe() {
			SSShaderProgram.DeactivateAll ();
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.Lighting);
		}

		public override float boundingSphereRadius {
			get {
				float maxRadSq = 0f;
				Vector3 maxCoponent = new Vector3 (0f);
				foreach (var subset in geometrySubsets) {
                    foreach (var vtx in subset.triangleMesh.lastAssignedVertices) {
						maxRadSq = Math.Max (maxRadSq, vtx.Position.LengthSquared);
					}
				}
				return (float)Math.Sqrt (maxRadSq);
			}
		}

		public override Vector3 boundingSphereCenter {
			get { return Vector3.Zero; }
		}

        public override bool preciseIntersect (ref SSRay localRay, out float localRayContact)
        {
            localRayContact = float.PositiveInfinity;

            foreach (var subset in geometrySubsets) {
                float contact;
                if (subset.triangleMesh.preciseIntersect(ref localRay, out contact)
                    && contact < localRayContact) {
                    localRayContact = contact;
                }
            }
            return localRayContact < float.PositiveInfinity;
        }

        #if false
		public override bool traverseTriangles<T>(T state, traverseFn<T> fn) {
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
        #endif



		private void _renderSendVBOTriangles(SSRenderConfig renderConfig, SSMeshOBJSubsetData subset) {
            subset.triangleMesh.drawSingle(renderConfig, PrimitiveType.Triangles);
		}

		private void _renderSendTriangles(SSMeshOBJSubsetData subset) {
			// Step 3: draw faces.. here we use the "old school" manual method of drawing

			//         note: programs written for modern OpenGL & D3D don't do this!
			//               instead, they hand the vertex-buffer and index-buffer to the
			//               GPU and let it do this..		

			GL.Begin(PrimitiveType.Triangles);
            foreach(var idx in subset.triangleMesh.lastAssignedIndices) {
                var vertex = subset.triangleMesh.lastAssignedVertices[idx];  // retrieve the vertex

				// draw the vertex..
				GL.Color3(System.Drawing.Color.FromArgb(vertex.DiffuseColor));
				GL.TexCoord2(vertex.TexCoord.X, vertex.TexCoord.Y);
				GL.Normal3(vertex.Normal);
				GL.Vertex3(vertex.Position);
			}
			GL.End();
		}
			
		private void _renderSendVBOLines(SSRenderConfig renderConfig, SSMeshOBJSubsetData subset) {
			// TODO: this currently has classic problems with z-fighting between the model and the wireframe
			//     it is customary to "bump" the wireframe slightly towards the camera to prevent this. 
            GL.LineWidth(1.5f);
            GL.Color4(0.8f, 0.5f, 0.5f, 0.5f);		
            subset.wireframeMesh.drawSingle(renderConfig, PrimitiveType.Lines);
		}

		private void _renderSendLines(SSMeshOBJSubsetData subset) {
			// Step 3: draw faces.. here we use the "old school" manual method of drawing

			//         note: programs written for modern OpenGL & D3D don't do this!
			//               instead, they hand the vertex-buffer and index-buffer to the
			//               GPU and let it do this..


            var indices = subset.wireframeMesh.lastAssignedIndices;
            for(int i=0; i<indices.Length; i+=3) {
                var v1 = subset.wireframeMesh.lastAssignedVertices [indices[i]];
                var v2 = subset.wireframeMesh.lastAssignedVertices [indices[i+1]];
                var v3 = subset.wireframeMesh.lastAssignedVertices [indices[i+2]];

				// draw the vertex..
				GL.Color3(System.Drawing.Color.FromArgb(v1.DiffuseColor));

				GL.Begin(PrimitiveType.LineLoop);
				GL.Vertex3 (v1.Position);
				GL.Vertex3 (v2.Position);
				GL.Vertex3 (v3.Position);
				GL.End();
			}

		}


		public override void renderMesh(SSRenderConfig renderConfig) {
			base.renderMesh (renderConfig);
            foreach (SSMeshOBJSubsetData subset in this.geometrySubsets) {
                if (renderConfig.drawingShadowMap) {
                    _renderSendVBOTriangles(renderConfig, subset);
                } else {
                    if (renderConfig.drawGLSL) {
						_renderSetupGLSL(renderConfig, renderConfig.mainShader, subset);
                        if (renderConfig.useVBO && renderConfig.mainShader != null) {
                            _renderSendVBOTriangles(renderConfig, subset);
                        } else {
                            _renderSendTriangles(subset);
                        }
                    }
                    if (renderConfig.drawWireframeMode == WireframeMode.GL_Lines) {
                        _renderSetupWireframe();
                        if (renderConfig.useVBO && renderConfig.mainShader != null) {
                            _renderSendVBOLines(renderConfig, subset);
                        } else {
                            _renderSendLines(subset);
                        }
                    }
                }
            }
		}

#region Load Data
        private void _loadData(SSAssetManager.Context ctx ,WavefrontObjLoader m) {
            foreach (var srcmat in m.materials) {
                if (srcmat.faces.Count != 0) {
                    this.geometrySubsets.Add(_loadMaterialSubset(ctx, m, srcmat));
                }
            }
        }
        
		private SSMeshOBJSubsetData _loadMaterialSubset(SSAssetManager.Context ctx, WavefrontObjLoader wff, 
														WavefrontObjLoader.MaterialInfoWithFaces objMatSubset) 
        {
            // generate renderable geometry data...
            SSVertex_PosNormTexDiff[] vertices;
            UInt16[] triIndices, wireframeIndices;
            VertexSoup_VertexFormatBinder.generateDrawIndexBuffer(
                wff, out triIndices, out vertices);
            wireframeIndices = OpenTKHelper.generateLineIndicies (triIndices);
            SSMeshOBJSubsetData subsetData = new SSMeshOBJSubsetData(
                vertices, triIndices, wireframeIndices);

            // setup the material...            
            // load and link every texture present 
			subsetData.TextureMaterial = new SSTextureMaterial();
            if (objMatSubset.mtl.diffuseTextureResourceName != null) {
				subsetData.TextureMaterial.diffuseTex = SSAssetManager.GetInstance<SSTexture>(ctx, objMatSubset.mtl.diffuseTextureResourceName);
            }
            if (objMatSubset.mtl.ambientTextureResourceName != null) {
				subsetData.TextureMaterial.ambientTex = SSAssetManager.GetInstance<SSTexture>(ctx, objMatSubset.mtl.ambientTextureResourceName);
            } 
			if (objMatSubset.mtl.bumpTextureResourceName != null) {
				subsetData.TextureMaterial.bumpMapTex = SSAssetManager.GetInstance<SSTexture>(ctx, objMatSubset.mtl.bumpTextureResourceName);
            }
			if (objMatSubset.mtl.specularTextureResourceName != null) {
				subsetData.TextureMaterial.specularTex = SSAssetManager.GetInstance<SSTexture>(ctx, objMatSubset.mtl.specularTextureResourceName);
            }
            return subsetData;
       }
#endregion
    }
}