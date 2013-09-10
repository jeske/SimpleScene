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
			Console.WriteLine(shaderName + " - " + GL.GetShaderInfoLog(ShaderID));
		}
	}
	
	public class SSShaderProgram {
		public int ProgramID;
		public SSShaderProgram(int id) {
			this.ProgramID = id;
		}
	}
}

