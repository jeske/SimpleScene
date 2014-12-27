using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectSunFlare : SSObject
    {
        const int c_numElements = 5;
        const float c_bigOffset = 0.8889f;
        const float c_smallOffset = 0.125f;

        private static readonly Vector2[] c_textureCoords = {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, c_bigOffset),
            new Vector2(0f, c_bigOffset),

            new Vector2(0f, c_bigOffset),
            new Vector2(c_smallOffset, c_bigOffset),
            new Vector2(c_smallOffset, 1f),
            new Vector2(0f, 1f),

            new Vector2(c_smallOffset, c_bigOffset),
            new Vector2(c_smallOffset*2f, c_bigOffset),
            new Vector2(c_smallOffset*2f, 1f),
            new Vector2(c_smallOffset, 1f),

            new Vector2(c_smallOffset*2f, c_bigOffset),
            new Vector2(c_smallOffset*3f, c_bigOffset),
            new Vector2(c_smallOffset*3f, 1f),
            new Vector2(c_smallOffset*2f, 1f),

            new Vector2(c_smallOffset*3f, c_bigOffset),
            new Vector2(c_smallOffset*4f, c_bigOffset),
            new Vector2(c_smallOffset*4f, 1f),
            new Vector2(c_smallOffset*3f, 1f),
        };

        // individual sprite scales, in terms of the on-screen size of the sun
        private static readonly float[] c_spriteScales = {
            20f, 1f, 2f, 1f, 1f
        };

        private static readonly UInt16[] c_indices = {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19
        };

        private SSVertex_PosTex1[] m_vertices;
        private SSVertexBuffer<SSVertex_PosTex1> m_vbo;
        private SSIndexBuffer m_ibo;

        private SSScene m_sunScene;
        private SSObjectBillboard m_sun;
        private SSTexture m_texture;

        static private Vector2 worldToScreen(Vector3 worldPos, ref Matrix4 viewProj, 
                                             ref Vector2 screenCenter, ref Vector2 clientRect) {
            Vector4 pos = Vector4.Transform(new Vector4(worldPos, 1f), viewProj);
            pos /= pos.W;
            pos.Y = -pos.Y;
            return screenCenter + pos.Xy * clientRect / 2f;
        }

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
            int queryResult = m_sun.QueueryResult;
            if (queryResult <= 0) return;

            // Begin the quest to update VBO vertices
            Matrix4 viewInverted = m_sunScene.InvCameraViewMatrix.Inverted();
            Vector3 viewRight = Vector3.Transform(new Vector3 (1f, 0f, 0f), viewInverted);
            Vector3 viewUp = Vector3.Transform(new Vector3 (0f, 1f, 0f), viewInverted);
            Vector3 sunRightMost = m_sun.Pos + viewRight.Normalized() * m_sun.Scale.X;
            Vector3 sunTopMost = m_sun.Pos + viewUp.Normalized() * m_sun.Scale.Y;

            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            Vector2 screenOrig = new Vector2 (viewport [0], viewport [1]);
            Vector2 clientRect = new Vector2 (viewport [2], viewport [3]);
            Vector2 screenCenter = screenOrig + clientRect / 2f;

            Matrix4 viewProj = m_sunScene.InvCameraViewMatrix * m_sunScene.ProjectionMatrix;
            Vector2 sunScreenPos = worldToScreen(m_sun.Pos, ref viewProj, ref screenCenter, ref clientRect);
            Vector2 sunScreenRightMost = worldToScreen(sunRightMost, ref viewProj, ref screenCenter, ref clientRect);
            Vector2 sunScreenTopMost = worldToScreen(sunTopMost, ref viewProj, ref screenCenter, ref clientRect);
            Vector2 towardsCenter = (screenCenter - sunScreenPos);

            Vector2 tileVecBase = new Vector2 (sunScreenRightMost.X - sunScreenPos.X, sunScreenPos.Y - sunScreenTopMost.Y);
            float sunFullEstimate = (float)Math.PI * tileVecBase.X * tileVecBase.Y;
            float intensityFraction = (float)queryResult / sunFullEstimate;

            // modulate sprite size with the intensity fraction
            tileVecBase *= Math.Min(1f / (1f - intensityFraction), 2f);

            for (int i = 0; i < c_numElements; ++i) {
                //assign positions
                Vector2 center = sunScreenPos + towardsCenter * 2.5f / (float)c_numElements * (float)i;
                Vector2 tileVec = tileVecBase * c_spriteScales [i];

                int baseIdx = i * 4;
                m_vertices [baseIdx].Position.X = center.X - tileVec.X;
                m_vertices [baseIdx].Position.Y = center.Y - tileVec.Y;

                m_vertices [baseIdx+1].Position.X = center.X + tileVec.X;
                m_vertices [baseIdx+1].Position.Y = center.Y - tileVec.Y;

                m_vertices [baseIdx+2].Position.X = center.X + tileVec.X;
                m_vertices [baseIdx+2].Position.Y = center.Y + tileVec.Y;

                m_vertices [baseIdx+3].Position.X = center.X - tileVec.X;
                m_vertices [baseIdx+3].Position.Y = center.Y + tileVec.Y;
            }
            m_vbo.UpdateBufferData(m_vertices);

            // now, actually draw
            base.Render(ref renderConfig);
            SSShaderProgram.DeactivateAll(); // disable GLSL
            GL.Disable(EnableCap.CullFace);
            GL.Enable (EnableCap.AlphaTest);
            GL.Enable (EnableCap.Blend);
            GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Disable(EnableCap.Lighting);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);

            // modulate color alpha with the intensity fraction
            Vector4 color = m_sun.Color;
            color.W = intensityFraction;
            GL.Color4(color);
            //GL.Color3(0f, 1f, 0f);

            m_ibo.DrawElements(PrimitiveType.Triangles);
        }
    }
}

