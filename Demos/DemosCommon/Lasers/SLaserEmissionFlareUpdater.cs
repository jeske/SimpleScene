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
        //protected readonly SSObjectOcclusionQueuery _beamOccFlatObj;

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
            /*SSObjectOcclusionQueuery occFlat,*/ SSObjectOcclusionQueuery occPersp, 
            RectangleF backgroundRect,
            RectangleF overlayRect
        )
        {
            this._laser = laser;
            this._beamId = beamId;
            //this._beamOccFlatObj = occFlat;
            this._beamOccPerspObj = occPersp;

            this._backgroundRect = backgroundRect;
            this._overlayRect = overlayRect;
        }

        public SLaserEmissionFlareUpdater(
            SLaser laser, int beamId, 
            /*SSObjectOcclusionQueuery occFlat,*/ SSObjectOcclusionQueuery occPersp
        ) 
            : this(laser, beamId, /*occFlat,*/ occPersp,
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

        public void updateSprites(SInstancedSpriteData instanceData, ref RectangleF screenClientRect,
                                  ref Matrix4 camera3dView, ref Matrix4 camera3dProj)
        {
            #if false
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
            #endif

            if (_beamOccPerspObj == null) return;

            var laserParams = _laser.parameters;
            var beam = _laser.beam(_beamId);
            float beamIntensity = _laser.envelopeIntensity * beam.periodicIntensity;

            Matrix4 camera3dViewProjMat = camera3dView * camera3dProj;
            var beamStartScreen = OpenTKHelper.WorldToScreen(beam.startPos, 
                ref camera3dViewProjMat, ref screenClientRect);
            //var beamStartScreen = new Vector2(500f);

            Matrix4 viewInverted = camera3dView.Inverted();
            Vector3 viewRight = Vector3.Transform(Vector3.UnitX, viewInverted).Normalized();
            Vector3 occRightMost = _beamOccPerspObj.Pos + viewRight * laserParams.beamBackgroundWidth;
            Vector2 occRightMostPt = OpenTKHelper.WorldToScreen(occRightMost, 
                ref camera3dViewProjMat, ref screenClientRect);
            Vector2 occCenterPt = OpenTKHelper.WorldToScreen(_beamOccPerspObj.Pos, 
                ref camera3dViewProjMat, ref screenClientRect);
            float screenFullRadius = Math.Abs(occRightMostPt.X - occCenterPt.X);


            int numElements = instanceData.activeBlockLength;

            instanceData.writePosition(_backgroundSpriteIdx, beamStartScreen);
            instanceData.writePosition(_overlaySpriteIdx, beamStartScreen);

            float scale = Math.Max(laserParams.emissionFlareScreenSizeMin, screenFullRadius * 4f);
            instanceData.writeMasterScale(_backgroundSpriteIdx, scale * 1.2f);
            instanceData.writeMasterScale(_overlaySpriteIdx, scale * 1f); // TODO be able to customize
            //instanceData.writeMasterScale(_backgroundSpriteIdx, 10f);
            //instanceData.writeMasterScale(_overlaySpriteIdx, 10f);
            System.Console.WriteLine("sprite size = " + scale);

            instanceData.writeOrientationZ(_backgroundSpriteIdx, beamIntensity * 2f * (float)Math.PI * 4f);
            instanceData.writeOrientationZ(_overlaySpriteIdx, beamIntensity * 2f * (float)Math.PI * 4f);


            // color intensity - depends on the dot product between to-camera vector and beam direction

            float maxScreenArea = (float)Math.PI * laserParams.occDisk1RadiusPx * laserParams.occDisk1RadiusPx;
            float screenAreaUsed = (float)_beamOccPerspObj.OcclusionQueueryResult;
            float occDiskAreaRatio = screenAreaUsed / maxScreenArea;

            Vector3 toCamera = (Vector3.Transform(Vector3.Zero, camera3dView.Inverted()) - beam.startPos)
                .Normalized();
            //Vector3 cameraDir = Vector3.Transform(-Vector3.UnitZ, camera3dView.Inverted()).Normalized();
            float dot = Vector3.Dot(toCamera, beam.direction());

            //float occColorRatio = (float)Math.Pow(roughScreenRadiusUsed / screenFullRadius, 0.5f);
            float occColorRatioBackground = (float)Math.Pow(Math.Max(0f, dot), 1.0) * 2f * occDiskAreaRatio;
            float occColorRatioOverlay = (float)Math.Pow(Math.Max(0f, dot), 0.5) * 2f * occDiskAreaRatio;

            var colorIntensityBackground = beamIntensity * occColorRatioBackground;
            var colorIntensityOverlay = (float)Math.Pow(beamIntensity, 0.4) * occColorRatioOverlay;
            System.Console.WriteLine("color intensity = " + colorIntensityBackground + " / " + colorIntensityBackground);


            var backgroundColor = laserParams.backgroundColor;
            backgroundColor.A = Math.Min(colorIntensityBackground, 1f);
            instanceData.writeColor(_backgroundSpriteIdx, backgroundColor);

            var overlayColor = laserParams.overlayColor;
            overlayColor.A = Math.Min(colorIntensityOverlay, 1f);
            instanceData.writeColor(_overlaySpriteIdx, overlayColor);
        }
    }
}

