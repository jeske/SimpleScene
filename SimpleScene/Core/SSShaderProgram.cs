using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using SimpleScene;

public class SSShaderProgram {
	// BUG: this not valid, because there can be multiple simultaneous GL rendering contexts... 
	//     the scene or scene.renderConfig should hold the active shader program. - jeske

    private static SSShaderProgram s_activeProgram = null;

    public int m_programID = -1;
	protected bool m_isValid = false;

	static SSShaderProgram() {
        DeactivateAll ();
    }

	internal SSShaderProgram() {
		m_programID = GL.CreateProgram();
    }

    ~SSShaderProgram() {
        // not valid if the GL context is gone...
        // GL.DeleteProgram (m_programID);
    }

    public bool IsActive {
        get { return s_activeProgram == this; }
    }

	/// <summary>
	/// Should return 'true' if any failure occured during compilation or linking
	/// </summary>
	public bool IsValid {
		get { return m_isValid; }
	}

    #region user interface
    public void Activate() {
        if (s_activeProgram != this) {
            s_activeProgram = this;
            GL.UseProgram (m_programID);
        }
    }

    public static void DeactivateAll() {
        s_activeProgram = null;
        GL.UseProgram (0);
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

	protected bool checkGlValid() {
        ErrorCode glerr;
		if ((glerr = GL.GetError ()) != ErrorCode.NoError) {
			Console.WriteLine (String.Format ("GL Error: {0}", glerr));
			return false;
		} else {
			return true;
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

    protected int getAttrLoc(string name) {
		int ret = GL.GetAttribLocation(m_programID, name);
		System.Console.WriteLine ("Attr (" + name + ") = " + ret);
		return ret;
    }

	public void debugLocations()
	{
		int activeAttr, maxLength;
		GL.GetProgram (m_programID, GetProgramParameterName.ActiveAttributes, out activeAttr);
		GL.GetProgram (m_programID, GetProgramParameterName.ActiveAttributeMaxLength, out maxLength);
		System.Collections.SortedList info = new System.Collections.SortedList();

		for (int i = 0; i < activeAttr; i++) {
			int size, length;
			ActiveAttribType type;
			System.Text.StringBuilder name = new System.Text.StringBuilder (maxLength);

			GL.GetActiveAttrib (m_programID, i, maxLength, out length, out size, out type, name);
			int location = GL.GetAttribLocation (m_programID, name.ToString ());
			info.Add((location >= 0) ? location : (location*i),
				String.Format("{0} {1} is at location {2}, size {3}",
					type.ToString(),
					name,
					location,
					size)
			);
		}
		foreach (int key in info.Keys) {
			Console.WriteLine (info [key]);
		}
	}
    #endregion
}