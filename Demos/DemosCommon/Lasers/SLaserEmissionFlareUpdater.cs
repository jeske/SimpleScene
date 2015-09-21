using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SLaserEmissionFlareUpdater : ISSpriteUpdater
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

        protected readonly RectangleF _backgroundRect;
        protected readonly RectangleF _overlayRect;
        protected int _backgroundSpriteIdx;
        protected int _overlaySpriteIdx;


        public SLaserEmissionFlareUpdater(
            SLaser laser, int beamId, 
            SSObjectOcclusionQueuery occFlat, SSObjectOcclusionQueuery occPersp, 
            RectangleF backgroundRect,
            RectangleF overlayRect
        )
        {
            this._laser = laser;
            this._beamId = beamId;
            this._beamOccFlatObj = occFlat;
            this._beamOccPerspObj = occPersp;

            this._backgroundRect = backgroundRect;
            this._overlayRect = overlayRect;
        }

        public SLaserEmissionFlareUpdater(
            SLaser laser, int beamId, 
            SSObjectOcclusionQueuery occFlat, SSObjectOcclusionQueuery occPersp
        ) 
            : this(laser, beamId, occFlat, occPersp,
                new RectangleF(0f, 0.5f, 0.5f, 0.5f), new RectangleF(0f, 0.5f, 0.5f, 0.5f))
        { }

        public void setupSprites(SInstancedSpriteData instanceData)
        {
            _backgroundSpriteIdx = instanceData.requestSlot();
            _overlaySpriteIdx = instanceData.requestSlot();

            instanceData.writeRect(_backgroundSpriteIdx, _backgroundRect);
            instanceData.writeRect(_overlaySpriteIdx, _overlayRect);

        }

        public void releaseSprites(SInstancedSpriteData instanceData)
        {
            instanceData.releaseSlot(_backgroundSpriteIdx);
            instanceData.releaseSlot(_overlaySpriteIdx);
        }

        public void updateSprites(SInstancedSpriteData instanceData, ref RectangleF clientRect,
                                  ref Matrix4 camera3dView, ref Matrix4 camera3dProj)
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
                               (clientRect.Width * clientRect.Height));
                contribution = (float)Math.Pow(contribution, 0.2);
                contribution *= 0.8f;
                occIntensity += contribution;
                //System.Console.Write(" + " + contribution + " = " + occIntensity + "\n");
            }

            var beam = _laser.beam(_beamId);
            Matrix4 camera3dViewProjMat = camera3dView * camera3dProj;
            var beamStartScreen = OpenTKHelper.WorldToScreen(beam.startPos, ref camera3dViewProjMat, ref clientRect);
            //var beamStartScreen = new Vector2(500f);
            int numElements = instanceData.activeBlockLength;
            var intensity = _laser.envelopeIntensity * beam.periodicIntensity * occIntensity;
            var laserParams = _laser.parameters;
            instanceData.writePosition(_backgroundSpriteIdx, beamStartScreen);
            instanceData.writePosition(_overlaySpriteIdx, beamStartScreen);
            float scale = _laser.parameters.emissionFlareSizeMaxPx * intensity; // * Math.Min (1.5f, 1f / (1f - _occIntensity))
            instanceData.writeMasterScale(_backgroundSpriteIdx, scale);
            instanceData.writeMasterScale(_overlaySpriteIdx, scale * 0.5f); // TODO be able to customize

            var backgroundColor = laserParams.backgroundColor;
            backgroundColor.A = intensity;
            instanceData.writeColor(_backgroundSpriteIdx, backgroundColor);

            var overlayColor = laserParams.overlayColor;
            overlayColor.A = (float)Math.Pow(occIntensity, 0.5);
            instanceData.writeColor(_overlaySpriteIdx, overlayColor);
        }
    }
}

