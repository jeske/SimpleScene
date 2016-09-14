//#define TRAILS_DEBUG
//#define TRAILS_SLOW

using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util;
using System.Collections.Generic;

namespace SimpleScene
{
	public class STrailsParameters
	{
		public int capacity = 1000;

		#if !TRAILS_SLOW
		public float trailsEmissionInterval = 0.02f;
		public int numCylindersPerEmissionMax = 5;
		#else
		// debugging options
		public float trailsEmissionInterval = 1f;
		public int numCylindersPerEmissionMax = 1;
		#endif

		public int numCylindersPerEmissionMin = 1;

		public float minSegmentLength = 0.001f;
		public float radiansPerExtraCylinder = (float)Math.PI/36f; // 5 degress
		public float trailLifetime = 20f;
		public string textureFilename = "trail_debug.png";
		//public float distanceToAlpha = 0.20f;
		public float distanceToAlpha = 0.40f;

		public float alphaMax = 1f;
		public float alphaMin = 0f;
		public Vector3[] localJetDirs = new Vector3[] { -Vector3.UnitZ };
		public Vector3[] localJetOffsets = new Vector3[] {
			new Vector3(-2f, 0f, +2f),
			new Vector3(+2f, 0f, +2f)
			//new Vector3(0f, 0f, -5f),
			//new Vector3(0f, 0f, 5f)
		};

		public SortedList<float, Color4> outerColorKeyframes = new SortedList<float, Color4> () {
			{ 0f, new Color4(1f, 0f, 0f, 0.5f) },
			{ 0.2f, new Color4(1f, 0f, 0f, 0.375f) },
			{ 0.8f, new Color4(1f, 0f, 0f, 0f) }
		};

		public SortedList<float, Color4> innerColorKeyframes = new SortedList<float, Color4> () {
			{ 0f, new Color4(1f, 1f, 1f, 0.7f) },
			{ 0.2f, new Color4(1f, 0f, 0f, 0.375f) },
			{ 0.8f, new Color4(1f, 0f, 0f, 0f) }
		};

		public SortedList<float, float> widthKeyFrames = new SortedList<float, float>() {
			{ 0f, 2f },
			{ 0.01f, 3f },
			{ 1f, 5f }
		};

		public SortedList<float, float> innerColorRatioKeyframes = new SortedList<float, float>() {
			{ 0f, 0.9f },
			{ 0.1f, 0.3f },
			{ 1f, 0f }
		};

		public SortedList<float, float> outerColorRatioKeyframes = new SortedList<float, float>() {
			{ 0f, 0.1f },
			{ 0.1f, 0.3f },
			{ 1f, 1f }
		};

		public int numJets { get { return localJetOffsets.Length; } }

		public Vector3 localJetDir(int idx) 
		{
			return localJetDirs [Math.Min (idx, localJetDirs.Length - 1)];
		}

		// default value
		public STrailsParameters()
		{
		}

		//public string textureFilename = "trail.png";
	}

    public class STrailsRenderer : SSInstancedMeshRenderer
    {
        public delegate Vector3 PositionFunc ();
        public delegate Vector3 FwdFunc();
		public delegate Vector3 UpFunc();

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

