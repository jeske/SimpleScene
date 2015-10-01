using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SSSunFlareRenderer : SSInstancedSpriteRenderer
    {
        public static SSTexture defaultTexture()
        {
            return SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "sun_flare.png");
            //return SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "sun_flare_debug.png");
            //return SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "uv_checker large.png"))
        }

        protected readonly SSunFlareUpdater _updater;

        public SSSunFlareRenderer(SSScene camera3dScene, SSObjectOcclusionQueuery sunDiskObj,
            SSTexture texture = null)
            : base(camera3dScene, 
                new SInstancedSpriteData(Math.Max(SSunFlareUpdater.defaultRects.Length, 
                                                  SSunFlareUpdater.defaultSpriteScales.Length)),
                texture ?? defaultTexture())
        {
            this.renderState.alphaBlendingOn = true;
            this.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            this.renderState.blendFactorDest = BlendingFactorDest.OneMinusSrcAlpha;

            addUpdater(new SSunFlareUpdater (camera3dScene, sunDiskObj));
        }
    }

    public class SSunFlareUpdater : ISSpriteUpdater
    {
        #region default sprite and texture configuration
        protected const float _smallUOffset = 0.125f;
        protected const float _bigVOffset = 0.88889f;

        public static readonly RectangleF[] defaultRects = {
            new RectangleF(0f, 0f, 1f, _bigVOffset),
            new RectangleF(0f, _bigVOffset, _smallUOffset, 1f-_bigVOffset),
            new RectangleF(_smallUOffset, _bigVOffset, _smallUOffset, 1f-_bigVOffset),
            new RectangleF(_smallUOffset*2f, _bigVOffset, _smallUOffset, 1f-_bigVOffset),
            new RectangleF(_smallUOffset*3f, _bigVOffset, _smallUOffset, 1f-_bigVOffset)
        };

        public static readonly float[] defaultSpriteScales = { 40f, 2f, 4f, 2f, 2f };
        #endregion

        #region sun occlusion disk
        protected readonly SSScene _sunDiskOccScene = null;
        protected readonly SSObjectOcclusionQueuery _sunDiskOccObj = null;
        protected float _sunDiskOccIntensity = 1f;
        protected Vector2 _sunDiskOccPos = Vector2.Zero;
        protected Vector2 _sunDiskOccSize = Vector2.Zero;
        #endregion

        protected int[] _spriteSlotIdxs;
        protected readonly RectangleF[] _rects;
        protected readonly float[] _scales;

        public SSunFlareUpdater (
            SSScene sunDiskScene,
            SSObjectOcclusionQueuery sunDiskObj,
            RectangleF[] spriteRects = null,
            float[] spriteScales = null
        )
        {
            this._sunDiskOccScene = sunDiskScene;
            this._sunDiskOccObj = sunDiskObj;

            _rects = spriteRects ?? defaultRects;
            _scales = spriteScales ?? defaultSpriteScales;
        }

        public void setupSprites(SInstancedSpriteData instanceData)
        {
            var numElements = Math.Max(_rects.Length, _scales.Length);
            _spriteSlotIdxs = instanceData.requestSlots(numElements);

            for (int i = 0; i < _rects.Length; ++i) {
                instanceData.writeRect(_spriteSlotIdxs[i], _rects [i]);
            }
            for (int i = 0; i < _scales.Length; ++i) {
                instanceData.writeMasterScale(_spriteSlotIdxs [i], _scales [i]);
            }
        }

        public void releaseSprites(SInstancedSpriteData instancesData)
        {
            instancesData.releaseSlots(_spriteSlotIdxs);
        }

        public void updateSprites(SInstancedSpriteData instanceData, ref RectangleF clientRect,
                                  ref Vector3 cameraPos, ref Matrix4 camera3dView, ref Matrix4 camera3dProj)
        {
            Matrix4 camera3dViewProjMat = camera3dView * camera3dProj;
            if (_sunDiskOccObj != null) {
                Matrix4 viewInverted = _sunDiskOccScene.renderConfig.invCameraViewMatrix.Inverted();
                Vector3 viewRight = Vector3.Transform(Vector3.UnitX, viewInverted).Normalized();
                Vector3 viewUp = Vector3.Transform(Vector3.UnitY, viewInverted).Normalized();
                Vector3 occRightMost = _sunDiskOccObj.Pos + viewRight * _sunDiskOccObj.Scale.X;
                Vector3 occTopMost = _sunDiskOccObj.Pos + viewUp * _sunDiskOccObj.Scale.Y;
                _sunDiskOccPos = OpenTKHelper.WorldToScreen(_sunDiskOccObj.Pos, ref camera3dViewProjMat, ref clientRect);
                Vector2 occRightMostPt = OpenTKHelper.WorldToScreen(occRightMost, ref camera3dViewProjMat, ref clientRect);
                Vector2 occTopMostPt = OpenTKHelper.WorldToScreen(occTopMost, ref camera3dViewProjMat, ref clientRect);
                _sunDiskOccSize = 2f * new Vector2 (occRightMostPt.X - _sunDiskOccPos.X, _sunDiskOccPos.Y - occTopMostPt.Y);
                float bbFullEstimate = (float)Math.PI * (float)_sunDiskOccSize.X * (float)_sunDiskOccSize.Y / 4f;
                _sunDiskOccIntensity = Math.Min((float)_sunDiskOccObj.OcclusionQueueryResult / bbFullEstimate, 1f);
            }

            int numElements = _spriteSlotIdxs.Length;
            if (_sunDiskOccIntensity <= 0f) {
                // hide all sprites
                for (int i = 0; i < numElements; ++i) {
                    instanceData.writePosition(_spriteSlotIdxs [i], new Vector2 (float.NaN)); // hide all sprites
                }
            } else {
                Vector2 compScale = new Vector2(Math.Max (_sunDiskOccSize.X, _sunDiskOccSize.Y) 
                    * Math.Min (1.5f, 1f / (1f - _sunDiskOccIntensity)));
                Vector2 center = new Vector2 (clientRect.X, clientRect.Y)
                    + new Vector2 (clientRect.Width, clientRect.Width) / 2f;
                Vector2 towardsCenter = center - _sunDiskOccPos;
                var color4 = _sunDiskOccObj.MainColor;
                color4.A = _sunDiskOccIntensity;
                for (int i = 0; i < numElements; ++i) {
                    int writeIdx = _spriteSlotIdxs [i];
                    instanceData.writeComponentScale(writeIdx, compScale);
                    instanceData.writeColor(writeIdx, color4);

                    Vector2 spriteCenter = _sunDiskOccPos + towardsCenter * 2.5f 
                        / (float)numElements * (float)i;
                    instanceData.writePosition(_spriteSlotIdxs[i], spriteCenter);
                    
                }
            }
        }
    }
}

