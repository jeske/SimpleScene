using System;
using System.Drawing;
using OpenTK;


namespace SimpleScene.Demos
{
    public class SimpleSunFlareEffect : Instanced2dEffect
    {
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

        public SimpleSunFlareEffect (
            SSScene sunDiskScene,
            SSObjectOcclusionQueuery sunDiskObj,
            SSTexture tex = null,
            RectangleF[] spriteRects = null,
            float[] spriteScales = null)
            : base(spriteRects != null ? spriteRects.Length : _defaultRects.Length,
                   sunDiskScene, tex ?? _defaultTexture(), sunDiskObj)
        {
            this.renderState.alphaBlendingOn = true;

            base.rects = spriteRects ?? _defaultRects;
            base.masterScales = spriteScales ?? _defaultSpriteScales;
        }

        protected override void _prepareSpritesData ()
        {
            var color4 = occObj.MainColor;
            color4.A = _occIntensity;
            instanceData.writeColor(0, color4);

            Vector2 compScale = new Vector2(
                Math.Max (_occSize.X, _occSize.Y) * Math.Min (1.5f, 1f / (1f - _occIntensity)));
            instanceData.writeComponentScale(0, compScale);

            Vector2 towardsCenter = _screenCenter - _occPos;
            int numElements = instanceData.activeBlockLength;
            for (int i = 0; i < numElements; ++i) {
                Vector2 center = _occPos + towardsCenter * 2.5f / (float)numElements * (float)i;
                instanceData.writePosition(i, center);
            }
        }
    }
}

