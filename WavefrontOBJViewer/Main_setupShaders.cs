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
		
#if false	
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
			GL.AttachShader(ProgramID,vertexShader.ShaderID);
#endif

#if false
this.fragmentShader = new SSShader(ShaderType.FragmentShader, "helloFragment",
@"#version 120
 
// varying vec3 lightDir,normal;

uniform sampler2D diffTex;
uniform sampler2D specTex;
uniform sampler2D ambiTex;
uniform sampler2D bumpTex;
 
void main()
{
    vec3 lightDir = gl_LightSource[0].position;
    vec3 normal = gl_Normal;
    
    vec3 ct,cf;
    vec4 texel;
    float intensity,at,af;
    intensity = max(dot(lightDir,normalize(normal)),0.0);
 
    cf = intensity * (gl_FrontMaterial.diffuse).rgb +
                      gl_FrontMaterial.ambient.rgb;
    af = gl_FrontMaterial.diffuse.a;
    
    // lookup both textures
    texel = texture2D(diffTex,gl_TexCoord[0].st)+
            texture2D(specTex,gl_TexCoord[0].st); 
            
            
    ct = texel.rgb;
    at = texel.a;
    gl_FragColor = vec4(ct * cf, at * af);
 
}
 ");
			GL.AttachShader(ProgramID,fragmentShader.ShaderID);
#endif

this.fragmentShader = new SSShader(ShaderType.FragmentShader, "helloFragment2",
@"#version 120
 
uniform sampler2D diffTex;
uniform sampler2D specTex;
uniform sampler2D ambiTex;
uniform sampler2D bumpTex;

void main() {
  gl_FragColor = texture2D(diffTex, gl_TexCoord[0].st) + texture2D(ambiTex, gl_TexCoord[0].st);
}");
			GL.AttachShader(ProgramID,fragmentShader.ShaderID);
			
			
			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

			this.shaderPgm = new SSShaderProgram(ProgramID);
		}
	}
}


			
						
												
			
