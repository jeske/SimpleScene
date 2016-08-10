using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSInstancedCylinderShaderProgram : SSShaderProgram
    {
        protected readonly string basePath = "./Shaders/Cylinder/";

        protected readonly SSVertexShader _vertexShader;
        protected readonly SSFragmentShader _fragmentShader;

        #region attribute locations
        private readonly int u_screenWidth;
        private readonly int u_screenHeight;
        private readonly int u_viewMatrix;
        private readonly int u_viewMatrixInverse;
        private readonly int u_distanceToAlpha;
        private readonly int u_alphaMin;
        private readonly int u_alphaMax;

        private readonly int a_cylinderPos;
        private readonly int a_cylinderAxis;
        private readonly int a_prevJointAxis;
        private readonly int a_nextJointAxis;
        private readonly int a_cylinderWidth;
        private readonly int a_cylinderLength;
        private readonly int a_cylinderColor;
        private readonly int a_cylinderInnerColor;
        private readonly int a_innerColorRatio;
        private readonly int a_outerColorRatio;
        #endregion

        public float UniScreenWidth { set { GL.Uniform1(u_screenWidth, value); } }
        public float UniScreenHeight { set { GL.Uniform1(u_screenHeight, value); } }
        public Matrix4 UniViewMatrix { set { GL.UniformMatrix4(u_viewMatrix, false, ref value); } }
        public Matrix4 UniViewMatrixInverse { set { GL.UniformMatrix4(u_viewMatrixInverse, false, ref value); } }
        public float UniDistanceToAlpha { set { GL.Uniform1(u_distanceToAlpha, value); } }
        public float UniAlphaMin { set { GL.Uniform1(u_alphaMin, value); } }
        public float UniAlphaMax { set { GL.Uniform1(u_alphaMax, value); } }
                
        public int AttrCylinderPos { get { return a_cylinderPos; } }
        public int AttrCylinderAxis { get { return a_cylinderAxis; } }
        public int AttrPrevJointAxis { get { return a_prevJointAxis; } }
        public int AttrNextJointAxis { get { return a_nextJointAxis; } }
        public int AttrCylinderLength { get { return a_cylinderLength; } }
        public int AttrCylinderWidth { get { return a_cylinderWidth; } }
        public int AttrCylinderColor { get { return a_cylinderColor; } }
        public int AttrCylinderInnerColor { get { return a_cylinderInnerColor; } }
        public int AttrInnerColorRatio { get { return a_innerColorRatio; } }
        public int AttrOuterColorRatio { get { return a_outerColorRatio; } }

        public SSInstancedCylinderShaderProgram (string preprocessorDefs = "#define INSTANCE_DRAW")
        {
            m_programID = GL.CreateProgram();

            _vertexShader = SSAssetManager.GetInstance<SSVertexShader>(
                basePath + "cylinder_vertex.glsl");
            _vertexShader.Prepend(preprocessorDefs);
            _vertexShader.LoadShader();
            attach(_vertexShader);

            _fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(
                basePath + "cylinder_fragment.glsl");
            _fragmentShader.Prepend(preprocessorDefs);
            _fragmentShader.LoadShader();
            attach(_fragmentShader);

            link();

            Activate();

            u_screenWidth = getUniLoc("screenWidth");
            u_screenHeight = getUniLoc("screenHeight");
            u_viewMatrix = getUniLoc("viewMatrix");
            u_viewMatrixInverse = getUniLoc("viewMatrixInverse");
            u_distanceToAlpha = getUniLoc("distanceToAlpha");
            u_alphaMin = getUniLoc("alphaMin");
            u_alphaMax = getUniLoc("alphaMax");

            a_cylinderPos = getAttrLoc("cylinderCenter");
            a_cylinderAxis = getAttrLoc("cylinderAxis");
            a_prevJointAxis = getAttrLoc("prevJointAxis");
            a_nextJointAxis = getAttrLoc("nextJointAxis");
            a_cylinderWidth = getAttrLoc("cylinderWidth");
            a_cylinderLength = getAttrLoc("cylinderLength");
            a_cylinderColor = getAttrLoc("cylinderColor");
            a_cylinderInnerColor = getAttrLoc("cylinderInnerColor");
            a_innerColorRatio = getAttrLoc("innerColorRatio");
            a_outerColorRatio = getAttrLoc("outerColorRatio");

            m_isValid = checkGlValid();

            this.UniDistanceToAlpha = 0.1f;
            this.UniAlphaMin = 0.0f;
            this.UniAlphaMax = 0.6f;
        }
    }
}

