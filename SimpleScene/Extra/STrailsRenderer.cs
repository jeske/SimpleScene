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
            public int capacity = 2000;
            public float trailWidth = 5f;
            public float trailsEmissionInterval = 0.05f;
            public float velocityToLengthFactor = 1f;
            public float trailLifetime = 20f;
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
                SSTexturedQuad.DoubleFaceCrossBarInstance, _defaultUsageHint)
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

            // TODO this is kind of heavy heanded. try a few alternatives with bounding spheres
            renderState.frustumCulling = false;

            colorMaterial =         SSColorMaterial.pureAmbient;
            textureMaterial = new SSTextureMaterial (diffuse: tex);
            Name = "simple trails renderer";
        }

        public override void Render (SSRenderConfig renderConfig)
        {
            // a hack to draw segment particles in viewspace
            this.worldMat = renderConfig.invCameraViewMatrix.Inverted();

            base.Render(renderConfig);
        }

        public class STrailsData : SSParticleSystemData
        {
            public readonly STrailsParameters trailsParams;

            protected byte _headSegmentIdx = STrailsSegment.NotConnected;
            protected byte _tailSegmentIdx = STrailsSegment.NotConnected;
            protected readonly byte[] _nextSegmentData = null;
            protected readonly byte[] _prevSegmentData = null;
            protected readonly Vector3[] _motionVecs = null;
            protected readonly Vector3[] _worldCoords = null;
            protected readonly STrailUpdater _updater;
            //protected readonly SSParticleEmitter _headEmitter;

            public STrailsData(PositionFunc positionFunc, VelocityFunc velocityFunc, DirFunc fwdDirFunc,
                STrailsParameters trailsParams = null)
                : base(trailsParams.capacity)
            {
                this.trailsParams = trailsParams;

                _worldCoords = new Vector3[capacity];
                _motionVecs = new Vector3[capacity];
                _nextSegmentData = new byte[capacity];
                _prevSegmentData = new byte[capacity];

                //_headEmitter = new TrailEmitter(positionFunc, velocityFunc, trailParams);

                addEmitter(new STrailsEmitter(trailsParams, positionFunc, velocityFunc, fwdDirFunc));

                _updater = new STrailUpdater(trailsParams);
                addEffector(_updater);
            }

            protected override void simulateStep ()
            {
                base.simulateStep();


            }

            public override void updateCamera (ref Matrix4 model, ref Matrix4 view, ref Matrix4 projection)
            {
                _updater.updateViewMatrix(ref view);
            }

            protected override SSParticle createNewParticle ()
            {
                return new STrailsSegment ();
            }

            protected override void readParticle (int idx, SSParticle p)
            {
                base.readParticle(idx, p);

                var ts = (STrailsSegment)p;
                ts.worldPos = _worldCoords [idx];
                ts.motionVec = _motionVecs [idx];
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

                if (_worldCoords != null) {
                    _worldCoords [idx] = ts.worldPos;
                }
                if (_motionVecs != null) {
                    _motionVecs [idx] = ts.motionVec;
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

                public Vector3 worldPos = Vector3.Zero;
                public Vector3 motionVec = -Vector3.UnitZ;
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

                    base.life = tParams.trailLifetime;
                    base.emissionInterval = tParams.trailsEmissionInterval;
                    base.velocity = Vector3.Zero;
                    base.color = new Color4(1f, 1f, 1f, 0.3f);
                }

                #if false
                protected override void emitParticles (int particleCount, ParticleFactory factory, ReceiverHandler receiver)
                {
                    // don't emit particles when the motion is slow/wrong direction relative to "forward"
                    Vector3 vel = velFunc();
                    Vector3 dir = fwdDirFunc();gim
                    float relVelocity = Vector3.Dot(vel, dir);
                    if (relVelocity < trailParams.trailCutoffVelocity) {
                        return;
                    }

                    base.emitParticles(particleCount, factory, receiver);
                }
                #endif

                protected override void configureNewParticle (SSParticle p)
                {
                    base.configureNewParticle(p);


                    var ts = (STrailsSegment)p;
                    var velocity = this.velFunc();
                    //var dir 

                    //ts.billboardXY = true;
                    ts.pos = Vector3.Zero;
                    ts.worldPos = posFunc();
                    ts.motionVec = velocity;
                    ts.componentScale.Y = trailParams.trailWidth;
                    ts.componentScale.Z = trailParams.trailWidth;

                    /*
                    ts.componentScale = new Vector3 (
                        trailParams.velocityToLengthFactor * velocity.LengthFast,
                        trailParams.trailWidth,
                        1f);
                        */

                    #if false
                    //var dir = -1f * fwdDirFunc().Normalized(); 
                    var dir = velocity.Normalized();

                    float theta = (float)Math.Atan2(dir.Y, dir.X);
                    float xy = (float)Math.Sqrt(dir.X * dir.X * dir.Y * dir.Y);
                    float phi = (float)Math.Atan2(dir.Z, xy);

                    //ts.orientation.X = -(float)Math.PI / 2f;
                    //ts.orientation.Z = theta;
                    //ts.orientation.Y = phi;

                    //ts.orientation.X = 0f;
                    //ts.orientation.Y = -phi;
                    ts.orientation.Z = theta;
                    #endif

                    //Console.WriteLine("orientation =  " + ts.orientation);

                }
            }

            public class STrailUpdater : SSParticleEffector
            {
                //protected Vector3 _cameraX = Vector3.UnitX;
                //protected Vector3 _cameraY = Vector3.UnitY;
                protected Matrix4 _viewMat = Matrix4.Identity;

                public STrailsParameters trailsParams;

                public STrailUpdater(STrailsParameters trailParams)
                {
                    this.trailsParams = trailParams;
                }

                public void updateViewMatrix(ref Matrix4 viewMat)
                {
                    _viewMat = viewMat;
                    //_cameraX = Vector3.Transform(Vector3.UnitX, modelView);
                    //_cameraY = Vector3.Transform(Vector3.UnitY, modelView);
                }

                protected override void effectParticle (SSParticle particle, float deltaT)
                {
                    var ts = (STrailsSegment)particle;

                    Vector3 centerView = Vector3.Transform(ts.worldPos, _viewMat);
                    Vector3 endView = Vector3.Transform(
                        ts.worldPos + ts.motionVec * trailsParams.velocityToLengthFactor, _viewMat);
                    Vector3 motionView = endView - centerView;

                    float motionViewXy = motionView.Xy.LengthFast;

                    //ts.pos = Vector3.Zero;
                    ts.pos = centerView; // draw in view space
                    ts.componentScale.X = motionView.LengthFast;
                    ts.orientation.Z = (float)Math.Atan2(motionView.Y, motionView.X);
                    ts.orientation.Y = -(float)Math.Atan2(motionView.Z, motionViewXy);
                }
            }
        }
    }
}


