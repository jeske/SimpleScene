using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class STrailsRenderer : SSInstancedMeshRenderer
    {
        public delegate Vector3 VelocityFunc ();
        public delegate Vector3 PositionFunc ();

        public class STrailsParameters
        {
            public int capacity = 20;
            public float trailWidth = 20f;
            public float trailsEmissionInterval = 0.5f;
            public string textureFilename = "TODO"; // TODO
        }

        public STrailsData trailsData {
            get { return base.instanceData as STrailsData; }
        }

        public STrailsRenderer(PositionFunc positonFunc, VelocityFunc velocityFunc, 
            STrailsParameters trailParams)
            : base(new STrailsData(positonFunc, velocityFunc, trailParams),
                SSTexturedQuad.DoubleFaceInstance, _defaultUsageHint)
        {
            var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>(trailParams.textureFilename);
            this.textureMaterial = new SSTextureMaterial (tex);
        }

        public class STrailSegment : SSParticle
        {
            public const byte NotConnected = 255;

            public byte prevSegmentIdx = NotConnected;
            public byte nextSegmentIdx = NotConnected;
        }

        public class STrailsData : SSParticleSystemData
        {
            protected byte _headSegmentIdx = STrailSegment.NotConnected;
            protected byte _tailSegmentIdx = STrailSegment.NotConnected;
            protected readonly  byte[] _nextSegmentData = null;
            protected readonly  byte[] _prevSegmentData = null;
            protected readonly SSParticleEmitter _headEmitter;

            public STrailsData(PositionFunc positonFunc, VelocityFunc velocityFunc,
                STrailsParameters trailsParams)
                : base(trailsParams.capacity)
            {
                _nextSegmentData = new byte[capacity];
                _prevSegmentData = new byte[capacity];
                //_headEmitter = new TrailEmitter(positionFunc, velocityFunc, trailParams);
                _headEmitter = new SSRadialEmitter() {
                    particlesPerEmission = 1,
                    emissionInterval = trailsParams.trailsEmissionInterval,
                    masterScale = trailsParams.trailWidth,
                };
            }

            protected override void readParticle (int idx, SSParticle p)
            {
                base.readParticle(idx, p);

                var ts = (STrailSegment)p;
                ts.nextSegmentIdx = _nextSegmentData [idx];
                ts.prevSegmentIdx = _prevSegmentData [idx];
            }

            protected override void writeParticle (int idx, SSParticle p)
            {
                base.writeParticle(idx, p);

                var ts = (STrailSegment)p;

                if (ts.nextSegmentIdx == STrailSegment.NotConnected) {
                    _headSegmentIdx = (byte)idx;
                } else {
                    _prevSegmentData [ts.nextSegmentIdx] = (byte)idx;
                }

                if (ts.prevSegmentIdx == STrailSegment.NotConnected) {
                    _nextIdxToOverwrite = (byte)idx;
                    _tailSegmentIdx = (byte)idx;
                } else {
                    _nextSegmentData [ts.prevSegmentIdx] = (byte)idx;
                }

                _nextSegmentData [idx] = ts.nextSegmentIdx;
                _prevSegmentData [idx] = ts.prevSegmentIdx;
            }


            protected override int storeNewParticle (SSParticle newParticle)
            {
                var ts = (STrailSegment)newParticle;
                ts.nextSegmentIdx = STrailSegment.NotConnected;
                ts.prevSegmentIdx = _headSegmentIdx;

                if (numElements >= capacity) {
                    var oldTailIdx = _tailSegmentIdx;   
                    byte preTail = _nextSegmentData [oldTailIdx];
                    _prevSegmentData [preTail] = STrailSegment.NotConnected;
                    writeParticle(oldTailIdx, newParticle);
                    return oldTailIdx;
                } else {
                    return base.storeNewParticle(newParticle); // is this ok??
                }
            }
        }
    }
}

