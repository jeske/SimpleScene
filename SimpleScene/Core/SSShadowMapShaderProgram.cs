using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace SimpleScene
{
    public class SSShadowMapShaderProgram : SSShaderProgram
    {
        #region Constants
        private const string c_context = "./Shaders/shadowmap/";
        private const string c_vertexFilename = "shadowmap_vertex.glsl";
        private const string c_fragmentFilename = "shadowmap_fragment.glsl";
        #endregion

        #region Shaders
        private readonly SSShader m_vertexShader;
        private readonly SSShader m_fragmentShader;
        #endregion

        #region Uniform Locations
        private readonly int u_mvpMatrix;
        #endregion

        #region Uniform Modifiers
        public Matrix4 MVPMatrix {
            set { assertActive(); GL.UniformMatrix4(u_mvpMatrix, false, ref value); }
        }
        #endregion

        public SSShadowMapShaderProgram ()
        {

            m_vertexShader = SSAssetManager.GetInstance<SSVertexShader>(c_context, c_vertexFilename);
            attach(m_vertexShader);
            m_fragmentShader = SSAssetManager.GetInstance<SSFragmentShader>(c_context, c_fragmentFilename);
            attach(m_fragmentShader);
            link();
            GL.BindAttribLocation(m_programID, 0, "vertexPosition_modelspace");
            GL.BindAttribLocation(m_programID, 0, "fragmentDepth");
            Activate();
            u_mvpMatrix = getUniLoc("depthMVP");
            Deactivate();
        }
    }
}

