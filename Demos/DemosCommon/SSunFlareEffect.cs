using System;
using System.Drawing;
using OpenTK;


namespace SimpleScene.Demos
{
    public class SSunFlareEffect : SSInstanced2dEffect
    {
        #region default sprite and texture configuration
        protected const float _bigOffset = 0.8889f;
        protected const float _smallOffset = 0.125f;
        protected static readonly RectangleF[] _defaultRects = {
            new RectangleF(0f, 0f, 1f, _bigOffset),
            new RectangleF(0f, _bigOffset, _smallOffset, _smallOffset),
            new RectangleF(_smallOffset, _bigOffset, _smallOffset, _smallOffset),
            new RectangleF(_smallOffset*2f, _bigOffset, _smallOffset, _smallOffset),
            new RectangleF(_smallOffset*3f, _bigOffset, _smallOffset, _smallOffset)
        };

        protected static readonly float[] _defaultSpriteScales = { 40f, 2f, 4f, 2f, 2f };

        protected static SSTexture _defaultTexture()
        {
            //return SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "sun_flare_debug.png");
            return SSAssetManager.GetInstance<SSTextureWithAlpha>(".", "sun_flare.png");
        }
        #endregion

        #region sun occlusion disk
        protected readonly SSScene _sunDiskOccScene = null;
        protected readonly SSObjectOcclusionQueuery _sunDiskOccObj = null;
        protected float _sunDiskOccIntensity = 1f;
        protected Vector2 _sunDiskOccPos = Vector2.Zero;
        protected Vector2 _sunDiskOccSize = Vector2.Zero;
        #endregion

        public SSunFlareEffect (
            SSScene sunDiskScene,
            SSObjectOcclusionQueuery sunDiskObj,
            SSTexture tex = null,
            RectangleF[] spriteRects = null,
            float[] spriteScales = null)
            : base(spriteRects != null ? spriteRects.Length : _defaultRects.Length,
                   sunDiskScene, tex ?? _defaultTexture())
        {
            this.renderState.alphaBlendingOn = true;
            this._sunDiskOccScene = sunDiskScene;
            this._sunDiskOccObj = sunDiskObj;

            base.rects = spriteRects ?? _defaultRects;
            base.masterScales = spriteScales ?? _defaultSpriteScales;
        }

        public override void Render (SSRenderConfig renderConfig)
        {
            if (_sunDiskOccObj != null) {
                Matrix4 viewInverted = _sunDiskOccScene.renderConfig.invCameraViewMatrix.Inverted();
                Vector3 viewRight = Vector3.Transform(Vector3.UnitX, viewInverted).Normalized();
                Vector3 viewUp = Vector3.Transform(Vector3.UnitY, viewInverted).Normalized();
                Vector3 occRightMost = _sunDiskOccObj.Pos + viewRight * _sunDiskOccObj.Scale.X;
                Vector3 occTopMost = _sunDiskOccObj.Pos + viewUp * _sunDiskOccObj.Scale.Y;
                _sunDiskOccPos = worldToScreen(_sunDiskOccObj.Pos);
                Vector2 occRightMostPt = worldToScreen(occRightMost);
                Vector2 occTopMostPt = worldToScreen(occTopMost);
                _sunDiskOccSize = 2f * new Vector2 (occRightMostPt.X - _sunDiskOccPos.X, _sunDiskOccPos.Y - occTopMostPt.Y);
                float bbFullEstimate = (float)Math.PI * (float)_sunDiskOccSize.X * (float)_sunDiskOccSize.Y / 4f;
                _sunDiskOccIntensity = Math.Min((float)_sunDiskOccObj.OcclusionQueueryResult / bbFullEstimate, 1f);
            }

            base.Render(renderConfig);
        }

        protected override void _prepareSpritesData ()
        {
            var color4 = _sunDiskOccObj.MainColor;
            color4.A = _sunDiskOccIntensity;
            instanceData.writeColor(0, color4);

            Vector2 compScale = new Vector2(
                Math.Max (_sunDiskOccSize.X, _sunDiskOccSize.Y) * Math.Min (1.5f, 1f / (1f - _sunDiskOccIntensity)));
            instanceData.writeComponentScale(0, compScale);

            Vector2 center = new Vector2 (_clientRect.X, _clientRect.Y)
                           + new Vector2 (_clientRect.Width, _clientRect.Width) / 2f;
            Vector2 towardsCenter = center - _sunDiskOccPos;
            int numElements = instanceData.activeBlockLength;
            for (int i = 0; i < numElements; ++i) {
                Vector2 spriteCenter = _sunDiskOccPos + towardsCenter * 2.5f / (float)numElements * (float)i;
                instanceData.writePosition(i, spriteCenter);
            }
        }
    }
}

