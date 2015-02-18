using System;
using System.Collections.Generic;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSMeshDisk : SSIndexedMesh<SSVertex_PosTex1>
    {
        private SSTexture m_texture;

        public SSMeshDisk (int divisions = 50,
                           SSTexture texture = null, 
                           float texOffset = 0.1f)
            : base(BufferUsageHint.StaticDraw, BufferUsageHint.StaticDraw)
        {
            m_texture = texture;

            // generate vertices
            SSVertex_PosTex1[] vertices = new SSVertex_PosTex1[divisions + 1];
            vertices [0] = new SSVertex_PosTex1 (0f, 0f, 0f, 0.5f, 0.5f);

            float angleStep = 2f * (float)Math.PI / divisions;
            float Tr = 0.5f + texOffset;

            for (int i = 0; i < divisions; ++i) {
                float angle = i * angleStep;
                float x = (float)Math.Cos(angle);
                float y = (float)Math.Sin(angle);
                vertices [i + 1] = new SSVertex_PosTex1 (
                    x, y, 0f,
                    0.5f + Tr * x, 0.5f + Tr * y
                );       
            }
            UpdateVertices(vertices);

            // generate indices
            UInt16[] indices = new UInt16[divisions * 3];
            for (int i = 0; i < divisions; ++i) {
                int baseIdx = i * 3;
                indices [baseIdx] = 0;
                indices [baseIdx + 1] = (UInt16)(i + 1);
                indices [baseIdx + 2] = (UInt16)(i + 2);
            }
            // last one is a special case (wraparound)
            indices [indices.Length - 1] = 1;
            UpdateIndices(indices);
        }

        public override void RenderMesh(ref SSRenderConfig renderConfig)
        {
            SSShaderProgram.DeactivateAll();

            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.ColorMaterial);
            GL.Enable (EnableCap.AlphaTest);
            GL.Enable (EnableCap.Blend);
            GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            if (m_texture != null) {
                GL.Enable(EnableCap.Texture2D);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);                                   
            } else {
                GL.Disable(EnableCap.Texture2D);
            }

            base.RenderMesh(ref renderConfig);
        }

        public override float Radius ()
        {
            return 1f;
        }
    }
}

