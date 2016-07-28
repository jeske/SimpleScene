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

        private readonly int a_cylinderPos;
        private readonly int a_cylinderAxis;
        private readonly int a_cylinderWidth;
        private readonly int a_cylinderLength;
        private readonly int a_cylinderColor;
        #endregion

        public float UniScreenWidth { set { GL.Uniform1(u_screenWidth, value); } }
        public float UniScreenHeight { set { GL.Uniform1(u_screenHeight, value); } }
        public Matrix4 UniViewMatrix { set { GL.UniformMatrix4(u_viewMatrix, false, ref value); } }
        public Matrix4 UniViewMatrixInverse { set { GL.UniformMatrix4(u_viewMatrixInverse, false, ref value); } }
                
        public int AttrCylinderPos { get { return a_cylinderPos; } }
        public int AttrCylinderAxis { get { return a_cylinderAxis; } }
        public int AttrCylinderLength { get { return a_cylinderLength; } }
        public int AttrCylinderWidth { get { return a_cylinderWidth; } }
        public int AttrCylinderColor { get { return a_cylinderColor; } }

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

            a_cylinderPos = getAttrLoc("cylinderCenter");
            a_cylinderAxis = getAttrLoc("cylinderAxis");
            a_cylinderWidth = getAttrLoc("cylinderWidth");
            a_cylinderLength = getAttrLoc("cylinderLength");
            a_cylinderColor = getAttrLoc("cylinderColor");


            m_isValid = checkGlValid();
        }
    }
}

