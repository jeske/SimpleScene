using System;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSCylinderShaderProgram : SSShaderProgram
    {
        protected readonly string basePath = "./Shaders/Cylinder";

        protected readonly SSVertexShader _vertexShader;
        protected readonly SSFragmentShader _fragmentShader;

        public SSInstancedCylinderShaderProgram (string preprocessorDefs = null)
            : base(preprocessorDefs)
        {
            m_programID = GL.CreateProgram();

            _vertexShader = SSAssetManager.GetInstance<SSVertexShader>(
                "cylinder_instanced_vertex.glsl");
            _vertexShader.Prepend(preprocessorDefs);
            _vertexShader.LoadShader();
            attach(_vertexShader);

            _fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(
                "cylinder_fragment.glsl");
            _fragmentShader.Prepend(preprocessorDefs);
            _fragmentShader.LoadShader();
            attach(_fragmentShader);

            m_isValid = checkGlValid();
        }
    }
}

