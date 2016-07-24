//#define TRAILS_DEBUG

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util;

namespace SimpleScene
{
    public class STrailsRenderer : SSInstancedMeshRenderer
    {
        public delegate Vector3 VelocityFunc ();
        public delegate Vector3 PositionFunc ();
        public delegate Vector3 DirFunc();


        public class STrailsParameters
        {
            public int capacity = 200;
            public float trailWidth = 5f;
            //public float trailsEmissionInterval = 0.05f;
            public float trailsEmissionInterval = 0.15f;
            public float velocityToLengthFactor = 0.4f;
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
            get { return (STrailsData)base.instanceData; }
        }

        protected SSInstancedCylinderShaderProgram _shader;
        protected SSAttributeBuffer<SSAttributeVec3> _axesBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _widthsBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _lengthsBuffer;

        public STrailsRenderer(PositionFunc positonFunc, VelocityFunc velocityFunc, DirFunc fwdDirFunc,
            STrailsParameters trailsParams = null)
            : base(new STrailsData(positonFunc, velocityFunc, fwdDirFunc, 
                trailsParams ?? new STrailsParameters()),
                SSTexturedQuad.singleFaceInstance, _defaultUsageHint)
        {
            trailsParams = trailsData.trailsParams;
            var tex = SSAssetManager.GetInstance<SSTextureWithAlpha>(trailsParams.textureFilename);

            renderState.castsShadow = false;
            renderState.receivesShadows = false;
            renderState.doBillboarding = false;
            renderState.alphaBlendingOn = true;
            //renderState.alphaBlendingOn = false;
            renderState.depthTest = true;
            renderState.depthWrite = false;
            renderState.lighted = false;
            simulateOnUpdate = true;

            // TODO this is kind of heavy heanded. try a few alternatives with bounding spheres
            renderState.frustumCulling = false;

            colorMaterial = SSColorMaterial.pureAmbient;
            textureMaterial = new SSTextureMaterial (diffuse: tex);
            Name = "simple trails renderer";

            this.renderMode = RenderMode.GpuInstancing;
        }

        protected override void _initAttributeBuffers (BufferUsageHint hint)
        {
            _posBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
            _axesBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
            _widthsBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
            _lengthsBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
            _colorBuffer = new SSAttributeBuffer<SSAttributeColor> (hint);
        }

        protected override void _prepareInstanceShader (SSRenderConfig renderConfig)
        {
            _shader = _shader ?? (SSInstancedCylinderShaderProgram)renderConfig.otherShaders["instanced_cylinder"];
            _shader.Activate();

            Vector3 viewRay = Vector3.Transform(-Vector3.UnitZ, 
                renderConfig.invCameraViewMatrix.ExtractRotation().Inverted());
            GL.Uniform3(_shader.UniViewRay, viewRay);

            _prepareAttribute(_posBuffer, _shader.AttrCylinderPos, trailsData.positions);
            _prepareAttribute(_axesBuffer, _shader.AttrCylinderAxis, trailsData.cylinderAxes);
            _prepareAttribute(_lengthsBuffer, _shader.AttrCylinderLength, trailsData.cylinderLengths);
            _prepareAttribute(_widthsBuffer, _shader.AttrCylinderWidth, trailsData.cylinderWidth);
            _prepareAttribute(_colorBuffer, _shader.AttrCylinderColor, trailsData.colors);

        }

        public class STrailsData : SSParticleSystemData
        {
            public readonly STrailsParameters trailsParams;

            public SSAttributeVec3[] cylinderAxes { get { return _cylAxes; } }
            public SSAttributeFloat[] cylinderLengths { get { return _cylLengths; } }
            public SSAttributeFloat[] cylinderWidth { get { return _cylWidths; } }

            protected byte _headSegmentIdx = STrailsSegment.NotConnected;
            protected byte _tailSegmentIdx = STrailsSegment.NotConnected;
            protected byte[] _nextSegmentData = null;
            protected byte[] _prevSegmentData = null;
            protected SSAttributeVec3[] _cylAxes = null;
            protected SSAttributeFloat[] _cylLengths = null;
            protected SSAttributeFloat[] _cylWidths = null;
            protected readonly STrailUpdater _updater;

            protected PositionFunc positionFunc;
            protected VelocityFunc velocityFunc;
            //protected readonly SSParticleEmitter _headEmitter;

            public STrailsData(PositionFunc positionFunc, VelocityFunc velocityFunc, DirFunc fwdDirFunc,
                STrailsParameters trailsParams = null)
                : base(trailsParams.capacity)
            {
                this.trailsParams = trailsParams;
                this.positionFunc = positionFunc;
                this.velocityFunc = velocityFunc;

                //_headEmitter = new TrailEmitter(positionFunc, velocityFunc, trailParams);

                addEmitter(new STrailsEmitter(trailsParams, positionFunc, velocityFunc, fwdDirFunc));

                _updater = new STrailUpdater(trailsParams);
                addEffector(_updater);
            }

