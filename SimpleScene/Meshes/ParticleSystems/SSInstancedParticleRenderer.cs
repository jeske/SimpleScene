#define DRAW_USING_MAIN_SHADER

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSInstancedParticleRenderer : SSObject
    {
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
        public bool BillboardingEnabled = true;

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

            // update buffers
            m_posBuffer.UpdateBufferData(m_ps.Positions);
            m_masterScaleBuffer.UpdateBufferData(m_ps.MasterScales);
            m_componentScaleBuffer.UpdateBufferData(m_ps.ComponentScales);
            m_colorBuffer.UpdateBufferData(m_ps.Colors);

            Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMat;
            if (BillboardingEnabled) {
                modelView = OpenTKHelper.BillboardMatrix(ref modelView);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelView);
            }

            if (AlphaBlendingEnabled) {
                GL.Enable(EnableCap.AlphaTest);
                GL.Enable(EnableCap.Blend);
                GL.AlphaFunc(AlphaFunction.Greater, 0.2f);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Disable(EnableCap.Lighting);

                m_ps.SortByDepth(ref modelView);
            }

            #if DRAW_USING_MAIN_SHADER
            // draw using the main shader
            // TODO: debug with bump mapped lighting mode
            renderConfig.MainShader.Activate();
            renderConfig.MainShader.UniAmbTexEnabled = true;
            renderConfig.MainShader.UniDiffTexEnabled = false;
            renderConfig.MainShader.UniSpecTexEnabled = false;
            renderConfig.MainShader.UniBumpTexEnabled = false;
            GL.ActiveTexture(TextureUnit.Texture2);
            #else
            // TODO: Try drawing without shader
            SSShaderProgram.DeactivateAll();
            GL.ActiveTexture(TextureUnit.Texture0);
            #endif
            if (m_texture != null) {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);
            } else {
                GL.Disable(EnableCap.Texture2D);
            }

            //s_billboardVbo.DrawArrays(PrimitiveType.Triangles);
            //return;

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
            renderConfig.MainShader.UniInstanceDrawEnabled = true;
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

