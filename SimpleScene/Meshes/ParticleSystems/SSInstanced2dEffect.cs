using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
	public abstract class SSInstanced2dEffect : SSInstancedMeshRenderer
	{
		#region Source of Per-Frame Input
        public SSScene cameraScene3d = null;
		#endregion

        #region per-frame temp variables
        protected Matrix4 _viewProjMat3d;
        protected RectangleF _clientRect;
        #endregion

        public SSInstanced2dEffect (
            int numElements,
            SSScene cameraScene3d,
            SSTexture tex = null
        )
            : base(new SInstancedSpriteData(numElements),
				   SSTexturedQuad.DoubleFaceInstance,
				   BufferUsageHint.StreamDraw)
		{
            base.renderState.castsShadow = false;
            base.renderState.receivesShadows = false;
            base.renderState.depthTest = false;
            base.renderState.depthWrite = false;
            base.renderState.lighted = false;
            base.textureMaterial = new SSTextureMaterial (tex);
            base.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
            base.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
            base.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.ShininessMatColor = 0f;

            this.cameraScene3d = cameraScene3d;
		}

		protected new SInstancedSpriteData instanceData {
			get { return base.instanceData as SInstancedSpriteData; }
		}

		public override void Render (SSRenderConfig renderConfig)
		{
            var rc = cameraScene3d.renderConfig;
            _viewProjMat3d = rc.invCameraViewMatrix * rc.projectionMatrix;
            _clientRect = OpenTKHelper.GetClientRect();

            _prepareSpritesData();

			base.Render (renderConfig);
		}

        protected Vector2 worldToScreen(Vector3 worldPos) 
        {
            return OpenTKHelper.WorldToScreen(worldPos, ref _viewProjMat3d, ref _clientRect);

            #if false
            Vector4 pos = Vector4.Transform(new Vector4(worldPos, 1f), _viewProjMat3d);
            pos /= pos.W;
            pos.Y = -pos.Y;
            return  _screenCenter + pos.Xy * _screenSize / 2f;
            #endif
        }

        public float[] masterScales {
            set {
                for (int i = 0; i < value.Length; ++i) {
                    instanceData.writeMasterScale(i, value [i]);
                }
            }
        }

        public Vector2[] componentScales {
            set {
                for (int i = 0; i < value.Length; ++i) {
                    instanceData.writeComponentScale(i, value [i]);
                }
            }
        }

        public Color4[] colors {
            set {
                for (int i = 0; i < value.Length; ++i) {
                    instanceData.writeColor(i, value [i]);
                }
            }
        }

        public RectangleF[] rects {
            set {
                for (int i = 0; i < value.Length; ++i) {
                    instanceData.writeRect(i, value [i]);
                }
            }
        }

        /// <summary>
        /// Implement this to position sprites, update colors, etc.
        /// </summary>
        protected abstract void _prepareSpritesData();

		protected class SInstancedSpriteData : SSInstancesData
		{
            // TODO easy setters

            #region implement SSInstancesData
            public override int capacity { get { return _numElements; } }
            public override int activeBlockLength { get { return _numElements; } }
            public override float radius { get { return 100f; } } // TODO
            public override SSAttributeVec3[] positions { get { return _positions; } }
            public override SSAttributeVec2[] orientationsXY { get { return _orientationsXY; } }
            public override SSAttributeFloat[] orientationsZ { get { return _orientationsZ; } }
            public override SSAttributeColor[] colors { get { return _colors; } }
            public override SSAttributeFloat[] masterScales { get { return _masterScales; } }
            public override SSAttributeVec2[] componentScalesXY { get { return _componentScalesXY; } }
            public override SSAttributeFloat[] componentScalesZ { get { return _componentScalesZ; } }
            //public SSAttributeByte[] SpriteIndices { get { return m_spriteIndices; } }
            public override SSAttributeFloat[] spriteOffsetsU { get { return _spriteOffsetsU; ; } }
            public override SSAttributeFloat[] spriteOffsetsV { get { return _spriteOffsetsV; } }
            public override SSAttributeFloat[] spriteSizesU { get { return _spriteSizesU; } }
            public override SSAttributeFloat[] spriteSizesV { get { return _spriteSizesV; } }
            #endregion

			protected int _numElements;

			#region sprite data sent to the GPU
            protected SSAttributeVec3[] _positions = { new SSAttributeVec3(Vector3.Zero) };
			protected SSAttributeVec2[] _orientationsXY = { new SSAttributeVec2(Vector2.Zero) };
			protected SSAttributeFloat[] _orientationsZ = { new SSAttributeFloat(0f) };
			protected SSAttributeColor[] _colors = { new SSAttributeColor (Color4Helper.ToUInt32 (Color4.White)) };
			protected SSAttributeFloat[] _masterScales = { new SSAttributeFloat(1f) };
			protected SSAttributeVec2[] _componentScalesXY = { new SSAttributeVec2(Vector2.One) };
			protected SSAttributeFloat[] _componentScalesZ = { new SSAttributeFloat(1f) };

			//protected SSAttributeByte[] m_spriteIndices;
            protected SSAttributeFloat[] _spriteOffsetsU = { new SSAttributeFloat(0f) };
            protected SSAttributeFloat[] _spriteOffsetsV = { new SSAttributeFloat(0f) };
            protected SSAttributeFloat[] _spriteSizesU = { new SSAttributeFloat(1f) };
            protected SSAttributeFloat[] _spriteSizesV = { new SSAttributeFloat(1f) };
			#endregion

            public void writePosition(int idx, Vector2 pos) {
                var newPos = new Vector3 (pos.X, pos.Y, 0f);
                writeDataIfNeeded(ref _positions, idx, new SSAttributeVec3 (newPos));
            }

            public void writeMasterScale(int idx, float scale) {
                writeDataIfNeeded(ref _masterScales, idx, new SSAttributeFloat (scale));
            }

            public void writeComponentScale(int idx, Vector2 compScale) {
                writeDataIfNeeded(ref _componentScalesXY, idx, new SSAttributeVec2 (compScale));
            }

            public void writeColor(int idx, Color4 color) {
                var newColor = Color4Helper.ToUInt32(color);
                writeDataIfNeeded(ref _colors, idx, new SSAttributeColor(newColor));
            }

            public void writeRect(int idx, RectangleF rect) {
                writeDataIfNeeded(ref _spriteOffsetsU, idx, new SSAttributeFloat (rect.X));
                writeDataIfNeeded(ref _spriteOffsetsV, idx, new SSAttributeFloat (rect.Y));
                writeDataIfNeeded(ref _spriteSizesU, idx, new SSAttributeFloat (rect.Width));
                writeDataIfNeeded(ref _spriteSizesV, idx, new SSAttributeFloat (rect.Height));
            }

            public SInstancedSpriteData(int numElements)
			{
                _numElements = numElements;

                #if false
                if (spriteRects != null) {
                    int numSpriteRects = spriteRects.Length;
                    _numElements = Math.Max(_numElements, numSpriteRects);
                    _spriteSizesU = new SSAttributeFloat[numSpriteRects];
                    _spriteSizesV = new SSAttributeFloat[numSpriteRects];
                    _spriteOffsetsU = new SSAttributeFloat[numSpriteRects];
                    _spriteOffsetsV = new SSAttributeFloat[numSpriteRects];
                    for (int i = 0; i < numSpriteRects; ++i) {
                        var spriteRect = spriteRects[i]; 
                        _spriteSizesU[i] = new SSAttributeFloat(spriteRect.Width);
                        _spriteSizesV[i] = new SSAttributeFloat(spriteRect.Height);
                        _spriteOffsetsU[i] = new SSAttributeFloat(spriteRect.Left);
                        _spriteOffsetsV[i] = new SSAttributeFloat(spriteRect.Top);
                    }
                }

                if (masterScales != null) {
                    int numMaserScales = masterScales.Length;
                    _numElements = Math.Max(_numElements, numMaserScales);
                    _masterScales = new SSAttributeFloat[numMaserScales];
                    for (int i = 0; i < numMaserScales; ++i) {
                        _masterScales[i] = new SSAttributeFloat(masterScales[i]);
                    }
                }

                if (colors != null) {
                    int numColors = colors.Length;
                    _numElements = Math.Max(_numElements, numColors);
                    _colors = new SSAttributeColor[numColors];
                    for (int i = 0; i < colors.Length; ++i) {
                        _colors [i] = new SSAttributeColor(Color4Helper.ToUInt32(colors [i]));
                    }
                }

                if (componentScales != null) {
                    int numComponentScales = componentScales.Length;
                    _numElements = Math.Max(_numElements, numComponentScales);
                    _componentScalesXY = new SSAttributeVec2[numComponentScales];
                    for (int i = 0; i < numComponentScales; ++i) {
                        _componentScalesXY[i] = new SSAttributeVec2(componentScales[i]);
                    }
                }
                #endif

                _positions = new SSAttributeVec3[_numElements];
			}

            public void writeDataIfNeeded<T>(ref T[] array, int idx, T value) where T : IEquatable<T>
            {
                bool write = true;
                if (idx > 0 && array.Length == 1) {
                    T masterVal = array [0];
                    if (masterVal.Equals(value)) {
                        write = false;
                    } else {
                        // allocate the array to keep track of different values
                        array = new T[_numElements];
                        for (int i = 0; i < _numElements; ++i) {
                            array [i] = masterVal;
                        }
                    }
                }
                if (write) {
                    array [idx] = value;
                }
            }
		}
	}
}

