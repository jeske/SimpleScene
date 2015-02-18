using System;

using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSInstanceShaderProgram : SSShaderProgram
    {
        private static readonly string c_ctx = "./Shaders/Instancing";

        #region Shaders
        private readonly SSShader m_vertexShader;
        private readonly SSShader m_fragmentShader;
        //private readonly SSShader m_geometryShader;
        #endregion

        #region Uniform Locations
        private readonly int u_instanceBillboardingEnabled;

        private readonly int a_instancePos;
        private readonly int a_instanceOrientation;
        private readonly int a_instanceMasterScale;
        private readonly int a_instanceComponentScale;
        private readonly int a_instanceColor;

        private readonly int a_instanceSpriteIndex;
        private readonly int a_instanceSpriteOffsetU;
        private readonly int a_instanceSpriteOffsetV;
        private readonly int a_instanceSpriteSizeU;
        private readonly int a_instanceSpriteSizeV;
        #endregion

        #region Uniform Modifiers
        public bool UniInstanceBillboardingEnabled {
            set { assertActive(); GL.Uniform1(u_instanceBillboardingEnabled, value ? 1 : 0); } 
        }
        #endregion

        public int AttrInstancePos {
            get { return a_instancePos; }
        }

        public int AttrInstanceOrientation {
            get { return a_instanceOrientation; }
        }

        public int AttrInstanceMasterScale {
            get { return a_instanceMasterScale; }
        }

        public int AttrInstanceComponentScale {
            get { return a_instanceComponentScale; }
        }

        public int AttrInstanceColor {
            get { return a_instanceColor; }
        }

        public int AttrInstanceSpriteIndex {
            get { return a_instanceSpriteIndex; }
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

        public SSInstanceShaderProgram()
        {
            m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_ctx, "instance_vertex.glsl");
            m_vertexShader.LoadShader();
            attach(m_vertexShader);

            m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_ctx, "instance_fragment.glsl");
            m_fragmentShader.LoadShader();
            attach(m_fragmentShader);

            link();
            Activate ();

            // uniform(s)
            u_instanceBillboardingEnabled = getUniLoc("instanceBillboardingEnabled");
            //UniInstanceBillboardingEnabled = false;

            // attributes
            a_instancePos = getAttrLoc("instancePos");
            a_instanceOrientation = getAttrLoc("instanceOrientation");
            a_instanceMasterScale = getAttrLoc("instanceMasterScale");
            a_instanceComponentScale = getAttrLoc("instanceComponentScale");
            a_instanceColor = getAttrLoc("instanceColor");

            a_instanceSpriteIndex = getAttrLoc("instanceSpriteIndex");
            a_instanceSpriteOffsetU = getAttrLoc("instanceSpriteOffsetU");
            a_instanceSpriteOffsetV = getAttrLoc("instanceSpriteOffsetV");
            a_instanceSpriteSizeU = getAttrLoc("instanceSpriteSizeU");
            a_instanceSpriteSizeV = getAttrLoc("instanceSpriteSizeV");

            // uniform locations for texture setup only
            int uniPrimaryTex = getUniLoc("primaryTexture");

            // bind shader uniform variable handles to GL texture-unit numbers
            GL.Uniform1(uniPrimaryTex, 0); // Texture.Texture0

            // errors?
            checkErrors();
        }
    }
}

