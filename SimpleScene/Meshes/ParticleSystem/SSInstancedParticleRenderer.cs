using System;

namespace SimpleScene
{
    public abstract class SSInstancedParticleRenderer : SSObject
    {
        protected SSParticleSystem m_ps;
        protected SSIndexBuffer m_ibo;
        protected SSVertexBuffer<SSVertex_Pos> m_billboardVbo;
        protected SSAttributeBuffer<SSAttributePos> m_posBuffer;
        protected SSAttributeBuffer<SSAttributeColor> m_colorBuffer;

        public SSInstancedParticleRenderer (SSParticleSystem ps)
        {
            m_ps = ps;
        }

        public override void Render (ref SSRenderConfig renderConfig)
        {
            base.Render(ref renderConfig);


        }
    }
}

