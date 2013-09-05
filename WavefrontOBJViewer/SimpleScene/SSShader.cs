using System;

using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	public class SSShader
	{
		public int ShaderID;
		
		ShaderType type;
		string shaderProgramText;
		
		public SSShader (ShaderType type, string shaderProgramText)
		{
			this.type = type;
			this.shaderProgramText = shaderProgramText;

			this.loadShader();
		}
		
		private void loadShader() {			
			ShaderID = GL.CreateShader(this.type);
			GL.ShaderSource(ShaderID, this.shaderProgramText);
			GL.CompileShader(ShaderID);	
			Console.WriteLine(GL.GetShaderInfoLog(ShaderID));
		}
	}
}

