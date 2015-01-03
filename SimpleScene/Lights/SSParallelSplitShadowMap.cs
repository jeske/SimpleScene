using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;

namespace SimpleScene
{
    public class SSParallelSplitShadowMap : SSShadowMapBase
    {
        // http://http.developer.nvidia.com/GPUGems3/gpugems3_ch10.html

        public const int c_numberOfSplits = 4;
        private const int c_texWidth = 1024;
        private const int c_texHeight = 1024;

        private Matrix4[] m_viewProjMatrices = new Matrix4[c_numberOfSplits];
        private Matrix4[] m_viewProjBiasMatrices = new Matrix4[c_numberOfSplits];
        private float[] m_viewSplits = new float[c_numberOfSplits];

        public SSParallelSplitShadowMap(TextureUnit unit) 
            : base(unit)        
        { }

        public override void PrepareForRender(SSRenderConfig renderConfig, 
                                              List<SSObject> objects,
                                              float fov, float aspect, float nearZ, float farZ) {
            base.PrepareForRenderBase(renderConfig, objects);

            Util3d.Projections.ParallelShadowmapProjections(
                objects, m_light,
                renderConfig.invCameraViewMat,
                renderConfig.projectionMatrix,
                fov, aspect, nearZ, farZ,
                c_numberOfSplits, m_viewProjMatrices, m_viewSplits);

            // update info for the regular draw pass later
            for (int i = 0; i < c_numberOfSplits; ++i) {
                m_viewProjBiasMatrices[i] = m_viewProjMatrices[i] * c_biasMatrix;
            }
            renderConfig.MainShader.Activate();
            renderConfig.MainShader.NumShadowMaps = c_numberOfSplits;
            renderConfig.MainShader.UpdateShadowMapBiasVPs(m_viewProjBiasMatrices);
            renderConfig.MainShader.UpdatePssmSplits(m_viewSplits);

            // setup for render shadowmap pass
            renderConfig.PssmShader.Activate();
            renderConfig.PssmShader.UpdateShadowMapVPs(m_viewProjMatrices);
        }

        protected override int texWidth() { return c_texWidth; }
        protected override int texHeight() { return c_texHeight; }
    }
}

