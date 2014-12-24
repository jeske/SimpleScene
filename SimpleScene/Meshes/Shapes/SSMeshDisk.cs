using System;
using System.Collections.Generic;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSMeshDisk : SSAbstractMesh
    {
        private SSVertex_PosTex1[] m_vertices;
        private UInt16[] m_indices; 

        private SSIndexBuffer m_ibo;
        private SSVertexBuffer<SSVertex_PosTex1> m_vbo;
        private SSTexture m_texture;

        public SSMeshDisk (int divisions = 16,
            SSTexture texture = null, 
            float texOffset = 0.1f)
        {
            m_texture = texture;

            // generate vertices
            m_vertices = new SSVertex_PosTex1[divisions + 1];
            m_vertices [0] = new SSVertex_PosTex1 (0f, 0f, 0f, 0.5f, 0.5f);

            float angleStep = 2f * (float)Math.PI / divisions;
            float Tr = 0.5f - texOffset;

            for (int i = 0; i < divisions; ++i) {
                float angle = i * angleStep;
                float x = (float)Math.Cos(angle);
                float y = (float)Math.Sin(angle);
                m_vertices [i + 1] = new SSVertex_PosTex1 (
                    x, y, 0f,
                    0.5f + Tr * x, 0.5f + Tr * y
                );       
            }

            // generate indices
            m_indices = new UInt16[divisions * 3];
            for (int i = 0; i < divisions; ++i) {
                int baseIdx = i * 3;
                m_indices [baseIdx] = 0;
                m_indices [baseIdx + 1] = (UInt16)(i + 1);
                m_indices [baseIdx + 2] = (UInt16)(i + 2);
            }
            // last one is a special case (wraparound)
            m_indices [m_indices.Length - 1] = 1;

            // Generate VBO and IBO
            m_vbo = new SSVertexBuffer<SSVertex_PosTex1> (m_vertices);
            m_ibo = new SSIndexBuffer (m_indices, m_vbo);
        }

        public override void RenderMesh(ref SSRenderConfig renderConfig)
        {
            if (m_texture == null) return;

            SSShaderProgram.DeactivateAll();

            {
                GL.Enable (EnableCap.AlphaTest);
                GL.Enable (EnableCap.Blend);
                GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            }

            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Lighting);
            GL.Color3(Color.White);
            GL.Disable(EnableCap.ColorMaterial);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);

            m_ibo.DrawElements(PrimitiveType.Triangles);
        }

        public override IEnumerable<Vector3> EnumeratePoints () 
        {
            for (int i = 0; i < m_vertices.Length; ++i) {
                yield return new Vector3 (m_vertices[i].Position);
            }
        }

    }
}

