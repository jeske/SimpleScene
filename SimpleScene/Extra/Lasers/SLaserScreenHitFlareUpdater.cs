﻿using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util3d;

namespace SimpleScene.Demos
{
    public class SLaserScreenHitFlareUpdater : ISSpriteUpdater
    {
        public enum SpriteId : int { coronaBackground=0, coronaOverlay=1, ring1=2, ring2=3 };

        protected static readonly float[] _defaultMasterScales = { 
            4f,     // corona background
            0.5f,   // corona overlay
            0.275f, // ring 1
            0.25f   // ring 2
        };

        protected static readonly RectangleF[] _defaultRects = { 
            new RectangleF(0.5f, 0f, 0.5f, 0.5f),
            new RectangleF(0.5f, 0f, 0.5f, 0.5f),
            new RectangleF(0f, 0f, 0.5f, 0.5f),
            new RectangleF(0f, 0f, 0.5f, 0.5f),
        };

        protected readonly SLaser _laser;
        protected readonly int _beamId;
        protected readonly RectangleF[] _rects;
        protected readonly float[] _masterScales;
        protected int[] _spriteSlotIdxs;

        public SLaserScreenHitFlareUpdater (
            SLaser laser, int beamId,
            SSScene camera3dScene,
            RectangleF[] rects = null,
            float[] scales = null
        ) 
        {
            _rects = rects ?? _defaultRects;
            _masterScales = scales ?? _defaultMasterScales;
            this._laser = laser;
            this._beamId = beamId;
        }

        public void setupSprites (SInstancedSpriteData instanceData)
        {
            int numElements = Math.Max(_rects.Length, _masterScales.Length);
            _spriteSlotIdxs = instanceData.requestSlots(numElements);

            for (int i = 0; i < _rects.Length; ++i) {
                instanceData.writeRect(_spriteSlotIdxs [i], _rects [i]);
            }
            for (int i = 0; i < _masterScales.Length; ++i) {
                instanceData.writeMasterScale(_spriteSlotIdxs [i], _masterScales [i]);
            }
        }

        public void releaseSprites(SInstancedSpriteData instanceData)
        {
            instanceData.releaseSlots(_spriteSlotIdxs);
        }

        public void updateSprites(SInstancedSpriteData instanceData, ref RectangleF clientRect,
                                  ref Vector3 cameraPos, ref Matrix4 camera3dView, ref Matrix4 camera3dProj)
        {
            var beam = _laser.beam(_beamId);
            var ray = beam.rayWorld();
            var laserParams = _laser.parameters;
            //var beamSrcInViewSpace = Vector3.Transform(beam.startPosWorld, camera3dView);

            //some_name code start 24112019
            Vector3 beamSrcInViewSpace = (new Vector4(beam.startPosWorld, 1) * camera3dView).Xyz;
            //some_name code end


            bool hideSprites = true;
            Vector3 intersectPt3d;
            if (beamSrcInViewSpace.Z < 0f) {
                Matrix4 cameraViewProjMat3d = camera3dView * camera3dProj;
                // extract a near plane from the camera view projection matrix
                // https://www.opengl.org/discussion_boards/showthread.php/159332-Extract-clip-planes-from-projection-matrix
                SSPlane3d nearPlane = new SSPlane3d () {
                    A = cameraViewProjMat3d.M14 + cameraViewProjMat3d.M13,
                    B = cameraViewProjMat3d.M24 + cameraViewProjMat3d.M23,
                    C = cameraViewProjMat3d.M34 + cameraViewProjMat3d.M33,
                    D = cameraViewProjMat3d.M44 + cameraViewProjMat3d.M43
                };

                if (nearPlane.intersects(ref ray, out intersectPt3d)) {
                    #if false
                    Console.WriteLine("camera at world pos = " + cameraPos);
                    Console.WriteLine("camera in screen pos = " + OpenTKHelper.WorldToScreen(
                        cameraPos, ref cameraViewProjMat3d, ref clientRect));
                    Console.WriteLine("screen hit at world pos = " + intersectPt3d);
                    #endif

                    float lengthToIntersectionSq = 
                        (intersectPt3d - beam.startPosWorld).LengthSquared;
                    float beamLengthSq = beam.lengthSqWorld();
                    if (lengthToIntersectionSq  < beamLengthSq) {
                        hideSprites = false;
                        Vector2 drawScreenPos = OpenTKHelper.WorldToScreen(
                            intersectPt3d - ray.dir * 1f, ref cameraViewProjMat3d, ref clientRect);
                        //Console.WriteLine("screen hit at screen pos = " + drawScreenPos);
                        float intensity = _laser.envelopeIntensity * beam.periodicIntensity;
                        Vector2 drawScale 
                            = new Vector2 (laserParams.hitFlareSizeMaxPx * (float)Math.Exp(intensity));
                        for (int i = 0; i < _spriteSlotIdxs.Length; ++i) {
                            int writeIdx = _spriteSlotIdxs [i];
                            instanceData.writePosition(writeIdx, drawScreenPos);
                            instanceData.writeComponentScale(writeIdx, drawScale);
                            instanceData.writeOrientationZ(writeIdx, intensity * 2f * (float)Math.PI);
                        }

                        Color4 backgroundColor = laserParams.backgroundColor;
                        backgroundColor.A = intensity;
                        instanceData.writeColor(_spriteSlotIdxs[(int)SpriteId.coronaBackground], backgroundColor);

                        Color4 overlayColor = laserParams.overlayColor;
                        //overlayColor.A = intensity / _laser.parameters.intensityEnvelope.sustainLevel;
                        overlayColor.A = Math.Min(intensity * 2f, 1f);
                        instanceData.writeColor(_spriteSlotIdxs[(int)SpriteId.coronaOverlay], overlayColor);
                        //System.Console.WriteLine("overlay.alpha == " + overlayColor.A);

                        Color4 ring1Color = laserParams.overlayColor;
                        //ring1Color.A = (float)Math.Pow(intensity, 5.0);
                        ring1Color.A = 0.1f * intensity;
                        instanceData.writeComponentScale(_spriteSlotIdxs[(int)SpriteId.ring1], 
                            drawScale * (float)Math.Exp(intensity));
                        instanceData.writeColor(_spriteSlotIdxs[(int)SpriteId.ring1], ring1Color);
                        //instanceData.writeColor(_spriteSlotIdxs[(int)SpriteId.ring1], Color4.LimeGreen);

                        Color4 ring2Color = laserParams.backgroundColor;
                        //ring2Color.A = (float)Math.Pow(intensity, 10.0);
                        ring2Color.A = intensity * 0.1f;
                        instanceData.writeColor(_spriteSlotIdxs[(int)SpriteId.ring2], ring2Color);
                        //instanceData.writeColor(_spriteSlotIdxs[(int)SpriteId.ring2], Color4.Magenta);
                    }
                }
            }

            if (hideSprites) {
                // hide sprites
                for (int i = 0; i < _spriteSlotIdxs.Length; ++i) {
                    instanceData.writeComponentScale(_spriteSlotIdxs[i], Vector2.Zero);
                }
            }
           //System.Console.WriteLine("beam id " + _beamId + " hitting screen at xy " + hitPosOnScreen);
        }
    }
}

