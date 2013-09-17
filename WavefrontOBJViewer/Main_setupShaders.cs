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

#if false
this.fragmentShader = new SSShader(ShaderType.FragmentShader, "helloFragment2",
@"#version 120
 
uniform sampler2D diffTex;
uniform sampler2D specTex;
uniform sampler2D ambiTex;
uniform sampler2D bumpTex;

void main() {
  vec3 ambientColor = texture2D(ambiTex, gl_TexCoord[0].st) * gl_FrontMaterial.ambient;
  vec3 diffuseColor = texture2D(diffTex, gl_TexCoord[0].st) * gl_FrontMaterial.diffuse;
  vec3 specularColor = texture2D(specTex, gl_TexCoord[0].st) * gl_FrontMaterial.specular;

  gl_FragColor = ambientColor + diffuseColor + specularColor;
}");
			GL.AttachShader(ProgramID,fragmentShader.ShaderID);
#endif



// GLSL fragment shader and bump mapping tutorial
// http://fabiensanglard.net/bumpMapping/index.php



vertexShader = new SSShader(ShaderType.VertexShader, "bumpVertex",
@"#version 120

	attribute vec3 tangent;
	
	varying vec3 lightVec;
	varying vec3 halfVec;
	varying vec3 eyeVec;
	
	varying vec3 n;
	varying vec3 VV;
	
	varying vec3 vertexNormal;

void main()
{
	gl_TexCoord[0] =  gl_MultiTexCoord0;
	
	// Building the matrix Eye Space -> Tangent Space
	n = normalize (gl_NormalMatrix * gl_Normal);
	vec3 t = normalize (gl_NormalMatrix * tangent);
	vec3 b = cross (n, t);
	
	vec3 vertexPosition = vec3(gl_ModelViewMatrix *  gl_Vertex);
	vec3 lightDir = normalize(gl_LightSource[0].position.xyz - vertexPosition);
		
	// transformed vertex position.
	VV = vec3(gl_ModelViewMatrix * gl_Vertex);
	
		
		
	// transform light and half angle vectors by tangent basis
	vec3 v;
	v.x = dot (lightDir, t);
	v.y = dot (lightDir, b);
	v.z = dot (lightDir, n);
	lightVec = normalize (v);
	
	  
	v.x = dot (vertexPosition, t);
	v.y = dot (vertexPosition, b);
	v.z = dot (vertexPosition, n);
	eyeVec = normalize (v);
	
	
	vertexPosition = normalize(vertexPosition);
	
	/* Normalize the halfVector to pass it to the fragment shader */

	// No need to divide by two, the result is normalized anyway.
	// vec3 halfVector = normalize((vertexPosition + lightDir) / 2.0); 
	vec3 halfVector = normalize(vertexPosition + lightDir);
	v.x = dot (halfVector, t);
	v.y = dot (halfVector, b);
	v.z = dot (halfVector, n);

	// No need to normalize, t,b,n and halfVector are normal vectors.
	//normalize (v);
	halfVec = v ; 
	  
	vertexNormal = gl_Normal;
	gl_Position = ftransform();
}");
			GL.AttachShader(ProgramID,vertexShader.ShaderID);
			
			
this.fragmentShader = new SSShader(ShaderType.FragmentShader, "bumpFragment",
@"#version 120
 
uniform sampler2D diffTex;
uniform sampler2D specTex;
uniform sampler2D ambiTex;
uniform sampler2D bumpTex;
		
// New bumpmapping
varying vec3 lightVec;
varying vec3 halfVec;
varying vec3 eyeVec;

varying vec3 n;
varying vec3 VV;

varying vec3 vertexNormal;

void main()
{
	vec4 outputColor;

	// lighting strength
	vec4 ambientStrength = gl_FrontMaterial.ambient;
	vec4 diffuseStrength = gl_FrontMaterial.diffuse;
	vec4 specularStrength = gl_FrontMaterial.specular;
	vec3 lightPosition = normalize(gl_LightSource[0].position.xyz - VV);

	// diffuse color baseline
	// http://www.clockworkcoders.com/oglsl/tutorial5.htm
	vec4 diffuseMaterial = texture2D (diffTex, gl_TexCoord[0].st);
	//outputColor += diffuseMaterial * (ambientStrength + diffuseStrength);
	outputColor += diffuseMaterial * max(dot(n, lightPosition), 0.0);
	
	// ambient glow map
	vec4 glowFactor = vec4(0.5);
	outputColor = mix(outputColor, texture2D (ambiTex, gl_TexCoord[0].st), glowFactor);
	
	// lookup normal from normal map, move from [0,1] to  [-1, 1] range, normalize
	vec3 normal = 2.0 * texture2D (bumpTex, gl_TexCoord[0].st).rgb - 1.0;
	normal = normalize (normal);
	
	// compute bump lighting factor
	float lamberFactor = max (dot (lightVec, normal), 0.0);
	
	// apply bump lighting
	if (lamberFactor > 0.0) {   
		// outputColor +=	diffuseMaterial * diffuseStrength * lamberFactor;	
	}

	// compute specular lighting
    float shininess = pow (max (dot (halfVec, vertexNormal), 0.0), 2.0);
    shininess = 10.0;
	// outputColor += texture2D (specTex, gl_TexCoord[0].st) * specularStrength * shininess;

	gl_FragColor = outputColor;
}			

");
			GL.AttachShader(ProgramID,fragmentShader.ShaderID);


			
			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

			this.shaderPgm = new SSShaderProgram(ProgramID);
		}
	}
}


			
						
												
			
