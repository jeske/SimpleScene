// Copyright(C) David W. Jeske, 2013
// Released to the public domain. 

using System;
using System.IO;
using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL;

using Util;


namespace SimpleScene
{

    public class SSShaderLoadException : Exception { 
        public SSShaderLoadException(String info) : base(info) {}
    }

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
		}
		
        public void LoadShader() {			
			ShaderID = GL.CreateShader(this.type);
			GL.ShaderSource(ShaderID, this.shaderProgramText);
			GL.CompileShader(ShaderID);	
			int compiled;
			GL.GetShader(ShaderID,ShaderParameter.CompileStatus,out compiled);
			this.PrintPrettyShaderInfoLog();
			if (compiled == (int)OpenTK.Graphics.OpenGL.All.False) {
			    Console.WriteLine("** Shader Compile Failed **");
			    ShaderID = 0;
                throw new SSShaderLoadException(this.shaderName);
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

        public void Prepend(string prefix)
        {
			if (prefix == null) return;
			string pattern = @"#version \d+\r?\n";
			Regex regex = new Regex (pattern);
			Match match = regex.Match (shaderProgramText);
			if (match.Success) {
				shaderProgramText 
					= shaderProgramText.Insert (match.Index + match.Length, prefix);
			}
        }
	}

    public class SSVertexShader : SSShader
    {
        public SSVertexShader(string path)
            : base(ShaderType.VertexShader, Path.GetFileName(path), 
                SSAssetManager.OpenStream(path).AsString())
        { }
    }

    public class SSFragmentShader : SSShader
    {
        public SSFragmentShader(string path)
            : base(ShaderType.FragmentShader, Path.GetFileName(path), 
                SSAssetManager.OpenStream(path).AsString())
        { }
    }

    public class SSGeometryShader : SSShader
    {
        public SSGeometryShader(string path)
            : base(ShaderType.GeometryShader, Path.GetFileName(path), 
                SSAssetManager.OpenStream(path).AsString())
        { }
    }
}

