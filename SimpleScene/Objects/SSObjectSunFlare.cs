using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSObjectSunFlare : SSObjectMesh
    {
        // TODO decouple some sprite details from this class
        // (make it more friendly for generic use)

        #region Instance-Specific Drawing Constructs
        private SSVertex_PosTex[] vertices;

        private Vector2[] spriteScales;
        private int numElements;
        #endregion

        #region Source of Per-Frame Input
        private SSScene sunScene;
        private SSObjectBillboard sunBillboard;
        #endregion

        #region Per-Frame Temp Variables
        private Matrix4 sunSceneViewProj;
        private Vector2 screenCenter;
        private Vector2 clientRect;
        #endregion

		public new SSIndexedMesh<SSVertex_PosTex> Mesh {
			get { return base.Mesh as SSIndexedMesh<SSVertex_PosTex>; }
			set { base.Mesh = value; }
		}

		public override bool alphaBlendingEnabled { get { return true; } }

        private Vector2 worldToScreen(Vector3 worldPos) {
            Vector4 pos = Vector4.Transform(new Vector4(worldPos, 1f), sunSceneViewProj);
            pos /= pos.W;
            pos.Y = -pos.Y;
            return screenCenter + pos.Xy * clientRect / 2f;
        }

        public SSObjectSunFlare (SSScene sunScene,
                                 SSObjectBillboard sun,
                                 SSTexture texture,
                                 RectangleF[] spriteRects,
                                 Vector2[] spriteScales = null)
        {
            init(sunScene, sun, texture, spriteRects, spriteScales);
        }

        public SSObjectSunFlare(SSScene sunScene,
                                SSObjectBillboard sun,
                                SSTexture texture,
                                RectangleF[] spriteRects,
                                float[] spriteScales) {
            Vector2[] spriteScalesV2 = new Vector2[spriteScales.Length];
            for (int i = 0; i < spriteScalesV2.Length; ++i) {
                spriteScalesV2 [i] = new Vector2 (spriteScales [i]);
            }
            init(sunScene, sun, texture, spriteRects, spriteScalesV2);
        }

        public override void Render (SSRenderConfig renderConfig)
        {
            int queryResult = sunBillboard.OcclusionQueueryResult;
            if (queryResult <= 0) return;

            // Begin the quest to update VBO vertices
            Matrix4 viewInverted = sunScene.renderConfig.invCameraViewMatrix.Inverted();
            Vector3 viewRight = Vector3.Transform(new Vector3 (1f, 0f, 0f), viewInverted);
            Vector3 viewUp = Vector3.Transform(new Vector3 (0f, 1f, 0f), viewInverted);
            Vector3 sunRightMost = sunBillboard.Pos + viewRight.Normalized() * sunBillboard.Scale.X;
            Vector3 sunTopMost = sunBillboard.Pos + viewUp.Normalized() * sunBillboard.Scale.Y;

            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            Vector2 screenOrig = new Vector2 (viewport [0], viewport [1]);
			clientRect = new Vector2 (viewport [2], viewport [3]) / 2f;
            screenCenter = screenOrig + clientRect / 2f;
            sunSceneViewProj = sunScene.renderConfig.invCameraViewMatrix * sunScene.renderConfig.projectionMatrix;
            Vector2 sunScreenPos = worldToScreen(sunBillboard.Pos);
            Vector2 sunScreenRightMost = worldToScreen(sunRightMost);
            Vector2 sunScreenTopMost = worldToScreen(sunTopMost);
            Vector2 towardsCenter = screenCenter - sunScreenPos;

            Vector2 tileVecBase = new Vector2 (sunScreenRightMost.X - sunScreenPos.X, sunScreenPos.Y - sunScreenTopMost.Y);
            float sunFullEstimate = (float)Math.PI * tileVecBase.X * tileVecBase.Y;
			float intensityFraction = Math.Min((float)queryResult / sunFullEstimate, 1f);

            // modulate sprite size with the intensity fraction
            tileVecBase *= Math.Min(1f / (1f - intensityFraction), 1.5f);

            // allow simple scaling
            tileVecBase.X *= Scale.X; 
            tileVecBase.Y *= Scale.Y;

            for (int i = 0; i < numElements; ++i) {
                //assign positions
                Vector2 center = sunScreenPos + towardsCenter * 2.5f / (float)numElements * (float)i;
                Vector2 tileVec = tileVecBase * spriteScales [i];

                int baseIdx = i * 4;
                vertices [baseIdx].Position.X = center.X - tileVec.X;
                vertices [baseIdx].Position.Y = center.Y - tileVec.Y;

                vertices [baseIdx+1].Position.X = center.X + tileVec.X;
                vertices [baseIdx+1].Position.Y = center.Y - tileVec.Y;

                vertices [baseIdx+2].Position.X = center.X + tileVec.X;
                vertices [baseIdx+2].Position.Y = center.Y + tileVec.Y;

                vertices [baseIdx+3].Position.X = center.X - tileVec.X;
                vertices [baseIdx+3].Position.Y = center.Y + tileVec.Y;
            }
			Mesh.UpdateVertices(vertices);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            SSShaderProgram.DeactivateAll(); // disable shaders

            // modulate color alpha with the intensity fraction
			this.MainColor = sunBillboard.MainColor;
			this.MainColor.A = intensityFraction;

			// now, actually draw
			base.Render(renderConfig);
        }

        private void init(SSScene sunScene,
                          SSObjectBillboard sun,
                          SSTexture texture,
                          RectangleF[] spriteRects,
                          Vector2[] spriteScales)
        {
			base.textureMaterial = new SSTextureMaterial (texture);
            this.sunBillboard = sun;
            this.sunScene = sunScene;
            this.numElements = spriteRects.Length;
            if (spriteScales == null) {
                spriteScales = new Vector2[numElements];
                for (int i = 0; i < numElements; ++i) {
                    spriteScales [i] = new Vector2(1f);
                }
            } else {                
                if (spriteScales.Length != numElements) {
                    throw new Exception ("texture coordinate array size does not match that of sprite scale array");
                    spriteScales = new Vector2[numElements];
                    for (int i = 0; i < numElements; ++i) {
                        spriteScales [i] = new Vector2(1f);
                    }
                }
            }
            this.spriteScales = spriteScales;

            UInt16[] indices = new UInt16[numElements*6];
            for (int i = 0; i < numElements; ++i) {
                int baseLoc = i * 6;
                int baseVal = i * 4;
                indices [baseLoc] = (UInt16)baseVal;
                indices [baseLoc + 1] = (UInt16)(baseVal + 2);
                indices [baseLoc + 2] = (UInt16)(baseVal + 1);
                indices [baseLoc + 3] = (UInt16)baseVal;
                indices [baseLoc + 4] = (UInt16)(baseVal + 3);
                indices [baseLoc + 5] = (UInt16)(baseVal + 2);
            }
			Mesh = new SSIndexedMesh<SSVertex_PosTex> (null, indices);

            vertices = new SSVertex_PosTex[numElements * 4];
            for (int r = 0; r < spriteRects.Length; ++r) {
                RectangleF rect = spriteRects [r];
                int baseIdx = r * 4;
                vertices [baseIdx]   = new SSVertex_PosTex (0f, 0f, 0f, rect.Left, rect.Bottom);
                vertices [baseIdx+1] = new SSVertex_PosTex (0f, 0f, 0f, rect.Right, rect.Bottom);
                vertices [baseIdx+2] = new SSVertex_PosTex (0f, 0f, 0f, rect.Right, rect.Top);
                vertices [baseIdx+3] = new SSVertex_PosTex (0f, 0f, 0f, rect.Left, rect.Top);
            }
        }
    }
}

