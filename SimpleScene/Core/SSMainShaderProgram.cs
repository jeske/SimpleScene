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
        private readonly int u_shadowMapTextures;
        private readonly int u_shadowMapMVPs;
        private readonly int u_objectWorldTransform;
		#endregion

        #region Uniform Modifiers
        public bool DiffTexEnabled {
            set { assertActive (); GL.Uniform1 (u_diffTexEnabled, value ? 1 : 0); }
        }

        public bool SpecTexEnabled {
            set { assertActive (); GL.Uniform1 (u_specTexEnabled, value ? 1 : 0); }
        }

        public bool AmbTexEnabled {
            set { assertActive (); GL.Uniform1 (u_ambiTexEnabled, value ? 1 : 0); }
        }

        public bool BumpTexEnabled {
            set { assertActive (); GL.Uniform1 (u_bumpTexEnabled, value ? 1 : 0); }
        }

        public float AnimateSecondsOffset {
            set { assertActive (); GL.Uniform1 (u_animateSecondsOffset, value); }
        }

        public bool ShowWireframes {
            set { assertActive (); GL.Uniform1 (u_showWireframes, value ? 1 : 0); }
        }

        public Rectangle WinScale {
            set { assertActive (); GL.Uniform2 (u_winScale, (float)value.Width, (float)value.Height); }
        }

        public Matrix4 ObjectWorldTransform {
            set { assertActive(); GL.UniformMatrix4(u_objectWorldTransform, false, ref value); }
        }

        public List<SSShadowMap> ShadowMaps {
            set {
                assertActive();
                if (value.Count > SSShadowMap.c_maxNumberOfShadowMaps) {
                    throw new Exception ("Unsupported number of shadow maps: " 
                                         + value.Count);
                }
                GL.Uniform1(u_numShadowMaps, value.Count);
                for (int i = 0; i < value.Count; ++i) {
                    SSShadowMap current = value [i];
                    Matrix4 currMVP = current.DepthBiasMVP;
                    GL.UniformMatrix4(u_shadowMapMVPs + i, false, ref currMVP);
                    GL.Uniform1(u_shadowMapTextures + i, current.TextureID);
                }
            }
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
                attach(m_geometryShader);
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
            u_shadowMapTextures = getUniLoc("shadowMapTextures");
            u_shadowMapMVPs = getUniLoc("shadowMapMVPs");
            u_objectWorldTransform = getUniLoc("objWorldTransform");

            ShowWireframes = false;
            AnimateSecondsOffset = 0.0f;
            
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

