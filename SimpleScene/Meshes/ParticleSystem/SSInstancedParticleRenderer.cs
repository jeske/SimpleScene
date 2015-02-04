using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public abstract class SSInstancedParticleRenderer : SSObject
    {
        private static readonly SSVertex_Pos[] c_billboardVertices = {
            // CCW quad; no indexing
            new SSVertex_Pos(0f, 0f, 0f),
            new SSVertex_Pos(1f, 0f, 0f),
            new SSVertex_Pos(1f, 1f, 0f),

            new SSVertex_Pos(0f, 0f, 0f),
            new SSVertex_Pos(1f, 1f, 0f),
            new SSVertex_Pos(0f, 1f, 0f),
        };
        protected static readonly SSVertexBuffer<SSVertex_Pos> s_billboardVbo;

        protected SSParticleSystem m_ps;
        protected SSIndexBuffer m_ibo;
        protected SSAttributeBuffer<SSAttributePos> m_posBuffer;
        protected SSAttributeBuffer<SSAttributeColor> m_colorBuffer;

        static SSInstancedParticleRenderer()
        {
            s_billboardVbo = new SSVertexBuffer<SSVertex_Pos> (c_billboardVertices);
        }

        public SSInstancedParticleRenderer (SSParticleSystem ps)
        {
            m_ps = ps;
            m_posBuffer = new SSAttributeBuffer<SSAttributePos> (BufferUsageHint.StreamDraw);
            m_colorBuffer = new SSAttributeBuffer<SSAttributeColor> (BufferUsageHint.StreamDraw);
        }

        public override void Render (ref SSRenderConfig renderConfig)
        {
            base.Render(ref renderConfig);


        }
    }
}

