using System;

using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public interface ISSInstancableShaderProgram
    {
        void Activate();

        int AttrInstancePos { get; }
        int AttrInstanceOrientationXY { get; }
        int AttrInstanceOrientationZ { get; }
        int AttrInstanceMasterScale { get; }
        int AttrInstanceComponentScaleXY { get; }
        int AttrInstanceComponentScaleZ { get; }
        int AttrInstanceColor { get; }
        int AttrInstanceSpriteOffsetU { get; }
        int AttrInstanceSpriteOffsetV { get; }
        int AttrInstanceSpriteSizeU { get; }
        int AttrInstanceSpriteSizeV { get; }

        int AttrTexCoord { get; }
        int AttrNormal { get; }
    }

	public class SSInstanceShaderProgram : SSMainShaderProgram, ISSInstancableShaderProgram
    {
		#region attribute locations
        private readonly int a_instancePos;
		private readonly int a_instanceOrientationXY;
		private readonly int a_instanceOrientationZ;
        private readonly int a_instanceMasterScale;
		private readonly int a_instanceComponentScaleXY;
		private readonly int a_instanceComponentScaleZ;
        private readonly int a_instanceColor;

        private readonly int a_instanceSpriteOffsetU;
        private readonly int a_instanceSpriteOffsetV;
        private readonly int a_instanceSpriteSizeU;
        private readonly int a_instanceSpriteSizeV;

		private readonly int a_texCoord;
		private readonly int a_normal;
        #endregion

        public int AttrInstancePos {
            get { return a_instancePos; }
        }

		public int AttrInstanceOrientationXY {
			get { return a_instanceOrientationXY; }
        }

		public int AttrInstanceOrientationZ {
			get { return a_instanceOrientationZ; }
		}

        public int AttrInstanceMasterScale {
            get { return a_instanceMasterScale; }
        }

		public int AttrInstanceComponentScaleXY {
            get { return a_instanceComponentScaleXY; }
        }

		public int AttrInstanceComponentScaleZ {
			get { return a_instanceComponentScaleZ; }
		}

        public int AttrInstanceColor {
            get { return a_instanceColor; }
        }

        public int AttrInstanceSpriteOffsetU {
            get { return a_instanceSpriteOffsetU; }
        }

        public int AttrInstanceSpriteOffsetV {
            get { return a_instanceSpriteOffsetV; }
        }

        public int AttrInstanceSpriteSizeU {
            get { return a_instanceSpriteSizeU; }
        }

        public int AttrInstanceSpriteSizeV {
            get { return a_instanceSpriteSizeV; }
        }

		public int AttrTexCoord {
			get { return a_texCoord; }
		}

		public int AttrNormal {
			get { return a_normal; }
		}

		public SSInstanceShaderProgram()
			: base("#define INSTANCE_DRAW\n")
        {
			// attributes
            a_instancePos = getAttrLoc("instancePos");
			a_instanceOrientationXY = getAttrLoc("instanceOrientationXY");
			a_instanceOrientationZ = getAttrLoc ("instanceOrientationZ");
            a_instanceMasterScale = getAttrLoc("instanceMasterScale");
			a_instanceComponentScaleXY = getAttrLoc("instanceComponentScaleXY");
			a_instanceComponentScaleZ = getAttrLoc("instanceComponentScaleZ");
            a_instanceColor = getAttrLoc("instanceColor");

            //a_instanceSpriteIndex = getAttrLoc("instanceSpriteIndex");
            a_instanceSpriteOffsetU = getAttrLoc("instanceSpriteOffsetU");
            a_instanceSpriteOffsetV = getAttrLoc("instanceSpriteOffsetV");
            a_instanceSpriteSizeU = getAttrLoc("instanceSpriteSizeU");
            a_instanceSpriteSizeV = getAttrLoc("instanceSpriteSizeV");

			a_texCoord = getAttrLoc ("attrTexCoord");
			a_normal = getAttrLoc ("attrNormal");

            // uniform locations for texture setup only
            int uniPrimaryTex = getUniLoc("primaryTexture");

            // bind shader uniform variable handles to GL texture-unit numbers
            GL.Uniform1(uniPrimaryTex, 0); // Texture.Texture0

            // errors?
			m_isValid = checkGlValid();
        }
    }
}

