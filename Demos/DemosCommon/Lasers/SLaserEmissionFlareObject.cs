using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SLaserEmissionFlareObject : SSInstanced2dEffect
    {
        public static SSTexture getDefaultTexture() 
        { 
            return SSAssetManager.GetInstance<SSTextureWithAlpha>("./lasers", "flareOverlay.png");
        }

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
            SSScene camera3dScene, 
            SSObjectOcclusionQueuery occFlat, SSObjectOcclusionQueuery occPersp, 
            SSTexture texture, 
            RectangleF backgroundRect,
            RectangleF overlayRect
        )
            : base(2, camera3dScene, texture ?? getDefaultTexture())
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

        public SLaserEmissionFlareObject(
            SLaser laser, int beamId, 
            SSScene camera3dScene, 
            SSObjectOcclusionQueuery occFlat, SSObjectOcclusionQueuery occPersp, 
            SSTexture texture = null)
            : this(laser, beamId, camera3dScene, occFlat, occPersp, texture,
                new RectangleF(0f, 0f, 1f, 1f), new RectangleF(0f, 0f, 1f, 1f))
        { }

        protected override void _prepareSpritesData ()
        {
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
                //System.Console.Write("beamId: " + _beamId + " occIntensity = " + contribution.ToString());
            }

            if (_beamOccPerspObj != null) {
                // "perspective" disk that that just shrinks when looking from far away due to perspective
                var contribution = (float)Math.Sqrt((float)_beamOccPerspObj.OcclusionQueueryResult /
                               (_clientRect.Width * _clientRect.Height));
                contribution = (float)Math.Pow(contribution, 0.2);
                contribution *= 0.8f;
                occIntensity += contribution;
                //System.Console.Write(" + " + contribution + " = " + occIntensity + "\n");
            }

            var beam = _laser.beam(_beamId);
            var beamStartScreen = worldToScreen(beam.startPos);
            int numElements = instanceData.activeBlockLength;
            var intensity = _laser.envelopeIntensity * beam.periodicIntensity * occIntensity;
            var laserParams = _laser.parameters;
            instanceData.writePosition(0, beamStartScreen);
            instanceData.writePosition(1, beamStartScreen);
            float scale = _laser.parameters.emissionFlareSizeMaxPx * intensity; // * Math.Min (1.5f, 1f / (1f - _occIntensity))
            instanceData.writeMasterScale(0, scale);
            instanceData.writeMasterScale(1, scale * 0.5f); // TODO be able to customize

            var backgroundColor = laserParams.backgroundColor;
            backgroundColor.A = intensity;
            instanceData.writeColor(0, backgroundColor);

            var overlayColor = laserParams.overlayColor;
            overlayColor.A = (float)Math.Pow(occIntensity, 0.5);
            instanceData.writeColor(1, overlayColor);
        }
    }
}

