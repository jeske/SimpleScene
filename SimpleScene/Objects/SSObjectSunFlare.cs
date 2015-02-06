using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectSunFlare : SSObject
    {
        // TODO decouple some sprite details from this class
        // (make it more friendly for generic use)

        #region Instance-Specific Drawing Constructs
        private SSVertex_PosTex1[] m_vertices;

        private SSVertexBuffer<SSVertex_PosTex1> m_vbo;
        private SSIndexBuffer m_ibo;
        private SSTexture m_texture;
        private Vector2[] m_textureCoords;
        private Vector2[] m_spriteScales;
        private int m_numElements;
        #endregion

        #region Source of Per-Frame Input
        private SSScene m_sunScene;
        private SSObjectBillboard m_sun;
        #endregion

        #region Per-Frame Temp Variables
        private Matrix4 m_sunSceneViewProj;
        private Vector2 m_screenCenter;
        private Vector2 m_clientRect;
        #endregion

        private Vector2 worldToScreen(Vector3 worldPos) {
            Vector4 pos = Vector4.Transform(new Vector4(worldPos, 1f), m_sunSceneViewProj);
            pos /= pos.W;
            pos.Y = -pos.Y;
            return m_screenCenter + pos.Xy * m_clientRect / 2f;
        }

        public SSObjectSunFlare (SSScene sunScene,
                                 SSObjectBillboard sun,
                                 SSTexture texture,
                                 Vector2[] texCoords,
                                 Vector2[] spriteScales = null)
        {
            init(sunScene, sun, texture, texCoords, spriteScales);
        }

        public SSObjectSunFlare(SSScene sunScene,
                                SSObjectBillboard sun,
                                SSTexture texture,
                                Vector2[] texCoords,
                                float[] spriteScales) {
            Vector2[] spriteScalesV2 = new Vector2[spriteScales.Length];
            for (int i = 0; i < spriteScalesV2.Length; ++i) {
                spriteScalesV2 [i] = new Vector2 (spriteScales [i]);
            }
            init(sunScene, sun, texture, texCoords, spriteScalesV2);
        }

        public override void Render (ref SSRenderConfig renderConfig)
        {
            int queryResult = m_sun.OcclusionQueueryResult;
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
            m_clientRect = new Vector2 (viewport [2], viewport [3]);
            m_screenCenter = screenOrig + m_clientRect / 2f;
            m_sunSceneViewProj = m_sunScene.InvCameraViewMatrix * m_sunScene.ProjectionMatrix;
            Vector2 sunScreenPos = worldToScreen(m_sun.Pos);
            Vector2 sunScreenRightMost = worldToScreen(sunRightMost);
            Vector2 sunScreenTopMost = worldToScreen(sunTopMost);
            Vector2 towardsCenter = m_screenCenter - sunScreenPos;

            Vector2 tileVecBase = new Vector2 (sunScreenRightMost.X - sunScreenPos.X, sunScreenPos.Y - sunScreenTopMost.Y);
            float sunFullEstimate = (float)Math.PI * tileVecBase.X * tileVecBase.Y;
			float intensityFraction = Math.Min((float)queryResult / sunFullEstimate, 1f);

            // modulate sprite size with the intensity fraction
            tileVecBase *= Math.Min(1f / (1f - intensityFraction), 1.5f);

            // allow simple scaling
            tileVecBase.X *= Scale.X; 
            tileVecBase.Y *= Scale.Y;

            for (int i = 0; i < m_numElements; ++i) {
                //assign positions
                Vector2 center = sunScreenPos + towardsCenter * 2.5f / (float)m_numElements * (float)i;
                Vector2 tileVec = tileVecBase * m_spriteScales [i];

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
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            SSShaderProgram.DeactivateAll(); // disable shaders

            GL.Enable (EnableCap.AlphaTest);
            GL.Enable (EnableCap.Blend);
            GL.AlphaFunc(AlphaFunction.Always, 0f);
            GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Disable(EnableCap.Lighting);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, m_texture.TextureID);

            // modulate color alpha with the intensity fraction
            Color4 color = m_sun.MainColor;
            color.A = intensityFraction;
            GL.Color4(color);
            //GL.Color3(0f, 1f, 0f);

            m_ibo.DrawElements(PrimitiveType.Triangles);
        }

        private void init(SSScene sunScene,
                          SSObjectBillboard sun,
                          SSTexture texture,
                          Vector2[] texCoords,
                          Vector2[] spriteScales)
        {
            m_sun = sun;
            m_sunScene = sunScene;
            m_texture = texture;
            m_textureCoords = texCoords;
            if (spriteScales == null) {
                m_numElements = m_textureCoords.Length / 4;
                m_spriteScales = new Vector2[m_numElements];
                for (int i = 0; i < m_numElements; ++i) {
                    m_spriteScales [i] = new Vector2(1f);
                }
            } else {
                m_spriteScales = spriteScales;
                if (m_spriteScales.Length != m_textureCoords.Length / 4) {
                    throw new Exception ("texture coordinate array size does not match that of sprite scale array");
                }
                m_numElements = m_spriteScales.Length;
            }
            UInt16[] indices = new UInt16[m_numElements*6];
            for (int i = 0; i < m_numElements; ++i) {
                int baseLoc = i * 6;
                int baseVal = i * 4;
                indices [baseLoc] = (UInt16)baseVal;
                indices [baseLoc + 1] = (UInt16)(baseVal + 2);
                indices [baseLoc + 2] = (UInt16)(baseVal + 1);
                indices [baseLoc + 3] = (UInt16)baseVal;
                indices [baseLoc + 4] = (UInt16)(baseVal + 3);
                indices [baseLoc + 5] = (UInt16)(baseVal + 2);
            }
            m_vbo = new SSVertexBuffer<SSVertex_PosTex1> (BufferUsageHint.DynamicDraw);
            m_ibo = new SSIndexBuffer (indices, m_vbo);

            int vertSz = m_numElements * 4;
            m_vertices = new SSVertex_PosTex1[vertSz];
            for (int i = 0; i < vertSz; ++i) {
                Vector2 texCoord = m_textureCoords [i];
                m_vertices [i] = new SSVertex_PosTex1 (0f, 0f, 0f, texCoord.X, texCoord.Y);
            }
        }
    }
}