            protected override void initArrays ()
            {
                base.initArrays();

                _cylAxes = new SSAttributeVec3[1];
                _cylLengths = new SSAttributeFloat[1];
                _cylWidths = new SSAttributeFloat[1];
                _nextSegmentData = new byte[1];
                _prevSegmentData = new byte[1];
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
                ts.cylAxis = _readElement(_cylAxes, idx).Value;
                ts.cylWidth = _readElement(_cylWidths, idx).Value;
                ts.cylLendth = _readElement(_cylLengths, idx).Value;
                ts.nextSegmentIdx = _readElement(_nextSegmentData, idx);
                ts.prevSegmentIdx = _readElement(_prevSegmentData, idx);
            }

            protected override void writeParticle (int idx, SSParticle p)
            {
                #if false
                _lives [idx] = p.life;
                writeDataIfNeeded(ref _positions, idx, new SSAttributeVec3(p.pos));
                writeDataIfNeeded(ref _viewDepths, idx, p.viewDepth);
                writeDataIfNeeded(ref _effectorMasksHigh, idx, (byte)((p.effectorMask & 0xFF00) >> 8));
                writeDataIfNeeded(ref _effectorMasksLow, idx, (byte)(p.effectorMask & 0xFF));
                writeDataIfNeeded(ref _colors, idx, new SSAttributeColor(Color4Helper.ToUInt32(p.color)));
                #else
                base.writeParticle(idx, p);
                #endif


                var ts = (STrailsSegment)p;

                #if false
                if (ts.nextSegmentIdx == STrailsSegment.NotConnected) {
                    // make head
                    _headSegmentIdx = (byte)idx;
                } else {
                    // update connection from the next segment
                    writeDataIfNeeded(ref _prevSegmentData, ts.nextSegmentIdx, (byte)idx);
                }

                if (ts.prevSegmentIdx == STrailsSegment.NotConnected) {
                    // make tail
                    _nextIdxToOverwrite = idx;
                    _tailSegmentIdx = (byte)idx;
                } else {
                    // update connection from the previous segment
                    writeDataIfNeeded(ref _nextSegmentData, ts.prevSegmentIdx, (byte)idx);
                }
                #endif

                writeDataIfNeeded(ref _cylAxes, idx, new SSAttributeVec3(ts.cylAxis));
                writeDataIfNeeded(ref _cylLengths, idx, new SSAttributeFloat(ts.cylLendth));
                writeDataIfNeeded(ref _cylWidths, idx, new SSAttributeFloat(ts.cylWidth));
                writeDataIfNeeded(ref _nextSegmentData, idx, ts.nextSegmentIdx);
                writeDataIfNeeded(ref _prevSegmentData, idx, ts.prevSegmentIdx);
            }

            protected override void particleSwap (int leftIdx, int rightIdx)
            {
                if (leftIdx == rightIdx) {
                    return;
                }

                byte leftPrev = _readElement(_prevSegmentData, leftIdx);
                byte leftNext = _readElement(_nextSegmentData, leftIdx);
                byte rightPrev = _readElement(_prevSegmentData, rightIdx);
                byte rightNext = _readElement(_nextSegmentData, rightIdx);

                if (rightPrev == leftIdx) { // simplify special case
                    particleSwap(rightIdx, leftIdx);
                    return;
                }

                #if TRAILS_DEBUG
                Console.Write("before swap " + leftIdx + " and " + rightIdx + ": ");
                printTree();
                #endif

                base.particleSwap(leftIdx, rightIdx);


                // works for both
                if (leftNext != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _prevSegmentData, leftNext, (byte)rightIdx);
                }
                if (rightPrev != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _nextSegmentData, rightPrev, (byte)leftIdx);
                }

                if (leftPrev == rightIdx) { // special case
                    writeDataIfNeeded(ref _prevSegmentData, rightIdx, (byte)leftIdx);
                    writeDataIfNeeded(ref _nextSegmentData, leftIdx, (byte)rightIdx);
                } else { // general case
                    if (leftPrev != STrailsSegment.NotConnected) {
                        writeDataIfNeeded(ref _nextSegmentData, leftPrev, (byte)rightIdx);
                    }
                    if (rightNext != STrailsSegment.NotConnected) {
                        writeDataIfNeeded(ref _prevSegmentData, rightNext, (byte)leftIdx);
                    }
                }

