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
            public int capacity = 2000;
            public float trailWidth = 5f;
            //public float trailsEmissionInterval = 0.05f;
            public float trailsEmissionInterval = 1f;
            public int numCylindersPerEmissionMin = 1;
            //public int numCylindersPerEmissionMax = 5;
            public int numCylindersPerEmissionMax = 1;
            public float minSegmentLength = 0.01f;
            public float radiansPerExtraCylinder = (float)Math.PI/36f; // 5 degress
            public float velocityToLengthFactor = 1f;
            public float trailLifetime = 2000f;
            public float trailCutoffVelocity = 0.1f;
            public string textureFilename = "trail_debug.png";
            public float distanceToAlpha = 0.05f;
            public float alphaMax = 1f;
            public float alphaMin = 0f;

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
        protected SSAttributeBuffer<SSAttributeVec3> _cylAxesBuffer;
        protected SSAttributeBuffer<SSAttributeVec3> _prevJointBuffer;
        protected SSAttributeBuffer<SSAttributeVec3> _nextJointBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _widthsBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _lengthsBuffer;
        protected SSAttributeBuffer<SSAttributeColor> _innerColorBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _innerColorRatioBuffer;
        protected SSAttributeBuffer<SSAttributeFloat> _outerColorRatioBuffer;

        public STrailsRenderer(PositionFunc positonFunc, VelocityFunc velocityFunc, DirFunc fwdDirFunc,
            STrailsParameters trailsParams = null)
            : base(new STrailsData(positonFunc, velocityFunc, fwdDirFunc, 
                trailsParams ?? new STrailsParameters()),
                SSTexturedCube.Instance, _defaultUsageHint)
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

            renderState.blendEquationMode = BlendEquationMode.FuncAdd;
            renderState.blendFactorSrc = BlendingFactorSrc.SrcAlpha;
            renderState.blendFactorDest = BlendingFactorDest.One;
            //renderState.blendFactorDest = BlendingFactorDest.OneMinusSrcAlpha;

            simulateOnUpdate = true;

            // TODO this is kind of heavy heanded. try a few alternatives with bounding spheres
            renderState.frustumCulling = false;

            colorMaterial = SSColorMaterial.pureAmbient;
            textureMaterial = new SSTextureMaterial (diffuse: tex);
            Name = "simple trails renderer";

            //this.MainColor = Color4Helper.RandomDebugColor();
            this.renderMode = RenderMode.GpuInstancing;
        }

        protected override void _initAttributeBuffers (BufferUsageHint hint)
        {
            _posBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
            _cylAxesBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
            _prevJointBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
            _nextJointBuffer = new SSAttributeBuffer<SSAttributeVec3> (hint);
            _widthsBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
            _lengthsBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
            _colorBuffer = new SSAttributeBuffer<SSAttributeColor> (hint);
            _innerColorBuffer = new SSAttributeBuffer<SSAttributeColor> (hint);
            _innerColorRatioBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
            _outerColorRatioBuffer = new SSAttributeBuffer<SSAttributeFloat> (hint);
        }

        protected override void _prepareInstanceShader (SSRenderConfig renderConfig)
        {
            _shader = _shader ?? (SSInstancedCylinderShaderProgram)renderConfig.otherShaders["instanced_cylinder"];
            _shader.Activate();

            _shader.UniViewMatrix = renderConfig.invCameraViewMatrix;
            _shader.UniViewMatrixInverse = renderConfig.invCameraViewMatrix.Inverted();
            _shader.UniDistanceToAlpha = trailsData.trailsParams.distanceToAlpha;
            _shader.UniAlphaMax = trailsData.trailsParams.alphaMax;
            _shader.UniAlphaMin = trailsData.trailsParams.alphaMin;

            _prepareAttribute(_posBuffer, _shader.AttrCylinderPos, trailsData.positions);
            _prepareAttribute(_cylAxesBuffer, _shader.AttrCylinderAxis, trailsData.cylinderAxes);
            _prepareAttribute(_prevJointBuffer, _shader.AttrPrevJointAxis, trailsData.prevJointAxes);
            _prepareAttribute(_nextJointBuffer, _shader.AttrNextJointAxis, trailsData.nextJointAxes);
            _prepareAttribute(_lengthsBuffer, _shader.AttrCylinderLength, trailsData.cylinderLengths);
            _prepareAttribute(_widthsBuffer, _shader.AttrCylinderWidth, trailsData.cylinderWidth);
            _prepareAttribute(_colorBuffer, _shader.AttrCylinderColor, trailsData.colors);
            _prepareAttribute(_innerColorBuffer, _shader.AttrCylinderInnerColor, trailsData.innerColors);
            _prepareAttribute(_innerColorRatioBuffer, _shader.AttrInnerColorRatio, trailsData.innerColorRatios);
            _prepareAttribute(_outerColorRatioBuffer, _shader.AttrOuterColorRatio, trailsData.outerColorRatios);
        }

        public class STrailsData : SSParticleSystemData
        {
            public readonly STrailsParameters trailsParams;

            public SSAttributeVec3[] cylinderAxes { get { return _cylAxes; } }
            public SSAttributeVec3[] prevJointAxes { get { return _prevJointAxes; } }
            public SSAttributeVec3[] nextJointAxes { get { return _nextJointAxes; } }
            public SSAttributeFloat[] cylinderLengths { get { return _cylLengths; } }
            public SSAttributeFloat[] cylinderWidth { get { return _cylWidths; } }
            public SSAttributeColor[] innerColors { get { return _cylInnerColors; } }
            public SSAttributeFloat[] innerColorRatios { get { return _innerColorRatios; } }
            public SSAttributeFloat[] outerColorRatios { get { return _outerColorRatios; } }

            protected ushort _headSegmentIdx = STrailsSegment.NotConnected;
            protected ushort _tailSegmentIdx = STrailsSegment.NotConnected;
            protected ushort[] _nextSegmentData = null;
            protected ushort[] _prevSegmentData = null;
            protected SSAttributeColor[] _cylInnerColors;
            protected SSAttributeFloat[] _innerColorRatios;
            protected SSAttributeFloat[] _outerColorRatios;
            protected SSAttributeVec3[] _cylAxes = null;
            protected SSAttributeVec3[] _prevJointAxes = null;
            protected SSAttributeVec3[] _nextJointAxes = null;
            protected SSAttributeFloat[] _cylLengths = null;
            protected SSAttributeFloat[] _cylWidths = null;
            protected readonly STrailUpdater _updater;

            protected PositionFunc positionFunc;
            protected VelocityFunc velocityFunc;
            protected Vector3 _prevSplineIntervalEndPos;
            protected Vector3 _prevSplineIntervalEndSlope;
            protected Vector3 _newSplinePos;
            protected float splineEmissionCounter = 0f;

            public STrailsData(PositionFunc positionFunc, VelocityFunc velocityFunc, DirFunc fwdDirFunc,
                STrailsParameters trailsParams = null)
                : base(trailsParams.capacity)
            {
                this.trailsParams = trailsParams;
                this.positionFunc = positionFunc;
                this.velocityFunc = velocityFunc;

                //_headEmitter = new TrailEmitter(positionFunc, velocityFunc, trailParams);

                //addEmitter(new STrailsEmitter(trailsParams, positionFunc, velocityFunc, fwdDirFunc));

                _updater = new STrailUpdater(trailsParams);
                addEffector(_updater);

                _prevSplineIntervalEndPos = positionFunc();
                Vector3 vel = velocityFunc();
                float velLength = vel.Length;
                _prevSplineIntervalEndSlope = velLength > 0 ? (vel / velLength) : fwdDirFunc();
                    
            }

            protected override void initArrays ()
            {
                base.initArrays();

                _cylAxes = new SSAttributeVec3[1];
                _prevJointAxes = new SSAttributeVec3[1];
                _nextJointAxes = new SSAttributeVec3[1];
                _cylLengths = new SSAttributeFloat[1];
                _cylWidths = new SSAttributeFloat[1];
                _nextSegmentData = new ushort[1];
                _prevSegmentData = new ushort[1];
                _cylInnerColors = new SSAttributeColor[1];
                _innerColorRatios = new SSAttributeFloat[1];
                _outerColorRatios = new SSAttributeFloat[1];
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
                ts.cylInnerColor = Color4Helper.FromUInt32(_readElement(_cylInnerColors, idx).Color);
                ts.innerColorRatio = _readElement(_innerColorRatios, idx).Value;
                ts.outerColorRatio = _readElement(_outerColorRatios, idx).Value;
                ts.cylAxis = _readElement(_cylAxes, idx).Value;
                ts.prevJointAxis = _readElement(_prevJointAxes, idx).Value;
                ts.nextJointAxis = _readElement(_nextJointAxes, idx).Value;
                ts.cylWidth = _readElement(_cylWidths, idx).Value;
                ts.cylLendth = _readElement(_cylLengths, idx).Value;
                ts.nextSegmentIdx = _readElement(_nextSegmentData, idx);
                ts.prevSegmentIdx = _readElement(_prevSegmentData, idx);
            }

            protected override void writeParticle (int idx, SSParticle p)
            {
                #if false
                // attempt of only writing things that are relevant; skipping the rest
                _lives [idx] = p.life;
                writeDataIfNeeded(ref _positions, idx, new SSAttributeVec3(p.pos));
                writeDataIfNeeded(ref _viewDepths, idx, p.viewDepth);
                writeDataIfNeeded(ref _effectorMasksHigh, idx, (ushort)((p.effectorMask & 0xFF00) >> 8));
                writeDataIfNeeded(ref _effectorMasksLow, idx, (ushort)(p.effectorMask & 0xFF));
                writeDataIfNeeded(ref _colors, idx, new SSAttributeColor(Color4Helper.ToUInt32(p.color)));
                #else
                // for some reason the above doesn't work, so lets just do things slightly less efficiently for now
                base.writeParticle(idx, p);
                #endif

                var ts = (STrailsSegment)p;
                var innerColor = Color4Helper.ToUInt32(ts.cylInnerColor);
                writeDataIfNeeded(ref _cylInnerColors, idx, new SSAttributeColor(innerColor));
                writeDataIfNeeded(ref _innerColorRatios, idx, new SSAttributeFloat (ts.innerColorRatio));
                writeDataIfNeeded(ref _outerColorRatios, idx, new SSAttributeFloat(ts.outerColorRatio));
                writeDataIfNeeded(ref _cylAxes, idx, new SSAttributeVec3(ts.cylAxis));
                writeDataIfNeeded(ref _prevJointAxes, idx, new SSAttributeVec3 (ts.prevJointAxis));
                writeDataIfNeeded(ref _nextJointAxes, idx, new SSAttributeVec3 (ts.nextJointAxis));
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

                ushort leftPrev = _readElement(_prevSegmentData, leftIdx);
                ushort leftNext = _readElement(_nextSegmentData, leftIdx);
                ushort rightPrev = _readElement(_prevSegmentData, rightIdx);
                ushort rightNext = _readElement(_nextSegmentData, rightIdx);

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
                    writeDataIfNeeded(ref _prevSegmentData, leftNext, (ushort)rightIdx);
                }
                if (rightPrev != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _nextSegmentData, rightPrev, (ushort)leftIdx);
                }

                if (leftPrev == rightIdx) { // special case
                    writeDataIfNeeded(ref _prevSegmentData, rightIdx, (ushort)leftIdx);
                    writeDataIfNeeded(ref _nextSegmentData, leftIdx, (ushort)rightIdx);
                } else { // general case
                    if (leftPrev != STrailsSegment.NotConnected) {
                        writeDataIfNeeded(ref _nextSegmentData, leftPrev, (ushort)rightIdx);
                    }
                    if (rightNext != STrailsSegment.NotConnected) {
                        writeDataIfNeeded(ref _prevSegmentData, rightNext, (ushort)leftIdx);
                    }
                }

                if (leftIdx == _headSegmentIdx) {
                    _headSegmentIdx = (ushort)rightIdx;
                    #if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; head = " + _headSegmentIdx
                        + ", head pos " + _readElement(_positions, _headSegmentIdx).Value); 
                    #endif
                } else if (rightIdx == _headSegmentIdx) {
                    _headSegmentIdx = (ushort)leftIdx;
                    #if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; head =d " + _headSegmentIdx
                        + ", head pos " + _readElement(_positions, _headSegmentIdx).Value);
                    #endif
                }

                if (leftIdx == _tailSegmentIdx) {
                    _tailSegmentIdx = (ushort)rightIdx;
                    #if TRAILS_DEBUG
                     Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; tail = " + _tailSegmentIdx);
                    #endif
                } else if (rightIdx == _tailSegmentIdx) {
                    _tailSegmentIdx = (ushort)leftIdx;
                    #if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; tail = " + _tailSegmentIdx);
                    #endif
                }

                //Console.Write(" after swap " + leftIdx + " and " + rightIdx + ": ");
                //printTree();
            }

            protected override void simulateStep ()
            {
                // https://en.wikipedia.org/wiki/Cubic_Hermite_spline
                splineEmissionCounter += simulationStep;
                if (splineEmissionCounter >= trailsParams.trailsEmissionInterval) {
                    while (splineEmissionCounter >= trailsParams.trailsEmissionInterval) {
                        splineEmissionCounter -= trailsParams.trailsEmissionInterval;
                    }
                    Vector3 slope = velocityFunc();
                    Vector3 pos = positionFunc();
                    Vector3 diff = pos - _prevSplineIntervalEndPos;
                    if (diff.LengthFast >= trailsParams.minSegmentLength) {
                        float angleBetweenSlopes = Vector3.CalculateAngle(slope, _prevSplineIntervalEndSlope);
                        int numCylinders = (int)(angleBetweenSlopes / trailsParams.radiansPerExtraCylinder);
                        numCylinders = Math.Max(numCylinders, trailsParams.numCylindersPerEmissionMin);
                        numCylinders = Math.Min(numCylinders, trailsParams.numCylindersPerEmissionMax);
                        if (numCylinders == 1) {
                            var newParticle = createNewParticle();
                            //newParticle.color = Color4Helper.DebugPresets [0];
                            //newParticle.color = Color4.OrangeRed;
                            newParticle.color = Color4.Lime;
                            //newParticle.color.A = 0.25f;
                            //newParticle.color = Color4Helper.RandomDebugColor();
                            _newSplinePos = pos;

                            storeNewParticle(newParticle);
                        } else {
                            float dt = 1f / numCylinders;
                            for (int i = 1; i <= numCylinders; ++i) {
                                float t = i * dt;
                                float tSq = t * t;
                                float tMinusOne = t - 1;
                                float tMinusOneSq = tMinusOne * tMinusOne;
                                float h00 = (1 + 2 * t) * tMinusOneSq;
                                float h10 = t * tMinusOneSq;
                                float h01 = tSq * (3 - 2 * t);
                                float h11 = tSq * tMinusOne;
                                //float slopeScale = pos - _prevSplineIntervalEndPos;

                                _newSplinePos = h00 * _prevSplineIntervalEndPos + h10 * _prevSplineIntervalEndSlope * trailsParams.trailsEmissionInterval
                                + h01 * pos + h11 * slope * trailsParams.trailsEmissionInterval;
                                var newParticle = createNewParticle();
                                //newParticle.color = Color4Helper.DebugPresets [i % Color4Helper.DebugPresets.Length];
                                newParticle.color = Color4.OrangeRed;
                                //newParticle.color.A = 0.25f;
                                //newParticle.color = Color4Helper.RandomDebugColor();
                                storeNewParticle(newParticle);
                            }
                        }
                        _prevSplineIntervalEndPos = pos;
                        _prevSplineIntervalEndSlope = slope;
                    }
                }

                base.simulateStep();
            }

            protected override int storeNewParticle (SSParticle newParticle)
            {
                var ts = (STrailsSegment)newParticle;
                ts.life = trailsParams.trailLifetime;
                ts.vel = Vector3.Zero;
                ts.cylWidth = trailsParams.trailWidth;
                //ts.color = Color4.White;
                ts.nextSegmentIdx = STrailsSegment.NotConnected;

                ts.prevSegmentIdx = _headSegmentIdx;
                if (_headSegmentIdx != STrailsSegment.NotConnected) {
                    Vector3 prevCenter = _readElement(_positions, _headSegmentIdx).Value;
                    float prevLength = _readElement(_cylLengths, _headSegmentIdx).Value;
                    Vector3 prevAxis = _readElement(_cylAxes, _headSegmentIdx).Value;
                    Vector3 prevJointPos = prevCenter + prevAxis * prevLength / 2f;
                    Vector3 nextJointPos = _newSplinePos;
                    Vector3 diff = (nextJointPos - prevJointPos);
                    ts.cylLendth = diff.Length;
                    ts.cylAxis = ts.nextJointAxis = diff / ts.cylLendth;
                    ts.pos = (nextJointPos + prevJointPos) / 2f;
                    Vector3 avgAxis = (prevAxis + ts.cylAxis).Normalized();
                    ts.prevJointAxis = -avgAxis;
                    writeDataIfNeeded(ref _nextJointAxes, _headSegmentIdx, new SSAttributeVec3 (avgAxis));
                } else {
                    ts.pos = _newSplinePos;
                    ts.cylWidth = trailsParams.trailWidth;
                    ts.cylLendth = 0f;
                    ts.cylAxis = ts.nextJointAxis = -Vector3.UnitZ;
                    ts.prevJointAxis = -ts.cylAxis;
                    ts.pos = positionFunc();
                }

                if (numElements >= capacity && _tailSegmentIdx != STrailsSegment.NotConnected) {
                    _nextIdxToOverwrite = _tailSegmentIdx;
                }

                #if TRAILS_DEBUG
                Console.Write("before new particle, numElements = {0}: ", numElements);
                printTree();
                #endif

                ushort newHead = (ushort)base.storeNewParticle(newParticle);
                if (_headSegmentIdx != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _nextSegmentData, _headSegmentIdx, (ushort)newHead);
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

                ushort prev = _readElement(_prevSegmentData, idx);
                ushort next = _readElement(_nextSegmentData, idx);

                if (prev != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _nextSegmentData, prev, next);
                }
                if (next != STrailsSegment.NotConnected) {
                    writeDataIfNeeded(ref _prevSegmentData, next, prev);
                }

                if (idx == _tailSegmentIdx) {
                    _tailSegmentIdx = (ushort)next;
                }
                if (idx == _headSegmentIdx) {
                    _headSegmentIdx = (ushort)prev;
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
                public const ushort NotConnected = ushort.MaxValue;

                public Vector3 cylAxis = -Vector3.UnitZ;
                public Vector3 prevJointAxis = -Vector3.UnitZ;
                public Vector3 nextJointAxis = -Vector3.UnitZ;
                public Color4 cylInnerColor = Color4.Red;
                /// <summary> inner cylinder is 100% inner color. Its radius is innerColorRatio * total radius /// </summary>
                public float innerColorRatio = 0.3f; 
                /// <summary> outer tube is 100% outer color. It begins at outerColorRatio * total radius, and ends at total radius // </summary>
                public float outerColorRatio = 0.3f;
                public float cylLendth = 5f;
                public float cylWidth = 2f;
                public ushort prevSegmentIdx = NotConnected;
                public ushort nextSegmentIdx = NotConnected;
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
                    // TODO may be a good place to implement splines
                }
                #endif
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

                    // TODO?
                }
            }
        }
    }
}


