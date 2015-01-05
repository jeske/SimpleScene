using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;

namespace SimpleScene
{
    public class SSSimpleShadowMap : SSShadowMapBase
    {
        // http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-16-shadow-mapping/

        // TODO: update shadowmap mvp when position of SSLight changes
        private const int c_texWidth = 1024;
        private const int c_texHeight = 1024;

        private Matrix4 m_projMatrix;
        private Matrix4 m_viewMatrix;

        public SSSimpleShadowMap(TextureUnit unit)
            : base(unit)
        { }


        public override void PrepareForRender (
            SSRenderConfig renderConfig,
            List<SSObject> objects,
            float fov, float aspect, float nearZ, float farZ) {
            base.PrepareForRenderBase(renderConfig, objects);

			// dynamically compute light frustum
            Util3d.Projections.SimpleShadowmapProjection(
                objects, m_light,
                renderConfig.invCameraViewMat, renderConfig.projectionMatrix,
                out m_viewMatrix, out m_projMatrix);

            // update info for the regular draw pass later
            Matrix4[] vp = { m_viewMatrix * m_projMatrix * c_biasMatrix };
            renderConfig.MainShader.Activate();
            renderConfig.MainShader.UniNumShadowMaps = 1;
            renderConfig.MainShader.UpdateShadowMapBiasVPs(vp);

            // setup for render shadowmap pass
            renderConfig.projectionMatrix = m_projMatrix;
            renderConfig.invCameraViewMat = m_viewMatrix;
            SSShaderProgram.DeactivateAll();
		}

        protected override int texWidth() { return c_texWidth; }
        protected override int texHeight() { return c_texHeight; }
    }
}

