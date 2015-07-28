using System;
using System.Drawing;
using OpenTK;

namespace SimpleScene.Demos
{
    public class SLaserHitFlareObject : SSInstanced2dEffect
    {
        protected static SSTexture getDefaultTexture()
        {
            return SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "corona.png");
        }

        protected static readonly float[] _defaultScales = { 1f };

        protected static readonly RectangleF[] _defaultRects = { new RectangleF(0f, 0f, 1f, 1f) };

        public SLaserHitFlareObject (
            SLaser laser, int beamId,
            SSScene camera3dScene,
            SSTexture texture = null,
            RectangleF[] rects = null,
            float[] scales = null
        ) : base(1, camera3dScene, texture)
        {
            base.rects = rects ?? _defaultRects;
            base.masterScales = scales ?? _defaultScales;
        }

        protected override void _prepareSpritesData ()
        {
            throw new NotImplementedException();
        }
    }
}

