// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
// Released to the public domain. 

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using SimpleScene;
using System.Drawing;
using System.Collections.Generic;

namespace SimpleScene
{
    public class SSShadowMapShaderProgram : SSShaderProgram
    {
        private static string c_ctx = "./Shaders/Shadowmap";

        #region Shaders
        private readonly SSShader m_vertexShader;  
        private readonly SSShader m_fragmentShader;
        private readonly SSShader m_geometryShader;
        #endregion

        #region Uniform Locations
        private readonly int u_numShadowMaps;
        private readonly int u_shadowMapVPs;
        private readonly int u_objectWorldTransform;
        private readonly int u_shadowMapSplits;
        #endregion

        #region Uniform Modifiers
        public Matrix4 UniObjectWorldTransform {
            // pass object world transform matrix for use in shadowmap lookup
            set { assertActive(); GL.UniformMatrix4(u_objectWorldTransform, false, ref value); }
        }

        public void UpdateShadowMapMVPs(SSLight light) {
            // pass update mvp matrices for shadowmap lookup
            assertActive();
            int count = 0;
            if (light.ShadowMap != null) {
                Matrix4[] temp = light.ShadowMap.ViewProjectionMatrices;
                for (int s = 0; s < SSShadowMap.c_numberOfSplits; ++s) {
                    GL.UniformMatrix4(u_shadowMapVPs + s, false, ref temp[s]);
                }
                ++count;
            }
        }

        public void UpdateViewSplits(float[] splits) {
            assertActive();
            for (int s = 0; s < splits.Length; ++s) {
                GL.Uniform1(u_shadowMapSplits + s, splits[s]);
            }
        }
        #endregion

        public SSShadowMapShaderProgram()
        {
            if (GL.GetString(StringName.Extensions).ToLower().Contains("gl_ext_gpu_shader4")) {
                m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_ctx, "shadowmap_vertex.glsl");
                attach(m_vertexShader);

                m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_ctx, "shadowmap_fragment.glsl");
                attach(m_fragmentShader);

                m_geometryShader = SSAssetManager.GetInstance<SSGeometryShader>(c_ctx, "shadowmap_geometry.glsl");
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryInputTypeExt, (int)All.Triangles);
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryOutputTypeExt, (int)All.TriangleStrip);
                GL.Ext.ProgramParameter (m_programID, ExtGeometryShader4.GeometryVerticesOutExt, 3);
                attach(m_geometryShader);
            } else {
                throw new NotImplementedException ();
            }
            link();
            Activate();

            u_shadowMapVPs = getUniLoc("shadowMapVPs");
            u_objectWorldTransform = getUniLoc("objWorldTransform");
            u_numShadowMaps = getUniLoc("numShadowMaps");
            u_shadowMapSplits = getUniLoc("shadowMapSplits");

            GL.Uniform1(u_numShadowMaps, SSShadowMap.c_numberOfSplits);

            checkErrors();
        }
    }
}