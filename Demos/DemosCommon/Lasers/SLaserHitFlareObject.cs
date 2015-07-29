using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util3d;

namespace SimpleScene.Demos
{
    public class SLaserHitFlareObject : SSInstanced2dEffect
    {
        protected static SSTexture getDefaultTexture()
        {
            return SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "corona.png");
            //return SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "flareOverlay.png");
            //return SSAssetManager.GetInstance<SSTextureWithAlpha>("./", "sun_flare_debug.png");
        }

        protected static readonly float[] _defaultScales = { 1f, 0.5f };

        protected static readonly RectangleF[] _defaultRects = { 
            new RectangleF(0f, 0f, 1f, 1f),
            new RectangleF(0f, 0f, 1f, 1f)
        };

        protected readonly SLaser _laser;
        protected readonly int _beamId;
        protected readonly int _numSprites;

        public SLaserHitFlareObject (
            SLaser laser, int beamId,
            SSScene camera3dScene,
            SSTexture texture = null,
            RectangleF[] rects = null,
            float[] scales = null
        ) : base(rects != null ? rects.Length : _defaultRects.Length, 
                 camera3dScene, texture ?? getDefaultTexture())
        {
            this.renderState.alphaBlendingOn = true;
            //this.renderState.alphaBlendingOn = false;
            this.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            this.renderState.blendFactorDest = BlendingFactorDest.One;

            base.rects = rects ?? _defaultRects;
            base.masterScales = scales ?? _defaultScales;

            this._laser = laser;
            this._beamId = beamId;

            this.MainColor = Color4.White;
            instanceData.writeRect(0, _defaultRects [0]);
        }

        protected override void _prepareSpritesData ()
        {
            var rc = base.cameraScene3d.renderConfig;
            SSPlane3d nearPlane = new SSPlane3d ();
            nearPlane.A = _viewProjMat3d.M14 + _viewProjMat3d.M13;
            nearPlane.B = _viewProjMat3d.M24 + _viewProjMat3d.M23;
            nearPlane.C = _viewProjMat3d.M34 + _viewProjMat3d.M33;
            nearPlane.D = _viewProjMat3d.M44 + _viewProjMat3d.M43;
            nearPlane.Normalize();

            var beam = _laser.beam(_beamId);
            SSRay laserRay = new SSRay (beam.startPos, beam.direction());

            bool doDrawing = false;
            Vector3 intersectPt3d;
            if (nearPlane.intersects(ref laserRay, out intersectPt3d)) {
                float lengthToIntersectionSq = (intersectPt3d - beam.startPos).LengthSquared;
                float beamLengthSq = beam.lengthSq();
                if (lengthToIntersectionSq  < beamLengthSq) {
                    doDrawing = true;
                    Vector2 drawScreenPos = base.worldToScreen(intersectPt3d);
                    float intensity = _laser.envelopeIntensity * beam.periodicIntensity;
                    Vector2 drawScale = new Vector2 (_laser.parameters.hitFlareSizeMaxPx *
                        (float)Math.Exp(intensity));                   
                    for (int i = 0; i < instanceData.activeBlockLength; ++i) {
                        instanceData.writePosition(i, drawScreenPos);
                        instanceData.writeComponentScale(i, drawScale);
                    }

                    Color4 backgroundColor = _laser.parameters.backgroundColor;
                    backgroundColor.A = intensity;
                    Color4 overlayColor = _laser.parameters.overlayColor;
                    overlayColor.A = (float)Math.Exp(intensity);
                    instanceData.writeColor(0, backgroundColor);
                    instanceData.writeColor(1, overlayColor);
                }
            }

            if (!doDrawing) {
                // hide sprites
                for (int i = 0; i < instanceData.activeBlockLength; ++i) {
                    instanceData.writeComponentScale(i, Vector2.Zero);
                }
            }
           //System.Console.WriteLine("beam id " + _beamId + " hitting screen at xy " + hitPosOnScreen);


        }
    }
}