		public STrailsRenderer(PositionFunc positonFunc, FwdFunc fwdDirFunc, UpFunc upFunc,
            STrailsParameters trailsParams = null)
			: base(new STrailsData(positonFunc, fwdDirFunc, upFunc,
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

            renderState.blendEquationModeRGB = BlendEquationMode.FuncAdd;
            renderState.blendFactorSrcRGB = BlendingFactorSrc.SrcAlpha;
			//renderState.blendFactorDestRGB = BlendingFactorDest.DstAlpha;
			renderState.blendFactorDestRGB = BlendingFactorDest.OneMinusSrc1Alpha;

			renderState.blendEquationModeAlpha = BlendEquationMode.FuncAdd;
			renderState.blendFactorSrcAlpha = BlendingFactorSrc.One;
			renderState.blendFactorDestAlpha = BlendingFactorDest.One;
			//renderState.blendFactorSrcAlpha = BlendingFactorSrc.SrcAlpha;
			renderState.blendFactorDestAlpha = BlendingFactorDest.OneMinusSrcAlpha;

            simulateOnUpdate = true;

            // TODO 
            renderState.frustumCulling = true;

            colorMaterial = SSColorMaterial.pureAmbient;
            textureMaterial = new SSTextureMaterial (diffuse: tex);
            Name = "simple trails renderer";

            //this.MainColor = Color4Helper.RandomDebugColor();
            this.renderMode = RenderMode.GpuInstancing;
        }

        protected override void _initAttributeBuffers (BufferUsageHint hint)
        {
			hint = BufferUsageHint.DynamicDraw;

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

		public override void Render (SSRenderConfig renderConfig)
		{
			GL.ColorMask (false, false, false, true);
			GL.ClearColor (0f, 0f, 0f, 0f);
			GL.Clear (ClearBufferMask.ColorBufferBit);
			GL.ColorMask (true, true, true, true);

			base.Render (renderConfig);
		}

        public class STrailsData : SSParticleSystemData
        {
            public readonly STrailsParameters trailsParams;

			#region attribute array accessors
            public SSAttributeVec3[] cylinderAxes { get { return _cylAxes; } }
            public SSAttributeVec3[] prevJointAxes { get { return _prevJointAxes; } }
            public SSAttributeVec3[] nextJointAxes { get { return _nextJointAxes; } }
            public SSAttributeFloat[] cylinderLengths { get { return _cylLengths; } }
            public SSAttributeFloat[] cylinderWidth { get { return _cylWidths; } }
            public SSAttributeColor[] innerColors { get { return _cylInnerColors; } }
            public SSAttributeFloat[] innerColorRatios { get { return _innerColorRatios; } }
            public SSAttributeFloat[] outerColorRatios { get { return _outerColorRatios; } }
			#endregion

			#region attribute arrays
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
			#endregion
            
			#region array management
			protected readonly ushort[] _headSegmentIdxs;
			protected readonly ushort[] _tailSegmentIdxs;
			#endregion

			#region spline generation
	        protected PositionFunc _positionFunc;
			protected FwdFunc _fwdFunc;
			protected UpFunc _upFunc;
			protected Matrix4[] _localJetOrients;
            protected Vector3[] _prevSplineIntervalEndPos;
            protected Vector3[] _prevSplineIntervalEndSlope;
            protected Vector3 _newSplinePos;
            protected float _splineEmissionCounter = 0f;
			protected int _jetIndex = 0;
			#endregion

			#region cylinder updates
			protected STrailsWidthEffector _widthEffector;
			protected STrailsInnerColorRatioEffector _innerRatioEffector;
			protected STrailsOuterColorRatioEffector _outerRatioEffector;
			protected SSColorKeyframesEffector _outerColorEffector;
			protected STrailsInnerColorEffector _innerColorEffector;

			//protected readonly STrailUpdater _updater;
			#endregion

            public STrailsData(
				PositionFunc positionFunc, FwdFunc fwdDirFunc, UpFunc upFunc,
                STrailsParameters trailsParams = null)
                : base(trailsParams.capacity)
            {
                this.trailsParams = trailsParams;
                this._positionFunc = positionFunc;
				this._fwdFunc = fwdDirFunc;
				this._upFunc = upFunc;

				_headSegmentIdxs = new ushort[trailsParams.numJets];
				_tailSegmentIdxs = new ushort[trailsParams.numJets];
				_prevSplineIntervalEndPos = new Vector3[trailsParams.numJets];
				_prevSplineIntervalEndSlope = new Vector3[trailsParams.numJets];
				_localJetOrients = new Matrix4[trailsParams.numJets];

				Vector3 pos = _positionFunc();
				Vector3 fwd = _fwdFunc ();
				Vector3 up = _upFunc ();
				Vector3 right = Vector3.Cross (fwd, up);
				Matrix4 globalOrient = new Matrix4 (
					new Vector4 (right, 0f),
					new Vector4 (up, 0f),
					new Vector4 (fwd, 0f),
					new Vector4 (0f, 0f, 0f, 1f));

				for (int i = 0; i < trailsParams.numJets; ++i) {
					_headSegmentIdxs[i] = STrailsSegment.NotConnected;
					_tailSegmentIdxs[i] = STrailsSegment.NotConnected;
					Vector3 localFwd = trailsParams.localJetDir(i);
					_localJetOrients[i] = OpenTKHelper.neededRotationMat(-Vector3.UnitZ, localFwd);
					jetTxfm(i, ref pos, ref globalOrient, 
						out _prevSplineIntervalEndPos[i], out _prevSplineIntervalEndSlope[i]);

				}

				_outerColorEffector = new SSColorKeyframesEffector() { 
					particleLifetime = trailsParams.trailLifetime,
					keyframes = trailsParams.outerColorKeyframes 
				};
				addEffector(_outerColorEffector);

				_innerColorEffector = new STrailsInnerColorEffector() {
					particleLifetime = trailsParams.trailLifetime,
					keyframes = trailsParams.innerColorKeyframes
				};
				addEffector(_innerColorEffector);

				_innerRatioEffector = new STrailsInnerColorRatioEffector() { 
					particleLifetime = trailsParams.trailLifetime,
					keyframes = trailsParams.innerColorRatioKeyframes 
				};
				addEffector(_innerRatioEffector);

				_outerRatioEffector = new STrailsOuterColorRatioEffector() {
					particleLifetime = trailsParams.trailLifetime,
					keyframes = trailsParams.outerColorRatioKeyframes
				};
				addEffector(_outerRatioEffector);


				_widthEffector = new STrailsWidthEffector() { 
					particleLifetime = trailsParams.trailLifetime,
					keyframes = trailsParams.widthKeyFrames 
				};
				addEffector(_widthEffector);


                //_updater = new STrailUpdater(trailsParams);
                //addEffector(_updater);
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
                //_updater.updateViewMatrix(ref view);
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

				for (int i = 0; i < trailsParams.numJets; ++i) {
					if (leftIdx == _headSegmentIdxs[i]) {
						_headSegmentIdxs[i] = (ushort)rightIdx;
						#if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; head = " + _headSegmentIdx
                        + ", head pos " + _readElement(_positions, _headSegmentIdx).Value); 
						#endif
					} else if (rightIdx == _headSegmentIdxs[i]) {
						_headSegmentIdxs[i] = (ushort)leftIdx;
						#if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; head =d " + _headSegmentIdx
                        + ", head pos " + _readElement(_positions, _headSegmentIdx).Value);
						#endif
					}

					if (leftIdx == _tailSegmentIdxs[i]) {
						_tailSegmentIdxs[i] = (ushort)rightIdx;
						#if TRAILS_DEBUG
                     Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; tail = " + _tailSegmentIdx);
						#endif
					} else if (rightIdx == _tailSegmentIdxs[i]) {
						_tailSegmentIdxs[i] = (ushort)leftIdx;
						#if TRAILS_DEBUG
                    Console.WriteLine("swap: " + leftIdx + " and " + rightIdx + "; tail = " + _tailSegmentIdx);
						#endif
					}
				}

                //Console.Write(" after swap " + leftIdx + " and " + rightIdx + ": ");
                //printTree();
            }

            protected override void simulateStep ()
            {
                // https://en.wikipedia.org/wiki/Cubic_Hermite_spline
                _splineEmissionCounter += simulationStep;
                if (_splineEmissionCounter >= trailsParams.trailsEmissionInterval) {
                    while (_splineEmissionCounter >= trailsParams.trailsEmissionInterval) {
                        _splineEmissionCounter -= trailsParams.trailsEmissionInterval;
                    }

					Vector3 pos = _positionFunc ();

					Vector3 fwd = _fwdFunc ();
					Vector3 up = _upFunc ();
					Vector3 right = Vector3.Cross (fwd, up);
					Matrix4 globalOrient = new Matrix4 (
                       new Vector4 (right, 0f),
                       new Vector4 (up, 0f),
                       new Vector4 (fwd, 0f),
                       new Vector4 (0f, 0f, 0f, 1f));
					for (int i = 0; i < trailsParams.numJets; ++i) {
						Vector3 jetPos, jetFwd;
						jetTxfm (i, ref pos, ref globalOrient, out jetPos, out jetFwd);
						generateSplines (i, ref jetPos, ref jetFwd);
					}
                }

                base.simulateStep();
            }

			protected void jetTxfm(int jetIdx, ref Vector3 pos, ref Matrix4 globalOrient,
				out Vector3 jetPos, out Vector3 jetFwd)
			{
				Vector3 offset = trailsParams.localJetOffsets [jetIdx];
				Matrix4 combinedOrient = _localJetOrients[jetIdx] * globalOrient;
				jetPos = pos + Vector3.Transform (offset, combinedOrient);
				jetFwd = Vector3.Transform (Vector3.UnitZ, combinedOrient);
			}

			protected void generateSplines(int jetIdx, ref Vector3 jetPos, ref Vector3 jetSlope)
			{
				_jetIndex = jetIdx;
				Vector3 diff = jetPos - _prevSplineIntervalEndPos[jetIdx];
				if (diff.LengthFast >= trailsParams.minSegmentLength) {
					Vector3 prevSlope = _prevSplineIntervalEndSlope [jetIdx];
					Vector3 prevPos = _prevSplineIntervalEndPos [jetIdx];
					float angleBetweenSlopes = Vector3.CalculateAngle(jetSlope, prevSlope);
					int numCylinders = (int)(angleBetweenSlopes / trailsParams.radiansPerExtraCylinder);
					numCylinders = Math.Max(numCylinders, trailsParams.numCylindersPerEmissionMin);
					numCylinders = Math.Min(numCylinders, trailsParams.numCylindersPerEmissionMax);
					if (numCylinders == 1) {
						_newSplinePos = jetPos;
						storeNewParticle(createNewParticle());
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

							_newSplinePos = h00 * prevPos + h10 * prevSlope * trailsParams.trailsEmissionInterval
								+ h01 * jetPos + h11 * jetSlope * trailsParams.trailsEmissionInterval;
							storeNewParticle(createNewParticle());
						}
					}
					_prevSplineIntervalEndPos[jetIdx] = jetPos;
					_prevSplineIntervalEndSlope[jetIdx] = jetSlope;
				}
			}

