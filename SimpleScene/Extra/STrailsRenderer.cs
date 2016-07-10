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
        public delegate Vector3 DirFunc();

        public class STrailsParameters
        {
            public int capacity = 20;
            public float trailWidth = 20f;
            public float trailsEmissionInterval = 0.5f;
            public float velocityToLengthFactor = 1f;
            public float trailCutoffVelocity = 0.1f;
            public string textureFilename = "trail_debug.png";
            //public string textureFilename = "trail.png";

            // default value
            public STrailsParameters()
            {
            }
        }

        public STrailsData trailsData {
            get { return base.instanceData as STrailsData; }
        }

        public STrailsRenderer(PositionFunc positonFunc, VelocityFunc velocityFunc, DirFunc fwdDirFunc,
            STrailsParameters trailsParams = null)
            : base(new STrailsData(positonFunc, velocityFunc, fwdDirFunc, 
                trailsParams ?? new STrailsParameters()),
                SSTexturedQuad.DoubleFaceInstance, _defaultUsageHint)
        {
            trailsParams = trailsData.trailsParams;
            var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>(trailsParams.textureFilename);

            renderState.castsShadow = false;
            renderState.receivesShadows = false;
            renderState.doBillboarding = false;
            renderState.alphaBlendingOn = true;
            //renderState.depthTest = true;
            renderState.depthTest = true;
            renderState.depthWrite = false;
            renderState.lighted = false;
            simulateOnUpdate = true;

            colorMaterial = SSColorMaterial.pureAmbient;
            textureMaterial = new SSTextureMaterial (diffuse: tex);
            Name = "simple trails renderer";
        }

        public class STrailsData : SSParticleSystemData
        {
            public readonly STrailsParameters trailsParams;

            protected byte _headSegmentIdx = STrailsSegment.NotConnected;
            protected byte _tailSegmentIdx = STrailsSegment.NotConnected;
            protected readonly  byte[] _nextSegmentData = null;
            protected readonly  byte[] _prevSegmentData = null;
            //protected readonly SSParticleEmitter _headEmitter;

            public STrailsData(PositionFunc positionFunc, VelocityFunc velocityFunc, DirFunc fwdDirFunc,
                STrailsParameters trailsParams = null)
                : base(trailsParams.capacity)
            {
                this.trailsParams = trailsParams;

                _nextSegmentData = new byte[capacity];
                _prevSegmentData = new byte[capacity];
                //_headEmitter = new TrailEmitter(positionFunc, velocityFunc, trailParams);

                addEmitter(new STrailsEmitter(trailsParams, positionFunc, velocityFunc, fwdDirFunc));
                addEffector(new SRadialBillboardOrientator());
            }

            protected override SSParticle createNewParticle ()
            {
                return new STrailsSegment ();
            }

            protected override void readParticle (int idx, SSParticle p)
            {
                base.readParticle(idx, p);

                var ts = (STrailsSegment)p;
                ts.nextSegmentIdx = _nextSegmentData [idx];
                ts.prevSegmentIdx = _prevSegmentData [idx];
            }

            protected override void writeParticle (int idx, SSParticle p)
            {
                base.writeParticle(idx, p);

                var ts = (STrailsSegment)p;

                if (ts.nextSegmentIdx == STrailsSegment.NotConnected) {
                    // make head
                    _headSegmentIdx = (byte)idx;
                } else {
                    // update connection from the next segment
                    _prevSegmentData [ts.nextSegmentIdx] = (byte)idx;
                }

                if (ts.prevSegmentIdx == STrailsSegment.NotConnected) {
                    // make tail
                    _nextIdxToOverwrite = idx;
                    _tailSegmentIdx = (byte)idx;
                } else {
                    // update connection from the previous segment
                    _nextSegmentData [ts.prevSegmentIdx] = (byte)idx;
                }

                if (_nextSegmentData != null) {
                    _nextSegmentData [idx] = ts.nextSegmentIdx;
                }
                if (_prevSegmentData != null) {
                    _prevSegmentData [idx] = ts.prevSegmentIdx;
                }
            }


            protected override int storeNewParticle (SSParticle newParticle)
            {
                var ts = (STrailsSegment)newParticle;
                ts.nextSegmentIdx = STrailsSegment.NotConnected;
                ts.prevSegmentIdx = _headSegmentIdx;

                // TODO you can set scale here based on velocity

                if (numElements >= capacity) {
                    var oldTailIdx = _tailSegmentIdx;
                    // pre-tail is now tail
                    byte preTail = _nextSegmentData [oldTailIdx];
                    _prevSegmentData [preTail] = STrailsSegment.NotConnected;
                    _tailSegmentIdx = preTail;
                    // old trail will get overwritten in base.storeNewParticle()
                    _nextIdxToOverwrite = oldTailIdx;
                }

                return base.storeNewParticle(newParticle);
            }

            public class STrailsSegment : SSParticle
            {
                public const byte NotConnected = 255;

                public byte prevSegmentIdx = NotConnected;
                public byte nextSegmentIdx = NotConnected;
            }

            public class STrailsEmitter : SSParticleEmitter
            {
                public PositionFunc posFunc;
                public VelocityFunc velFunc;
                public DirFunc fwdDirFunc;
                public STrailsParameters trailParams;
                public float velocityToScaleFactor = 1f; 

                public STrailsEmitter(STrailsParameters tParams, 
                    PositionFunc posFunc, VelocityFunc velFunc, DirFunc fwdDirFunc)
                {
                    this.trailParams = tParams;
                    this.posFunc = posFunc;
                    this.velFunc = velFunc;
                    this.fwdDirFunc = fwdDirFunc;
                }

                protected override void emitParticles (int particleCount, ParticleFactory factory, ReceiverHandler receiver)
                {
                    // don't emit particles when the motion is slow/wrong direction relative to "forward"
                    Vector3 vel = velFunc();
                    Vector3 dir = fwdDirFunc();
                    float relVelocity = Vector3.Dot(vel, dir);
                    if (relVelocity < trailParams.trailCutoffVelocity) {
                        return;
                    }

                    base.emitParticles(particleCount, factory, receiver);
                }

                protected override void configureNewParticle (SSParticle p)
                {
                    base.configureNewParticle(p);

                    var velocity = this.velFunc();

                    p.pos = posFunc();
                    p.componentScale = new Vector3 (
                        trailParams.velocityToLengthFactor * velocity.LengthFast,
                        trailParams.trailWidth,
                        1f);
                }
            }
        }
    }
}


