// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	partial class Game : OpenTK.GameWindow
	{
		SSShader vertexShader;
		SSShader fragmentShader;
		SSShaderProgram shaderPgm;
		
		public void setupShaders() {
			int ProgramID = GL.CreateProgram();
			
vertexShader = new SSShader(ShaderType.VertexShader, "helloVertex",
@"#version 120
 
 void main(void) {
    // normal MVP transform       
    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;       
    
    vec3 N = normalize(gl_NormalMatrix * gl_Normal);       
    vec4 V = gl_ModelViewMatrix * gl_Vertex;       
    vec3 L = normalize(gl_LightSource[0].position - V.xyz);       
    
    // output the diffuse color       
    float NdotL = dot(N, L);       
    gl_FrontColor = gl_Color * vec4(max(0.0, NdotL)); 
}
");
			// GL.AttachShader(ProgramID,vertexShader.ShaderID);

this.fragmentShader = new SSShader(ShaderType.FragmentShader, "helloFragment",
@"#version 120
 
 void main(void) {       
     //  Mix primary and secondary colors, 50/50       
     gl_FragColor = mix(gl_Color, vec4(vec3(gl_SecondaryColor), 1.0), 0.5); 
 }
 
 ");
			GL.AttachShader(ProgramID,fragmentShader.ShaderID);

			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

			this.shaderPgm = new SSShaderProgram(ProgramID);
		}
	}
}

			
						
												
			
