using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SimpleLaserFlareEffect : Instanced2dEffect
    {
        protected SimpleLaser _laser;
        protected int _beamId;

        protected SSObjectOcclusionQueuery _beamOccObj;

        public SimpleLaserFlareEffect(
            SimpleLaser laser, int beamId, 
            SSScene beamScene, SSObjectOcclusionQueuery beamOccDisk, 
            SSTexture texture, 
            RectangleF backgroundRect, RectangleF overlayRect)
            : base(2, beamScene, texture)
        {
            this._laser = laser;
            this._beamId = beamId;
            this._beamOccObj = beamOccDisk;

            this.renderState.alphaBlendingOn = true;
            this.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            this.renderState.blendFactorDest = BlendingFactorDest.One;

            instanceData.writeRect(0, backgroundRect);
            instanceData.writeRect(1, overlayRect);
        }
        protected override void _prepareSpritesData ()
        {
            float occIntensity = 1f;
            if (_beamOccObj != null) {
                float occR = _laser.parameters.occDiskRadiusPx;
                float occAreaExpected = (float)Math.PI * occR * occR;
                occIntensity = (float)_beamOccObj.OcclusionQueueryResult / occAreaExpected;
                occIntensity = Math.Min(occIntensity, 1f);
                occIntensity = (float)Math.Pow(occIntensity, 3.0);
            }

            var beam = _laser.beam(_beamId);
            var beamStartScreen = worldToScreen(beam.startPos);
            int numElements = instanceData.activeBlockLength;
            var intensity = _laser.envelopeIntensity * beam.periodicIntensity * occIntensity;
            var laserParams = _laser.parameters;
            instanceData.writePosition(0, beamStartScreen);
            instanceData.writePosition(1, beamStartScreen);
            float scale = _laser.parameters.flareSizeMaxPx * intensity; // * Math.Min (1.5f, 1f / (1f - _occIntensity))
            instanceData.writeMasterScale(0, scale);
            instanceData.writeMasterScale(1, scale * 0.5f); // TODO customize

            var backgroundColor = laserParams.backgroundColor;
            backgroundColor.A = intensity;
            instanceData.writeColor(0, backgroundColor);

            var overlayColor = laserParams.overlayColor;
            overlayColor.A = (float)Math.Pow(occIntensity, 0.5);
            instanceData.writeColor(1, overlayColor);
        }
    }
}

