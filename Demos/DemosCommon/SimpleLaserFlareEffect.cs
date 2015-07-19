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
        int _beamId;

        public SimpleLaserFlareEffect(
            SimpleLaser laser, int beamId, 
            SSScene beamScene, SSObjectOcclusionQueuery beamOccDisk, 
            SSTexture texture, 
            RectangleF backgroundRect, RectangleF overlayRect)
            : base(2, beamScene, texture, beamOccDisk)
        {
            _laser = laser;
            _beamId = beamId;

            this.renderState.alphaBlendingOn = true;
            this.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            this.renderState.blendFactorDest = BlendingFactorDest.One;

            instanceData.writeRect(0, backgroundRect);
            instanceData.writeRect(1, overlayRect);
        }
        protected override void _prepareSpritesData ()
        {
            GL.Color4(Color4.White);

            var beam = _laser.beam(_beamId);
            var beamStartScreen = worldToScreen(beam.startPos);
            int numElements = instanceData.activeBlockLength;
            var intensity = _laser.envelopeIntensity * beam.periodicIntensity * _occIntensity;
            var laserParams = _laser.parameters;
            instanceData.writePosition(0, beamStartScreen);
            instanceData.writePosition(1, beamStartScreen);
            float scale = _laser.parameters.flareSizePx * intensity; // * Math.Min (1.5f, 1f / (1f - _occIntensity))
            instanceData.writeMasterScale(0, scale);
            instanceData.writeMasterScale(1, scale * 0.5f); // TODO customize
            instanceData.writeColor(0, laserParams.backgroundColor);
            instanceData.writeColor(1, laserParams.overlayColor);
        }
    }
}

