//#define DRAW_USING_MAIN_SHADER

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSInstancedParticleRenderer : SSObject
    {
        // TODO Draw any ibo/vbo mesh

        public enum BillboardingType { None, Instanced, Global };

        public const BufferUsageHint c_usageHint = BufferUsageHint.StreamDraw;

        private static readonly SSVertex_PosTex1[] c_billboardVertices = {
            // CCW quad; no indexing
            new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
            new SSVertex_PosTex1(+.5f, -.5f, 0f, 1f, 1f),
            new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),

            new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
            new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),
            new SSVertex_PosTex1(-.5f, +.5f, 0f, 0f, 0f),
        };
        protected static readonly SSVertexBuffer<SSVertex_PosTex1> s_billboardVbo;

        public bool AlphaBlendingEnabled = true;
        public BillboardingType Billboarding = BillboardingType.Instanced;

        protected SSParticleSystem m_ps;

        protected SSAttributeBuffer<SSAttributePos> m_posBuffer;
        protected SSAttributeBuffer<SSAttributeMasterScale> m_masterScaleBuffer;
        protected SSAttributeBuffer<SSAttributeComponentScale> m_componentScaleBuffer;
        protected SSAttributeBuffer<SSAttributeColor> m_colorBuffer;

        protected SSAttributeBuffer<SSAttributeByte> m_spriteIndexBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteOffsetUBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteOffsetVBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteSizeUBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> m_spriteSizeVBuffer;
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

            m_spriteIndexBuffer = new SSAttributeBuffer<SSAttributeByte> (c_usageHint);
            m_spriteOffsetUBuffer = new SSAttributeBuffer<SSAttributeFloat> (c_usageHint);
            m_spriteOffsetVBuffer = new SSAttributeBuffer<SSAttributeFloat> (c_usageHint);
            m_spriteSizeUBuffer = new SSAttributeBuffer<SSAttributeFloat> (c_usageHint);
            m_spriteSizeVBuffer = new SSAttributeBuffer<SSAttributeFloat> (c_usageHint);

            // test
            this.boundingSphere = new SSObjectSphere (10f);
        }

        public override void Render (ref SSRenderConfig renderConfig)
        {
            if (m_ps.ActiveBlockLength <= 0) return;

            base.Render(ref renderConfig);

            Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMat;
            // Update buffers early for better streaming
            if (AlphaBlendingEnabled) {
                // Must be called before updating buffers
                m_ps.SortByDepth(ref modelView);
            }
            m_posBuffer.UpdateBufferData(m_ps.Positions);
            m_masterScaleBuffer.UpdateBufferData(m_ps.MasterScales);
            m_componentScaleBuffer.UpdateBufferData(m_ps.ComponentScales);
            m_colorBuffer.UpdateBufferData(m_ps.Colors);

            m_spriteIndexBuffer.UpdateBufferData(m_ps.SpriteIndices);
            m_spriteOffsetUBuffer.UpdateBufferData(m_ps.SpriteOffsetsU);
            m_spriteOffsetVBuffer.UpdateBufferData(m_ps.SpriteOffsetsV);
            m_spriteSizeUBuffer.UpdateBufferData(m_ps.SpriteSizesU);
            m_spriteSizeVBuffer.UpdateBufferData(m_ps.SpriteSizesV);

            if (Billboarding == BillboardingType.Global) {
                // Setup "global" billboarding. (entire particle system is rendered as a camera-facing
                // billboard and will show the same position of particles from all angles)
                modelView = OpenTKHelper.BillboardMatrix(ref modelView);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelView);
            }

            if (AlphaBlendingEnabled) {
                //GL.Enable(EnableCap.AlphaTest);
                //GL.AlphaFunc(AlphaFunction.Greater, 0.1f);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Disable(EnableCap.Lighting);

                // Fixes flicker issues for particles with "fighting" view depth values
                // Also assumes the particle system is the last to be drawn in a scene
                GL.DepthFunc(DepthFunction.Lequal);
            }

            #if DRAW_USING_MAIN_SHADER
            // draw using the main shader
            // TODO: debug with bump mapped lighting mode
            SSMainShaderProgram shader = renderConfig.MainShader;
            shader.UniAmbTexEnabled = true;
            shader.UniDiffTexEnabled = false;
            shader.UniSpecTexEnabled = false;
            shader.UniBumpTexEnabled = false;
            // texture slot setup
            GL.ActiveTexture(TextureUnit.Texture2);
            #else
            // draw using the instancing shader
            SSInstanceShaderProgram shader = renderConfig.InstanceShader;
            // texture slot setup
            GL.ActiveTexture(TextureUnit.Texture0);
            #endif
            GL.Disable(EnableCap.ColorMaterial);

            // texture binding setup
            if (m_texture != null) {
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);
            } else {
                GL.Disable(EnableCap.Texture2D);
            }

            shader.Activate();
            // prepare uniforms
            #if DRAW_USING_MAIN_SHADER
            shader.UniInstanceDrawEnabled = true;
            #endif
            shader.UniInstanceBillboardingEnabled = (Billboarding == BillboardingType.Instanced);

            // prepare attribute arrays for draw
            prepareAttribute(m_posBuffer, shader.AttrInstancePos, m_ps.Positions);
            prepareAttribute(m_masterScaleBuffer, shader.AttrInstanceMasterScale, m_ps.MasterScales);
            prepareAttribute(m_componentScaleBuffer, shader.AttrInstanceComponentScale, m_ps.ComponentScales);
            prepareAttribute(m_colorBuffer, shader.AttrInstanceColor, m_ps.Colors);

            prepareAttribute(m_spriteIndexBuffer, shader.AttrInstanceSpriteIndex, m_ps.SpriteIndices);
            prepareAttribute(m_spriteOffsetUBuffer, shader.AttrInstanceSpriteOffsetU, m_ps.SpriteOffsetsU);
            prepareAttribute(m_spriteOffsetVBuffer, shader.AttrInstanceSpriteOffsetV, m_ps.SpriteOffsetsV);
            prepareAttribute(m_spriteSizeUBuffer, shader.AttrInstanceSpriteSizeU, m_ps.SpriteSizesU);
            prepareAttribute(m_spriteSizeVBuffer, shader.AttrInstanceSpriteSizeV, m_ps.SpriteSizesV);

            // do the draw
            s_billboardVbo.DrawInstanced(PrimitiveType.Triangles, m_ps.ActiveBlockLength);
             
            #if DRAW_USING_MAIN_SHADER
            shader.UniInstanceDrawEnabled = false;
            #endif
            m_posBuffer.DisableAttribute(shader.AttrInstancePos);
            m_masterScaleBuffer.DisableAttribute(shader.AttrInstanceMasterScale);
            m_componentScaleBuffer.DisableAttribute(shader.AttrInstanceComponentScale);
            m_colorBuffer.DisableAttribute(shader.AttrInstanceColor);

            m_spriteIndexBuffer.DisableAttribute(shader.AttrInstanceSpriteIndex);
            m_spriteOffsetUBuffer.DisableAttribute(shader.AttrInstanceSpriteOffsetU);
            m_spriteOffsetVBuffer.DisableAttribute(shader.AttrInstanceSpriteOffsetV);
            m_spriteSizeUBuffer.DisableAttribute(shader.AttrInstanceSpriteSizeU);
            m_spriteSizeVBuffer.DisableAttribute(shader.AttrInstanceSpriteSizeV);
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

