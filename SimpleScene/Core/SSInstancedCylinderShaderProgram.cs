using System;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSInstancedCylinderShaderProgram : SSShaderProgram
    {
        protected readonly string basePath = "./Shaders/Cylinder";

        protected readonly SSVertexShader _vertexShader;
        protected readonly SSFragmentShader _fragmentShader;

        #region attribute locations
        private readonly int a_cylinderPos;
        private readonly int a_cylinderAxis;
        private readonly int a_cylinderWidth;
        private readonly int a_cylinderLength;
        private readonly int a_cylinderColor;
        #endregion

        public int AttrCylinderPos { get { return a_cylinderPos; } }
        public int AttrCylinderAxis { get { return a_cylinderAxis; } }
        public int AttrCylinder { get { return a_cylinder; } }
        public int AttrCylinder { get { return a_cylinder; } }


        public SSInstancedCylinderShaderProgram (string preprocessorDefs = "INSTANCE_DRAW")
            : base(preprocessorDefs)
        {
            m_programID = GL.CreateProgram();

            _vertexShader = SSAssetManager.GetInstance<SSVertexShader>(
                basePath + "cylinder_instanced_vertex.glsl");
            _vertexShader.Prepend(preprocessorDefs);
            _vertexShader.LoadShader();
            attach(_vertexShader);

            _fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(
                basePath + "cylinder_fragment.glsl");
            _fragmentShader.Prepend(preprocessorDefs);
            _fragmentShader.LoadShader();
            attach(_fragmentShader);

            m_isValid = checkGlValid();
        }
    }
}

