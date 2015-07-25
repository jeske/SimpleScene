using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SLaserEmissionFlareObject : SSInstanced2dEffect
    {
        protected SLaser _laser;
        protected int _beamId;

        /// <summary>
        /// "flat" disk that matches its draw scale to compenstate for perspective shrinking
        /// </summary>
        protected readonly SSObjectOcclusionQueuery _beamOccFlatObj;

        /// <summary>
        /// "perspective" disk that that just shrinks when looking from far away due to perspective
        /// </summary>
        protected readonly SSObjectOcclusionQueuery _beamOccPerspObj;

        public SLaserEmissionFlareObject(
            SLaser laser, int beamId, 
            SSScene beamScene, 
            SSObjectOcclusionQueuery occFlat, SSObjectOcclusionQueuery occPersp, 
            SSTexture texture, 
            RectangleF backgroundRect, RectangleF overlayRect)
            : base(2, beamScene, texture)
        {
            this._laser = laser;
            this._beamId = beamId;
            this._beamOccFlatObj = occFlat;
            this._beamOccPerspObj = occPersp;

            this.renderState.alphaBlendingOn = true;
            this.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            this.renderState.blendFactorDest = BlendingFactorDest.One;

            instanceData.writeRect(0, backgroundRect);
            instanceData.writeRect(1, overlayRect);
        }
        protected override void _prepareSpritesData ()
        {
            System.Console.Write("beamId: " + _beamId + " occIntensity = ");


            float occIntensity = 0f;
            if (_beamOccFlatObj != null) {
                // "flat" disk that matches its draw scale to compenstate for perspective shrinking
                float occR = _laser.parameters.occDisk1RadiusPx;
                float occAreaExpected = (float)Math.PI * occR * occR;
                var contribution = (float)_beamOccFlatObj.OcclusionQueueryResult / occAreaExpected;
                contribution = Math.Min(contribution, 1f);
                contribution = (float)Math.Pow(contribution, 3.0);
                contribution *= 0.2f;
                occIntensity += contribution;
                System.Console.Write(contribution.ToString());
            }

            if (_beamOccPerspObj != null) {
                // "perspective" disk that that just shrinks when looking from far away due to perspective
                var contribution = (float)Math.Sqrt((float)_beamOccPerspObj.OcclusionQueueryResult /
                               (_clientRect.Width * _clientRect.Height));
                contribution = (float)Math.Pow(contribution, 0.2);
                contribution *= 0.8f;
                occIntensity += contribution;
                System.Console.Write(" + " + contribution + " = " + occIntensity + "\n");
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

