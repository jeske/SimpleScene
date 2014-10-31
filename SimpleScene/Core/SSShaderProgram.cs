using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using SimpleScene;

public class SSShaderProgram {
	// BUG: this not valid, because there can be multiple simultaneous GL rendering contexts... 
	//     the scene or scene.renderConfig should hold the active shader program. - jeske

    private static SSShaderProgram s_activeProgram = null;

    protected readonly int m_programID;

    static SSShaderProgram() {
        DeactivateAll ();
    }

    internal SSShaderProgram() {
        m_programID = GL.CreateProgram();
    }

    ~SSShaderProgram() {
        GL.DeleteProgram (m_programID);
    }

    public bool IsActive {
        get { return s_activeProgram == this; }
    }

    #region user interface
    public void Activate() {
        if (s_activeProgram != this) {
            s_activeProgram = this;
            GL.UseProgram (m_programID);
        }
    }

    public static void DeactivateAll() {
        if (s_activeProgram != null) {
            s_activeProgram = null;
            GL.UseProgram (0);
        }
    }

    public void Deactivate() {
        SSShaderProgram.DeactivateAll ();
    }
    #endregion

    #region private and utilities
    protected void assertActive() {
        if (!IsActive) {
            throw new Exception ("Shader is not active");
        }
    }

    protected void checkErrors() {
        ErrorCode glerr;
        if ((glerr = GL.GetError ()) != ErrorCode.NoError) {
            throw new Exception (String.Format ("GL Error: {0}", glerr));
        }
    }

    protected void link() {
        GL.LinkProgram(m_programID);
        Console.WriteLine(GL.GetProgramInfoLog(m_programID));
    }

    protected void attach(SSShader shader) {
        GL.AttachShader(m_programID, shader.ShaderID);
    }

    protected int getUniLoc(string name) {
        return GL.GetUniformLocation(m_programID, name);
    }
    #endregion
}