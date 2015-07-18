using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public class SimpleLaserFlareEffect : InstancedFlareEffect
    {
        protected SimpleLaser _laser;
        int _beamId;

        public SimpleLaserFlareEffect(SimpleLaser laser, int beamId, SSScene beamScene, SSObjectOcclusionQueuery occObj, 
            SSTexture texture, RectangleF backgroundRect, RectangleF overlayRect)
            : base(beamScene, occObj, texture, new RectangleF[2], new Vector2[2], new Color4[2])
        {
            _laser = laser;
            _beamId = beamId;

            instanceData.spriteSizesU[0] = new SSAttributeFloat(backgroundRect.Width);
            instanceData.spriteSizesV[0] = new SSAttributeFloat(backgroundRect.Height);
            instanceData.spriteOffsetsU[0] = new SSAttributeFloat(backgroundRect.Left);
            instanceData.spriteOffsetsV[0] = new SSAttributeFloat(backgroundRect.Top);

            instanceData.spriteSizesU[1] = new SSAttributeFloat(overlayRect.Width);
            instanceData.spriteSizesV[1] = new SSAttributeFloat(overlayRect.Height);
            instanceData.spriteOffsetsU[1] = new SSAttributeFloat(overlayRect.Left);
            instanceData.spriteOffsetsV[1] = new SSAttributeFloat(overlayRect.Top);

            this.renderState.alphaBlendingOn = true;
            this.renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            this.renderState.blendFactorDest = BlendingFactorDest.One;
        }
        protected override void _updateScreenInstanceData(Vector2 screenCenter, Vector2 screenRect, Vector2 bbPos, Vector2 bbRect)
        {
            var beam = _laser.beam(_beamId);
            int numElements = instanceData.activeBlockLength;
            var intensity = _laser.envelopeIntensity * beam.periodicIntensity;
            var laserParams = _laser.parameters;
            instanceData.positions [0].Value.Xy = bbPos;
            instanceData.positions [1].Value.Xy = bbPos;
            var scale = new Vector2 (laserParams.backgroundWidth * intensity);
            instanceData.componentScalesXY [0] = new SSAttributeVec2(scale);
            instanceData.componentScalesXY [1] = new SSAttributeVec2(scale * 0.5f);
            instanceData.colors [0].Color = Color4Helper.ToUInt32(laserParams.backgroundColor);
            instanceData.colors [1].Color = Color4Helper.ToUInt32(laserParams.overlayColor);

        }
    }
}

