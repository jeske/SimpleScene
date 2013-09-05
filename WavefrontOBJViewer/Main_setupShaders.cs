using System;

using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	partial class Game : OpenTK.GameWindow
	{
		SSShader vertexShader;
		SSShader fragmentShader;
		int ProgramID;
		
		public void setupShaders() {
			ProgramID = GL.CreateProgram();
			
vertexShader = new SSShader(ShaderType.VertexShader,
@"#version 120
 
in vec3 vPosition;
in  vec3 vColor;
out vec4 color;
uniform mat4 modelview;
 
void
main()
{
    gl_Position = modelview * vec4(vPosition, 1.0);
 
    color = vec4( vColor, 1.0);		
		
		}
	}
}
");
			GL.AttachShader(ProgramID,vertexShader.ShaderID);

this.fragmentShader = new SSShader(ShaderType.FragmentShader,
@"#version 120
 
in vec4 color;
out vec4 outputColor;
 
void
main()
{
    outputColor = color;
}");
			GL.AttachShader(ProgramID,fragmentShader.ShaderID);

			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

		}
	}
}

			
						
												
			
