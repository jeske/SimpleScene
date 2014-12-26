using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectSunFlare : SSObject
    {
        const int c_numElements = 1;

        private static readonly UInt16[] c_indices = {
            0, 1, 2, 0, 2, 3
        };

        private static readonly Vector2[] c_textureCoords = {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        private SSVertex_PosTex1[] m_vertices;
        private SSVertexBuffer<SSVertex_PosTex1> m_vbo;
        private SSIndexBuffer m_ibo;

        private SSScene m_sunScene;
        private SSObjectBillboard m_sun;
        private SSTexture m_texture;

        public SSObjectSunFlare (SSScene sunScene,
                                 SSObjectBillboard sun,
                                 SSTexture texture)
        {
            m_sun = sun;
            m_sunScene = sunScene;
            m_texture = texture;

            m_vbo = new SSVertexBuffer<SSVertex_PosTex1> (BufferUsageHint.DynamicDraw);
            m_ibo = new SSIndexBuffer (c_indices, m_vbo);

            int vertSz = c_numElements * 4;
            m_vertices = new SSVertex_PosTex1[vertSz];
            for (int i = 0; i < vertSz; ++i) {
                Vector2 texCoord = c_textureCoords [i];
                m_vertices [i] = new SSVertex_PosTex1 (0f, 0f, 0f, texCoord.X, texCoord.Y);
            }
        }

          public override void Render (ref SSRenderConfig renderConfig)
        {
            Matrix4 viewProj = m_sunScene.InvCameraViewMatrix * m_sunScene.ProjectionMatrix;
            Vector4 sunPos = Vector4.Transform(new Vector4(m_sun.Pos, 1f), viewProj);
            sunPos /= sunPos.W;

            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            Vector2 screenOrig = new Vector2 (viewport [0], viewport [1]);
            Vector2 clientRect = new Vector2 (viewport [2], viewport [3]);
            Vector2 screenCenter = screenOrig + clientRect / 2f;
            Vector2 sunScreenPos = screenOrig + sunPos.Xy * clientRect;
            Vector2 towardsCenter = (screenCenter - sunScreenPos).Normalized();

            //sunScreenPos = new Vector2 (500f, 500f);

            Vector2 tileVec = Scale.Xy * 128f / 2f;
            for (int i = 0; i < c_numElements; ++i) {
                //assign positions
                Vector2 center = sunScreenPos + towardsCenter * i * 10f; // TODO scale based on sqrt(w^2 + h^2)

                m_vertices [i].Position.X = center.X - tileVec.X;
                m_vertices [i].Position.Y = center.Y - tileVec.Y;

                m_vertices [i+1].Position.X = center.X + tileVec.X;
                m_vertices [i+1].Position.Y = center.Y - tileVec.Y;

                m_vertices [i+2].Position.X = center.X + tileVec.X;
                m_vertices [i+2].Position.Y = center.Y + tileVec.Y;

                m_vertices [i+3].Position.X = center.X - tileVec.X;
                m_vertices [i+3].Position.Y = center.Y + tileVec.Y;
            }
            m_vbo.UpdateBufferData(m_vertices);

            SSShaderProgram.DeactivateAll();

            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.ColorMaterial);
            GL.Enable (EnableCap.AlphaTest);
            GL.Enable (EnableCap.Blend);
            GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //GL.Enable(EnableCap.Texture2D);
            //GL.ActiveTexture(TextureUnit.Texture0);
            //GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);                                   

            GL.Disable(EnableCap.Texture2D);

            GL.Translate(0f, 0f, 0f);
            //GL.Color3(m_sun.Color);
            GL.Color3(Color.Red);
            m_ibo.DrawElements(PrimitiveType.Triangles);
        }
    }
}

