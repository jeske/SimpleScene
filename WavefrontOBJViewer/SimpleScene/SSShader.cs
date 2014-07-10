// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
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
			
			if (log_lines.Length > 0) {
				Console.WriteLine("-- {0} --",this.shaderName);
			}
			foreach (var line in log_lines) {
				// print log line
				Console.WriteLine(line);
				
				// try to print the source-line
				var match = regex.Match(line);
				if (match.Success) {
					int lineno = int.Parse(match.Groups[2].Value);
					Console.WriteLine("   > " + programLines[lineno-1]);	
				}
			}
		}
		
	}
	
	public class SSShaderProgram {
		public int ProgramID;
		public SSShaderProgram(int id) {
			this.ProgramID = id;
		}
	}
}

