using System;
using System.Drawing;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Demos
{
    public interface ISSpriteUpdater
    {
        void setupSprites (SInstancedSpriteData instanceData);
        void updateSprites(SInstancedSpriteData instanceData,
                           ref Matrix4 camera3dViewProjMat, ref RectangleF clientRect);
        void releaseSprites (SInstancedSpriteData instanceData);
    }

	public class SSInstancedSpriteRenderer : SSInstancedMeshRenderer
	{
		#region Source of Per-Frame Input
        public SSScene cameraScene3d = null;
		#endregion

        protected List<ISSpriteUpdater> _spriteUpdaters = new List<ISSpriteUpdater> ();

        public SSInstancedSpriteRenderer (
            SSScene cameraScene3d,
            SSInstancesData instanceData,
            SSTexture tex = null
        )
            : base(instanceData,
				   SSTexturedQuad.DoubleFaceInstance,
				   BufferUsageHint.StreamDraw)
		{
            base.renderState.castsShadow = false;
            base.renderState.receivesShadows = false;
            base.renderState.depthTest = false;
            base.renderState.depthWrite = false;
            base.renderState.lighted = false;
            base.AmbientMatColor = new Color4 (1f, 1f, 1f, 1f);
            base.DiffuseMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.EmissionMatColor = new Color4(0f, 0f, 0f, 0f);
            base.SpecularMatColor = new Color4 (0f, 0f, 0f, 0f);
            base.ShininessMatColor = 0f;
            base.Selectable = false;
            if (tex != null) {
                base.textureMaterial = new SSTextureMaterial (tex);
            }

            this.cameraScene3d = cameraScene3d;
		}

		protected new SInstancedSpriteData instanceData {
			get { return base.instanceData as SInstancedSpriteData; }
		}

        public void addUpdater(ISSpriteUpdater updater) 
        {
            _spriteUpdaters.Add(updater);
            updater.setupSprites(this.instanceData);
        }

        public void removeUpdater(ISSpriteUpdater updater)
        {
            _spriteUpdaters.Remove(updater);
            updater.releaseSprites(this.instanceData);
        }

		public override void Render (SSRenderConfig renderConfig)
		{
            var rc = cameraScene3d.renderConfig;
            Matrix4 viewProjMat3d = rc.invCameraViewMatrix * rc.projectionMatrix;
            RectangleF clientRect = OpenTKHelper.GetClientRect();

            foreach (var updater in _spriteUpdaters) {
                updater.updateSprites(this.instanceData, ref viewProjMat3d, ref clientRect);
            }

			base.Render (renderConfig);
		}
	}

    public class SInstancedSpriteData : SSInstancesData
    {
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

        protected int _activeBlockLength = 0;
        protected int _numElements = 0;
        protected readonly int _capacity;
        protected List<int> _fragmentedVacantSlots = new List<int>();

        #region implement SSInstancesData
        public override int capacity { get { return _capacity; } }
        public override int activeBlockLength { get { return _activeBlockLength; } }
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

        public SInstancedSpriteData(int capacity)
        {
            _capacity = capacity;

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

            _positions = new SSAttributeVec3[_activeBlockLength];
            #endif
        }

        public int requestSlot()
        {
            ++_numElements;
            if (_numElements > _capacity) {
                throw new Exception ("SInstancedSpriteData: capacity overflow");
            }

            if (_fragmentedVacantSlots.Count > 0) {
                // pick the first "vacant" slot
                int retIdx = _fragmentedVacantSlots [0];
                _fragmentedVacantSlots.RemoveAt(0);
                return retIdx;
            } else {
                int retIdx = _activeBlockLength;
                ++_activeBlockLength;
                return retIdx;
            }
        }

        public int[] requestSlots(int numSlots)
        {
            int[] ret = new int[numSlots];
            for (int i = 0; i < numSlots; ++i) {
                ret [i] = requestSlot();
            }
            return ret;
        }

        public void releaseSlot(int slotIdx)
        {
            --_numElements;
            if (_numElements < 0) {
                throw new Exception ("SInstancedSpriteData: erroneous sprite release");
            }
            if (slotIdx < activeBlockLength - 1) {
                // release, updating fragmented slots in order so they can be refilled first
                int fragInsertIdx = 0;
                while (fragInsertIdx < _fragmentedVacantSlots.Count
                    && _fragmentedVacantSlots [fragInsertIdx] < slotIdx) {
                    ++fragInsertIdx;
                }
                _fragmentedVacantSlots.Insert(fragInsertIdx, slotIdx);
                // make sure the vacant slot isn't drawn
                writePosition(slotIdx, new Vector2(float.NaN));
            } else if (slotIdx == _activeBlockLength - 1) {
                --_activeBlockLength;
                int i;
                while ( (i =_fragmentedVacantSlots.LastIndexOf(_activeBlockLength-1)) != -1) {
                    _fragmentedVacantSlots.RemoveAt(i);
                    --_activeBlockLength;
                }
            } else { // slotIdx >= activeBlockLength
                throw new Exception ("SInstancedSpriteData: erroneous sprite release");
            }
        }

        public void releaseSlots(int[] slotIdxs)
        {
            foreach (int s in slotIdxs) {
                releaseSlot(s);
            }
        }

        public void writePosition(int idx, Vector2 pos) 
        {
            var newPos = new Vector3 (pos.X, pos.Y, 0f);
            writeDataIfNeeded(ref _positions, idx, new SSAttributeVec3 (newPos));
        }

        public void writeMasterScale(int idx, float scale) 
        {
            writeDataIfNeeded(ref _masterScales, idx, new SSAttributeFloat (scale));
        }

        public void writeComponentScale(int idx, Vector2 compScale) 
        {
            writeDataIfNeeded(ref _componentScalesXY, idx, new SSAttributeVec2 (compScale));
        }

        public void writeColor(int idx, Color4 color) 
        {
            var newColor = Color4Helper.ToUInt32(color);
            writeDataIfNeeded(ref _colors, idx, new SSAttributeColor(newColor));
        }

        public void writeOrientationZ(int idx, float orientationZ)
        {
            writeDataIfNeeded(ref _orientationsZ, idx, new SSAttributeFloat(orientationZ));
        }

        public void writeRect(int idx, RectangleF rect) {
            writeDataIfNeeded(ref _spriteOffsetsU, idx, new SSAttributeFloat (rect.X));
            writeDataIfNeeded(ref _spriteOffsetsV, idx, new SSAttributeFloat (rect.Y));
            writeDataIfNeeded(ref _spriteSizesU, idx, new SSAttributeFloat (rect.Width));
            writeDataIfNeeded(ref _spriteSizesV, idx, new SSAttributeFloat (rect.Height));
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
                    array = new T[_capacity];
                    for (int i = 0; i < _capacity; ++i) {
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

