// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;

using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	public class SSShader
	{
		public int ShaderID;
		
		ShaderType type;
		string shaderProgramText;
		
		string shaderName;

		public SSShader (ShaderType type, string shaderName, string shaderProgramText)
		{
			this.type = type;
			this.shaderProgramText = shaderProgramText;
			this.shaderName = shaderName;

			this.loadShader();
		}
		
		private void loadShader() {			
			ShaderID = GL.CreateShader(this.type);
			GL.ShaderSource(ShaderID, this.shaderProgramText);
			GL.CompileShader(ShaderID);	
			int compiled;
			GL.GetShader(ShaderID,ShaderParameter.CompileStatus,out compiled);
			this.PrintPrettyShaderInfoLog();
			if (compiled == (int)OpenTK.Graphics.OpenGL.All.False) {
			    Console.WriteLine("** Shader Compile Failed **");
			    ShaderID = 0;
			}
		}
		
		private void PrintPrettyShaderInfoLog() {
			string[] programLines = this.shaderProgramText.Split('\n');
			string[] log_lines = GL.GetShaderInfoLog(ShaderID).Split('\n');
		
			// example line:			
			// ERROR: 0:36: Use of undeclared identifier 'diffuseMaterial'

			var regex = new System.Text.RegularExpressions.Regex(@"([0-9]+):([0-9]+):");
			var regex2 = new System.Text.RegularExpressions.Regex(@"([0-9]+)\(([0-9]+)\)");

			if (log_lines.Length > 0) {
				Console.WriteLine("-- {0} --",this.shaderName);
			}
			foreach (var line in log_lines) {
				// print log line
				Console.WriteLine(line);

				// try to print the source-line
				var match = regex.Match(line);
				if (!match.Success) {
					match = regex2.Match (line);
				}
				if (match.Success) {
					int lineno = int.Parse(match.Groups[2].Value);
					Console.WriteLine("   > " + programLines[lineno-1]);	
				}
			}
		}
		
	}

    public class SSVertexShader : SSShader
    {
        public SSVertexShader(SSAssetManager.Context context, string filename)
            : base(ShaderType.VertexShader,
                   filename, 
                   context.Open(filename).AsString())
        { }
    }

    public class SSFragmentShader : SSShader
    {
        public SSFragmentShader(SSAssetManager.Context context, string filename)
            : base(ShaderType.FragmentShader,
                   filename,
                   context.Open(filename).AsString()) 
        { }
    }

    public class SSGeometryShader : SSShader
    {
        public SSGeometryShader(SSAssetManager.Context context, string filename)
            : base(ShaderType.GeometryShader,
                   filename,
                   context.Open(filename).AsString()) { }
    }

	public class SSShaderProgram {
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

		#region utilities
		protected void assertActive() {
			if (!IsActive) {
				throw new Exception ("Shader is not active");
			}
		}
		#endregion
	}
}