            protected override int storeNewParticle (SSParticle newParticle)
            {
                var ts = (STrailsSegment)newParticle;
				ts.color = Color4.Red;
				ts.cylInnerColor = Color4.Yellow;
                ts.life = trailsParams.trailLifetime;
                ts.vel = Vector3.Zero;
                //ts.color = Color4.White;
                ts.nextSegmentIdx = STrailsSegment.NotConnected;

				ts.prevSegmentIdx = _headSegmentIdxs[_jetIndex];
				if (_headSegmentIdxs[_jetIndex] != STrailsSegment.NotConnected) {
					Vector3 prevCenter = _readElement(_positions, _headSegmentIdxs[_jetIndex]).Value;
					float prevLength = _readElement(_cylLengths, _headSegmentIdxs[_jetIndex]).Value;
					Vector3 prevAxis = _readElement(_cylAxes, _headSegmentIdxs[_jetIndex]).Value;
                    Vector3 prevJointPos = prevCenter + prevAxis * prevLength / 2f;
                    Vector3 nextJointPos = _newSplinePos;
                    Vector3 diff = (nextJointPos - prevJointPos);
					ts.cylLendth = diff.Length;
					#if TRAILS_SLOW
					ts.cylLendth *= 0.75f;
					#endif
                    ts.cylAxis = ts.nextJointAxis = diff / ts.cylLendth;
                    ts.pos = (nextJointPos + prevJointPos) / 2f;
                    Vector3 avgAxis = (prevAxis + ts.cylAxis).Normalized();
                    ts.prevJointAxis = -avgAxis;
					writeDataIfNeeded(ref _nextJointAxes, _headSegmentIdxs[_jetIndex], new SSAttributeVec3 (avgAxis));
                } else {
                    ts.pos = _newSplinePos;
                    ts.cylLendth = 0f;
                    ts.cylAxis = ts.nextJointAxis = Vector3.UnitX;
                    ts.prevJointAxis = -ts.cylAxis;
                    ts.pos = _positionFunc();
                }

				if (numElements >= capacity && _tailSegmentIdxs[_jetIndex] != STrailsSegment.NotConnected) {
					_nextIdxToOverwrite = _tailSegmentIdxs[_jetIndex];
                }

                #if TRAILS_DEBUG
                Console.Write("before new particle, numElements = {0}: ", numElements);
                printTree();
                #endif

                ushort newHead = (ushort)base.storeNewParticle(newParticle);
				if (_headSegmentIdxs[_jetIndex] != STrailsSegment.NotConnected) {
					writeDataIfNeeded(ref _nextSegmentData, _headSegmentIdxs[_jetIndex], (ushort)newHead);
                }
				_headSegmentIdxs[_jetIndex] = newHead;

				if (_tailSegmentIdxs[_jetIndex] == STrailsSegment.NotConnected) {
					_tailSegmentIdxs[_jetIndex] = newHead;
                    #if TRAILS_DEBUG
                    Console.WriteLine("new tail = " + _tailSegmentIdx);
                    #endif

                }
                #if TRAILS_DEBUG
                Console.Write("after new particle, numElements = {0}: ", numElements);
                printTree();
                #endif
				return _headSegmentIdxs[_jetIndex];
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
				for (int j = 0; j < trailsParams.numJets; ++j) {
					if (idx == _tailSegmentIdxs[j]) {
						_tailSegmentIdxs[j] = (ushort)next;
					}
					if (idx == _headSegmentIdxs[j]) {
						_headSegmentIdxs[j] = (ushort)prev;
					}
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
				for (int j = 0; j < trailsParams.numJets; ++j) {
					Console.Write ("jet #" + j + ": ");
					if (dir) {
						int safety = 0;
						int idx = _headSegmentIdxs[j];
						while (idx != STrailsSegment.NotConnected && ++safety <= _capacity) {
							Console.Write (idx + " < ");
							idx = _readElement (_prevSegmentData, idx);
						}
					} else {
						int safety = 0;
						int idx = _tailSegmentIdxs[j];
						while (idx != STrailsSegment.NotConnected && ++safety <= _capacity) {
							Console.Write (idx + " > ");
							idx = _readElement (_nextSegmentData, idx);
						}
					}
					Console.Write ("\n");
				}
            }

            public class STrailsSegment : SSParticle
            {
                public const ushort NotConnected = ushort.MaxValue;

                public Vector3 cylAxis = -Vector3.UnitZ;
                public Vector3 prevJointAxis = -Vector3.UnitZ;
                public Vector3 nextJointAxis = -Vector3.UnitZ;
				public Color4 cylInnerColor = Color4.White;
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
                public FwdFunc fwdDirFunc;
                public STrailsParameters trailParams;

                public STrailsEmitter(STrailsParameters tParams, 
                    PositionFunc posFunc, FwdFunc fwdDirFunc)
                {
                    this.trailParams = tParams;
                    this.posFunc = posFunc;
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

			protected class STrailsWidthEffector : SSKeyframesEffector<float>
			{
				protected override void applyValue (SSParticle particle, float value)
				{
					var ts = (STrailsSegment)particle;
					ts.cylWidth = value;
				}

				protected override float computeValue (IInterpolater interpolater, float prevFrame, float nextKeyframe, float ammount)
				{
					return interpolater.compute (prevFrame, nextKeyframe, ammount);
				}
			}

			protected class STrailsInnerColorRatioEffector : SSKeyframesEffector<float>
			{
				protected override void applyValue (SSParticle particle, float value)
				{
					var ts = (STrailsSegment)particle;
					ts.innerColorRatio = value;
				}

				protected override float computeValue (IInterpolater interpolater, float prevFrame, float nextKeyframe, float ammount)
				{
					return interpolater.compute (prevFrame, nextKeyframe, ammount);
				}
			}

			protected class STrailsOuterColorRatioEffector : SSKeyframesEffector<float>
			{
				protected override void applyValue (SSParticle particle, float value)
				{
					var ts = (STrailsSegment)particle;
					ts.outerColorRatio = value;
				}

				protected override float computeValue (IInterpolater interpolater, float prevFrame, float nextKeyframe, float ammount)
				{
					return interpolater.compute (prevFrame, nextKeyframe, ammount);
				}
			}

			protected class STrailsInnerColorEffector : SSKeyframesEffector<Color4>
			{
				protected override void applyValue (SSParticle particle, Color4 value)
				{
					var ts = (STrailsSegment)particle;
					ts.cylInnerColor = value;
				}

				protected override Color4 computeValue (IInterpolater interpolater, Color4 prevFrame, Color4 nextKeyframe, float ammount)
				{
					return new Color4 (
						interpolater.compute (prevFrame.R, nextKeyframe.R, ammount),
						interpolater.compute (prevFrame.G, nextKeyframe.G, ammount),
						interpolater.compute (prevFrame.B, nextKeyframe.B, ammount),
						interpolater.compute (prevFrame.A, nextKeyframe.A, ammount));
				}
			}

			#if false
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
			#endif
        }
    }
}


