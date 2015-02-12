#define DRAW_USING_MAIN_SHADER

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSInstancedParticleRenderer : SSObject
    {
        public enum BillboardingType { None, Instanced, Global };

        public const BufferUsageHint c_usageHint = BufferUsageHint.StreamDraw;

        private static readonly SSVertex_PosTex1[] c_billboardVertices = {
            // CCW quad; no indexing
            new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 0f),
            new SSVertex_PosTex1(+.5f, -.5f, 0f, 1f, 0f),
            new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 1f),

            new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 0f),
            new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 1f),
            new SSVertex_PosTex1(-.5f, +.5f, 0f, 0f, 1f),
        };
        protected static readonly SSVertexBuffer<SSVertex_PosTex1> s_billboardVbo;

        public bool AlphaBlendingEnabled = true;
        public bool DepthMaskEnabled = false;
        public BillboardingType Billboarding = BillboardingType.Instanced;

        protected SSParticleSystem m_ps;
        protected SSIndexBuffer m_ibo;
        protected SSAttributeBuffer<SSAttributePos> m_posBuffer;
        protected SSAttributeBuffer<SSAttributeMasterScale> m_masterScaleBuffer;
        protected SSAttributeBuffer<SSAttributeComponentScale> m_componentScaleBuffer;
        protected SSAttributeBuffer<SSAttributeColor> m_colorBuffer;
        protected SSTexture m_texture;

        static SSInstancedParticleRenderer()
        {
            s_billboardVbo = new SSVertexBuffer<SSVertex_PosTex1> (c_billboardVertices);
        }

        public SSInstancedParticleRenderer (SSParticleSystem ps, SSTexture texture)
        {
            m_ps = ps;
            m_texture = texture;
            m_posBuffer = new SSAttributeBuffer<SSAttributePos> (c_usageHint);
            m_masterScaleBuffer = new SSAttributeBuffer<SSAttributeMasterScale> (c_usageHint);
            m_componentScaleBuffer = new SSAttributeBuffer<SSAttributeComponentScale> (c_usageHint);
            m_colorBuffer = new SSAttributeBuffer<SSAttributeColor> (c_usageHint);

            // test
            this.boundingSphere = new SSObjectSphere (10f);
        }

        public override void Render (ref SSRenderConfig renderConfig)
        {
            if (m_ps.ActiveBlockLength <= 0) return;

            base.Render(ref renderConfig);

            Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMat;
            if (Billboarding == BillboardingType.Global) {
                // Setup "global" billboarding. (entire particle system is rendered as a camera-facing
                // billboard and will show the same position of particles from all angles)
                modelView = OpenTKHelper.BillboardMatrix(ref modelView);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelView);
            }

            // Saving and restoring state is not ideal.
            // We have to choose to either sort things by "material" properties (not implemented yet)
            // -or- assume some state defaults for each scene (currently done). The latter poses
            // difficulties if it is desired to render a particle system in the scene with the rest of the
            // objects, and what you see below is a workaround to maintain a "main" depth mask state for 
            // the scene.
            bool prevDepthMask = GL.GetBoolean(GetPName.DepthWritemask);
            if (prevDepthMask != DepthMaskEnabled) {
                GL.DepthMask(DepthMaskEnabled);
            }

            if (AlphaBlendingEnabled) {
                //GL.Enable(EnableCap.AlphaTest);
                //GL.AlphaFunc(AlphaFunction.Greater, 0.1f);

                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Disable(EnableCap.Lighting);

                // Must be called before updating buffers
                m_ps.SortByDepth(ref modelView);
            }

            // update buffers
            m_posBuffer.UpdateBufferData(m_ps.Positions);
            m_masterScaleBuffer.UpdateBufferData(m_ps.MasterScales);
            m_componentScaleBuffer.UpdateBufferData(m_ps.ComponentScales);
            m_colorBuffer.UpdateBufferData(m_ps.Colors);

            #if DRAW_USING_MAIN_SHADER
            // draw using the main shader
            // TODO: debug with bump mapped lighting mode
            renderConfig.MainShader.Activate();
            renderConfig.MainShader.UniAmbTexEnabled = true;
            renderConfig.MainShader.UniDiffTexEnabled = false;
            renderConfig.MainShader.UniSpecTexEnabled = false;
            renderConfig.MainShader.UniBumpTexEnabled = false;
            // texture slot setup
            GL.ActiveTexture(TextureUnit.Texture2);
            #else
            // TODO: Try drawing without shader
            SSShaderProgram.DeactivateAll();
            GL.ActiveTexture(TextureUnit.Texture0);
            #endif

            // texture binding setup
            if (m_texture != null) {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);
            } else {
                GL.Disable(EnableCap.Texture2D);
            }

            // prepare attribute arrays for draw
            #if DRAW_USING_MAIN_SHADER
            SSMainShaderProgram mainShader = renderConfig.MainShader;
            prepareAttribute(m_posBuffer, mainShader.AttrInstancePos, m_ps.Positions);
            prepareAttribute(m_masterScaleBuffer, mainShader.AttrInstanceMasterScale, m_ps.MasterScales);
            prepareAttribute(m_componentScaleBuffer, mainShader.AttrInstanceComponentScale, m_ps.ComponentScales);
            prepareAttribute(m_colorBuffer, mainShader.AttrInstanceColor, m_ps.Colors);
            #else
            throw new NotImplementedException();
            #endif

            // do the draw
            // TODO configure instance billboarding
            bool instanceBB = (Billboarding == BillboardingType.Instanced);
            renderConfig.MainShader.UniInstanceDrawEnabled = true;
            renderConfig.MainShader.UniInstanceBillboardingEnabled = instanceBB;
            s_billboardVbo.DrawInstanced(PrimitiveType.Triangles, m_ps.ActiveBlockLength);
            renderConfig.MainShader.UniInstanceDrawEnabled = false;

            #if DRAW_USING_MAIN_SHADER
            // undo attribute state
            m_posBuffer.DisableAttribute(mainShader.AttrInstancePos);
            m_masterScaleBuffer.DisableAttribute(mainShader.AttrInstanceMasterScale);
            m_componentScaleBuffer.DisableAttribute(mainShader.AttrInstanceComponentScale);
            m_colorBuffer.DisableAttribute(mainShader.AttrInstanceColor);
            #else
            throw new NotImplementedException();
            #endif

            // Restore previous depth mask. This is not ideal; see setup before the draw
            if (prevDepthMask != DepthMaskEnabled) {
                GL.DepthMask(prevDepthMask);
            }

            //this.boundingSphere.Render(ref renderConfig);
        }

        void prepareAttribute<AB, A>(AB attrBuff, int attrLoc, A[] array) 
            where A : struct, ISSAttributeLayout 
            where AB : SSAttributeBuffer<A>
        {
            int numActive = m_ps.ActiveBlockLength;
            int numInstancesPerValue = array.Length < numActive ? numActive : 1;
            attrBuff.PrepareAttribute(attrLoc, numInstancesPerValue);
        }
    }
}

