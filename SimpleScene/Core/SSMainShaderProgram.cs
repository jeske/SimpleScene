// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
// Released to the public domain. 


// shader doc references..
//
// http://fabiensanglard.net/bumpMapping/index.php
// http://www.geeks3d.com/20091013/shader-library-phong-shader-with-multiple-lights-glsl/
// http://en.wikibooks.org/wiki/GLSL_Programming/GLUT/Specular_Highlights

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using SimpleScene;
using System.Drawing;
using System.Collections.Generic;

namespace SimpleScene
{
	public class SSMainShaderProgram : SSShaderProgram
	{
        private static string c_ctx = "./Shaders";

        #region Shaders
        private readonly SSShader m_vertexShader;  
        private readonly SSShader m_fragmentShader;
        private readonly SSShader m_geometryShader;
        #endregion

        #region Uniform Locations
        private readonly int u_winScale;
        private readonly int u_animateSecondsOffset;
        private readonly int u_showWireframes;

        private readonly int u_diffTexEnabled;
        private readonly int u_specTexEnabled;
        private readonly int u_ambiTexEnabled;
        private readonly int u_bumpTexEnabled;

        private readonly int u_numShadowMaps;
        private readonly int u_shadowMapTexture;
        private readonly int[] u_shadowMapVPs = new int[SSParallelSplitShadowMap.c_numberOfSplits];
        private readonly int u_objectWorldTransform;
        private readonly int u_shadowMapViewSplits;
        private readonly int u_poissonSamplingEnabled;
        private readonly int u_numPoissonSamples;
		#endregion

        #region Uniform Modifiers

		// I don't like the way this makes rendering-state uniform sets look like
		// normal variables.. I might undo this.. - jeske

        public bool UniDiffTexEnabled {
            set { assertActive (); GL.Uniform1 (u_diffTexEnabled, value ? 1 : 0); }
        }

        public bool UniSpecTexEnabled {
            set { assertActive (); GL.Uniform1 (u_specTexEnabled, value ? 1 : 0); }
        }

        public bool UniAmbTexEnabled {
            set { assertActive (); GL.Uniform1 (u_ambiTexEnabled, value ? 1 : 0); }
        }

        public bool UniBumpTexEnabled {
            set { assertActive (); GL.Uniform1 (u_bumpTexEnabled, value ? 1 : 0); }
        }

        public float UniAnimateSecondsOffset {
            set { assertActive (); GL.Uniform1 (u_animateSecondsOffset, value); }
        }

        public bool UniShowWireframes {
            set { assertActive (); GL.Uniform1 (u_showWireframes, value ? 1 : 0); }
        }

        public Rectangle UniWinScale {
            set { assertActive (); GL.Uniform2 (u_winScale, (float)value.Width, (float)value.Height); }
        }

        public Matrix4 UniObjectWorldTransform {
            // pass object world transform matrix for use in shadowmap lookup
            set { assertActive(); GL.UniformMatrix4(u_objectWorldTransform, false, ref value); }
        }

        public int UniNumShadowMaps {
            set { assertActive(); GL.Uniform1(u_numShadowMaps, value); }
        }

        public bool UniPoissonSamplingEnabled {
            set { assertActive(); GL.Uniform1(u_poissonSamplingEnabled, value ? 1 : 0); }
        }

        public int UniNumPoissonSamples {
            set { assertActive(); GL.Uniform1(u_numPoissonSamples, value); }
        }


        public void SetupShadowMap(List<SSLight> lights) {
            // setup number of shadowmaps, textures
			int count=0;
            assertActive();
            foreach (var light in lights) {
                if (light.ShadowMap != null) {
                    // TODO: multiple lights with shadowmaps?
                    if (count >= SSShadowMapBase.c_maxNumberOfShadowMaps) {
                        throw new Exception ("Unsupported number of shadow maps: " + count);
                    }
                    GL.Uniform1(u_shadowMapTexture, 
                                (int)light.ShadowMap.TextureUnit - (int)TextureUnit.Texture0);
					count ++;
                }
            }
        }

        public void UpdateShadowMapBiasVPs(Matrix4[] vps) {
            assertActive();
            for (int i = 0; i < vps.Length; ++i) {
                // update shadowmap view-projection-crop-bias matrices for shadowmap lookup
                GL.UniformMatrix4(u_shadowMapVPs [i], false, ref vps [i]);
            }
        }

        public void UpdatePssmSplits(float[] splits) {
            assertActive();
            GL.Uniform4(u_shadowMapViewSplits, splits [0], splits [1], splits [2], splits [3]);
        }
        #endregion

		public SSMainShaderProgram ()
		{
			// we use this method of detecting the extension because we are in a GL2.2 context

			if (GL.GetString(StringName.Extensions).ToLower().Contains("gl_ext_gpu_shader4")) {

                m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_ctx, "ss4_vertex.glsl");
                attach(m_vertexShader);

                m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_ctx, "ss4_fragment.glsl");
                attach(m_fragmentShader);

                m_geometryShader = SSAssetManager.GetInstance<SSGeometryShader>(c_ctx, "ss4_geometry.glsl");		
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryVerticesOutExt, 3);
                attach(m_geometryShader);

			} else {
                m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_ctx, "ss1_vertex.glsl");
                attach(m_vertexShader);

                m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_ctx, "ss1_fragment.glsl");
                attach(m_fragmentShader);
			}
            link();
			// shader is initialized now...	
			Activate ();

            // reused uniform locations
            u_diffTexEnabled = getUniLoc("diffTexEnabled");
            u_specTexEnabled = getUniLoc("specTexEnabled");
            u_ambiTexEnabled = getUniLoc("ambiTexEnabled");
            u_bumpTexEnabled = getUniLoc("bumpTexEnabled");
            u_animateSecondsOffset = getUniLoc("animateSecondsOffset");
            u_winScale = getUniLoc("WIN_SCALE");
            u_showWireframes = getUniLoc("showWireframes");
            u_numShadowMaps = getUniLoc("numShadowMaps");
            u_shadowMapTexture = getUniLoc("shadowMapTexture");
            u_poissonSamplingEnabled = getUniLoc("poissonSamplingEnabled");
            u_numPoissonSamples = getUniLoc("numPoissonSamples");
            u_objectWorldTransform = getUniLoc("objWorldTransform");
            u_shadowMapViewSplits = getUniLoc("shadowMapViewSplits");

            // TODO: debug passing things through arrays
            for (int i = 0; i < SSParallelSplitShadowMap.c_numberOfSplits; ++i) {
                var str = "shadowMapVPs" + i;
                u_shadowMapVPs[i] = getUniLoc(str);
            }

            UniShowWireframes = false;
            UniAnimateSecondsOffset = 0.0f;
            UniNumShadowMaps = 0;
            UniPoissonSamplingEnabled = true;
            UniNumPoissonSamples = 4;
            
            // uniform locations for texture setup only
            int GLun_diffTex = getUniLoc("diffTex");
            int GLun_specTex = getUniLoc("specTex");
            int GLun_ambiTex = getUniLoc("ambiTex");
            int GLun_bumpTex = getUniLoc("bumpTex");

			// bind shader uniform variable handles to GL texture-unit numbers
            GL.Uniform1(GLun_diffTex, 0); // Texture.Texture0
            GL.Uniform1(GLun_specTex, 1); // Texture.Texture1
            GL.Uniform1(GLun_ambiTex, 2); // Texture.Texture2
            GL.Uniform1(GLun_bumpTex, 3); // Texture.Texture3

            checkErrors();
		}
	}
}

