using System;
using System.Drawing; // for RectangleF
using OpenTK;
using OpenTK.Graphics;
using SimpleScene.Util;

namespace SimpleScene
{
    public abstract class SSInstancesData
    {
        protected static Element _readElement<Element>(Element[] array, int i)
        {
            return i >= array.Length ? array [0] : array [i];
        }

        public abstract int capacity { get; }
        public abstract int numElements { get; }
        public abstract int activeBlockLength { get; }
        public abstract float radius { get; }
		public abstract Vector3 center { get; }
        public abstract SSAttributeVec3[] positions { get; }

        public abstract SSAttributeVec2[] orientationsXY { get; }
        public abstract SSAttributeFloat[] orientationsZ { get; }
        public abstract SSAttributeColor[] colors { get; }
        public abstract SSAttributeFloat[] masterScales { get; }
        public abstract SSAttributeVec2[] componentScalesXY { get; }
        public abstract SSAttributeFloat[] componentScalesZ { get; }
        public abstract SSAttributeFloat[] spriteOffsetsU { get; }
        public abstract SSAttributeFloat[] spriteOffsetsV { get; }
        public abstract SSAttributeFloat[] spriteSizesU { get; }
        public abstract SSAttributeFloat[] spriteSizesV { get; }

        public virtual void sortByDepth(ref Matrix4 viewMatrix) { }
		public virtual void update(float elapsedS) { }
        public virtual void updateCamera(ref Matrix4 model, ref Matrix4 view, ref Matrix4 projection) { }

        public bool isValid(int slotIdx)
        {
            if (slotIdx >= positions.Length) {
                slotIdx = 0;
            }
            var pos = positions [slotIdx].Value; 
            return !float.IsNaN(pos.X) && !float.IsNaN(pos.Y) && !float.IsNaN(pos.Y);
        }

        public Vector3 readPosition(int i)
        {
            return _readElement(positions, i).Value;
        }

        public float readMasterScale(int i)
        {
            return _readElement(masterScales, i).Value;
        }

        public Vector2 readComponentScaleXY(int i)
        {
            return _readElement(componentScalesXY, i).Value;
        }

        public float readComponentScaleZ(int i)
        {
            return _readElement(componentScalesZ, i).Value;
        }

        public Vector3 readComponentScale(int i)
        {
            var xy = readComponentScaleXY(i);
            return new Vector3 (xy.X, xy.Y, readComponentScaleZ(i));
        }

        public Vector2 readOrientationXY(int i)
        {
            return _readElement(orientationsXY, i).Value;
        }

        public float readOrientationZ(int i)
        {
            return _readElement(orientationsZ, i).Value;
        }

        public Vector3 readOrientation(int i)
        {
            var xy = this.readOrientationXY(i);
            return new Vector3 (xy.X, xy.Y, readOrientationZ(i));
        }

        public float readSpriteOffsetU(int i)
        {
            return _readElement(spriteOffsetsU, i).Value;
        }

        public float readSpriteOffsetV(int i)
        {
            return _readElement(spriteOffsetsV, i).Value;
        }

        public float readSpriteSizeU(int i)
        {
            return _readElement(spriteSizesU, i).Value;
        }

        public float readSpriteSizeV(int i)
        {
            return _readElement(spriteSizesV, i).Value;
        }

        public RectangleF readRect(int i)
        {
            return new RectangleF (readSpriteOffsetU(i), readSpriteOffsetV(i),
                                   readSpriteSizeU(i), readSpriteSizeV(i));
        }

        public Color4 readColor (int i)
        {
            return Color4Helper.FromUInt32(_readElement(colors, i).Color);
        }
    }
}