                if (leftIdx == _headSegmentIdx) {
                    _headSegmentIdx = (byte)rightIdx;
                    #if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; head = " + _headSegmentIdx
                        + ", head pos " + _readElement(_positions, _headSegmentIdx).Value); 
                    #endif
                } else if (rightIdx == _headSegmentIdx) {
                    _headSegmentIdx = (byte)leftIdx;
                    #if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; head =d " + _headSegmentIdx
                        + ", head pos " + _readElement(_positions, _headSegmentIdx).Value);
                    #endif
                }

                if (leftIdx == _tailSegmentIdx) {
                    _tailSegmentIdx = (byte)rightIdx;
                    #if TRAILS_DEBUG
                     Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; tail = " + _tailSegmentIdx);
                    #endif
                } else if (rightIdx == _tailSegmentIdx) {
                    _tailSegmentIdx = (byte)leftIdx;
                    #if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; tail = " + _tailSegmentIdx);
                    #endif
                }

                //Console.Write(" after swap " + leftIdx + " and " + rightIdx + ": ");
                //printTree();
            }

            public override void sortByDepth (ref Matrix4 viewMatrix)
            {
                //Console.Write("before sort: ");
                //printTree();

                base.sortByDepth(ref viewMatrix);

                //Console.Write("after sort: ");
                //printTree();
            }

            protected override int storeNewParticle (SSParticle newParticle)
            {
                var ts = (STrailsSegment)newParticle;
                ts.nextSegmentIdx = STrailsSegment.NotConnected;
                ts.prevSegmentIdx = _headSegmentIdx;

                if (numElements >= capacity && _tailSegmentIdx != STrailsSegment.NotConnected) {
                    _nextIdxToOverwrite = _tailSegmentIdx;
                }

                if (_headSegmentIdx != STrailsSegment.NotConnected ) {
                    Vector3 cylEnd = positionFunc();
                    Vector3 cylStart = _readElement(_positions, (int)_headSegmentIdx).Value;
                    Vector3 center = (cylStart + cylEnd) / 2f;
                    Vector3 diff = cylEnd - cylStart;
                    ts.pos = center;
                    ts.cylAxis = diff.Normalized();
                    ts.cylLendth = diff.LengthFast;
                }

                #if TRAILS_DEBUG
                Console.Write("before new particle, numElements = {0}: ", numElements);
                printTree();
                #endif

                byte newHead = (byte)base.storeNewParticle(newParticle);
                if (_headSegmentIdx != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _nextSegmentData, _headSegmentIdx, (byte)newHead);
                }
                _headSegmentIdx = newHead;

                if (_tailSegmentIdx == STrailsSegment.NotConnected) {
                    _tailSegmentIdx = newHead;
                    #if TRAILS_DEBUG
                    Console.WriteLine("new tail = " + _tailSegmentIdx);
                    #endif

                }
                #if TRAILS_DEBUG
                Console.Write("after new particle, numElements = {0}: ", numElements);
                printTree();
                #endif
                return _headSegmentIdx;
            }

            protected override void destroyParticle (int idx)
            {
                #if TRAILS_DEBUG
                Console.Write("before destroy {0}, numElements = {1}, head = {2}, tail = {3}: ", 
                    idx, numElements, _headSegmentIdx, _tailSegmentIdx);
                printTree(false);
                printTree(true);
                #endif

                byte prev = _readElement(_prevSegmentData, idx);
                byte next = _readElement(_nextSegmentData, idx);

                if (prev != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _nextSegmentData, prev, next);
                }
                if (next != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _prevSegmentData, next, prev);
                }

                if (idx == _tailSegmentIdx) {
                    _tailSegmentIdx = (byte)next;
                }
                if (idx == _headSegmentIdx) {
                    _headSegmentIdx = (byte)prev;
                }

                base.destroyParticle(idx);
                _prevSegmentData [idx] = STrailsSegment.NotConnected;
                _nextSegmentData [idx] = STrailsSegment.NotConnected;

                #if TRAILS_DEBUG
                Console.Write("after destroy {0}, numElements = {1}, head = {2}, tail = {3}: ", 
                    idx, numElements, _headSegmentIdx, _tailSegmentIdx);
                printTree(false);
                printTree(true);
                #endif
            }

            protected void printTree(bool dir = false)
            {
                if (dir) {
                    int safety = 0;
                    int idx = _headSegmentIdx;
                    while (idx != STrailsSegment.NotConnected && ++safety <= _capacity) {
                        Console.Write(idx + " < ");
                        idx = _readElement(_prevSegmentData, idx);
                    }
                } else {
                    int safety = 0;
                    int idx = _tailSegmentIdx;
                    while (idx != STrailsSegment.NotConnected && ++safety <= _capacity) {
                        Console.Write(idx + " > ");
                        idx = _readElement(_nextSegmentData, idx);
                    }
                }
                Console.Write("\n");
            }

            public class STrailsSegment : SSParticle
            {
                public const byte NotConnected = 255;

                public Vector3 cylAxis = -Vector3.UnitZ;
                public float cylLendth = 5f;
                public float cylWidth = 2f;
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

                    ts.pos = posFunc();
                    ts.cylAxis = velocity.Normalized();
                    ts.cylLendth = velocity.Length * trailParams.velocityToLengthFactor;
                    ts.cylWidth = trailParams.trailWidth;
                    ts.color = Color4Helper.RandomDebugColor();
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

                    #if false
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
                    #endif
                }
            }
        }
    }
}


