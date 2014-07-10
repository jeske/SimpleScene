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
		SSShader geometryShader;

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
#extension GL_EXT_gpu_shader4 : enable

// edge distance from geometry shader..
noperspective varying vec3 f_dist;

uniform sampler2D diffTex;
uniform sampler2D specTex;
uniform sampler2D ambiTex;
uniform sampler2D bumpTex;
		
// New bumpmapping
varying vec3 f_lightVec;
varying vec3 f_halfVec;
varying vec3 f_eyeVec;

varying vec3 f_n;
varying vec3 f_VV;

varying vec3 f_vertexNormal;

void main()
{
	vec4 outputColor = vec4(0.0);

	// lighting strength
	vec4 ambientStrength = gl_FrontMaterial.ambient;
	vec4 diffuseStrength = gl_FrontMaterial.diffuse;
	vec4 specularStrength = gl_FrontMaterial.specular;
	vec3 lightPosition = normalize(gl_LightSource[0].position.xyz - f_VV);

	// compute the ambient color
	vec4 ambientColor = texture2D (ambiTex, gl_TexCoord[0].st);
	float glowFactor = length(ambientColor.rgb) * 0.5;

	// diffuse color baseline
	// http://www.clockworkcoders.com/oglsl/tutorial5.htm
	vec4 diffuseColor = texture2D (diffTex, gl_TexCoord[0].st);
	outputColor += diffuseColor * 
	                 (max(dot(f_n, lightPosition), 0.0) +
	                 glowFactor);  // the glow should light the diffuse color
	
	// mix in the ambient glow map
	
	outputColor = mix(outputColor, ambientColor, glowFactor);
	
	// lookup normal from normal map, move from [0,1] to  [-1, 1] range, normalize
	vec3 normal = 2.0 * texture2D (bumpTex, gl_TexCoord[0].st).rgb - 1.0;
	normal = normalize (normal);
	
	// compute bump lighting factor
	float lamberFactor = max (dot (f_lightVec, normal), 0.0);
	
	// apply bump lighting
	if (lamberFactor > 0.0) {   
		// outputColor +=	diffuseMaterial * diffuseStrength * lamberFactor;	
	}

	// compute specular lighting
    float shininess = pow (max (dot (f_halfVec, f_vertexNormal), 0.0), 2.0);
    shininess = 10.0;
	outputColor += texture2D (specTex, gl_TexCoord[0].st) * specularStrength * shininess;

	// single-pass wireframe calculation
	// .. compute distance from fragment to closest edge
	float nearD = min(min(f_dist[0],f_dist[1]),f_dist[2]);
	float edgeIntensity = exp2(-1.0*nearD*nearD * 2);
	vec4 edgeColor = vec4( clamp( (1.1-length(outputColor)) / 2,0.1,0.4) );	
    outputColor = mix(edgeColor,outputColor,1.0-edgeIntensity);


	// finally, output the fragment color
    gl_FragColor = outputColor;    
}			

");
			GL.AttachShader(ProgramID,fragmentShader.ShaderID);

			// http://www.lighthouse3d.com/tutorials/glsl-core-tutorial/geometry-shader/
			// http://www.opengl.org/wiki/Geometry_Shader_Examples



			// TODO : make this single-pass wireframe GLSL shader work..
			// http://strattonbrazil.blogspot.com/2011/09/single-pass-wireframe-rendering_10.html
			this.geometryShader = new SSShader (
				ShaderType.GeometryShader, "bumpGeometry",
@"#version 120
#extension GL_EXT_gpu_shader4 : enable
#extension GL_EXT_geometry_shader4 : enable


// not supported until GLSL 150
// ... see GL.Ext.ProgramParameter(..) call
// layout(triangles) in;
// layout (triangles, max_vertices=3) out;

// these are the sigle-pass wireframe variables
uniform vec2 WIN_SCALE;
noperspective varying vec3 dist;

// these are pass-through variables
varying in vec3 lightVec[3];
varying in vec3 halfVec[3];
varying in vec3 eyeVec[3];
varying in vec3 n[3];
varying in vec3 VV[3];
varying in vec3 vertexNormal[3];

// non-uniform blocks are not supported until GLSL 330?
varying out vec3 f_lightVec;
varying out vec3 f_halfVec;
varying out vec3 f_eyeVec;
varying out vec3 f_n;
varying out vec3 f_VV;
varying out vec3 f_vertexNormal;
noperspective varying out vec3 f_dist;

void main(void)
{

// taken from 'Single-Pass Wireframe Rendering'
vec2 p0 = WIN_SCALE * gl_PositionIn[0].xy/gl_PositionIn[0].w;
vec2 p1 = WIN_SCALE * gl_PositionIn[1].xy/gl_PositionIn[1].w;
vec2 p2 = WIN_SCALE * gl_PositionIn[2].xy/gl_PositionIn[2].w;
vec2 v0 = p2-p1;
vec2 v1 = p2-p0;
vec2 v2 = p1-p0;
float area = abs(v1.x*v2.y - v1.y * v2.x);

vec3 vertexEdgeDistance[3];
vertexEdgeDistance[0] = vec3(area/length(v0),0,0);
vertexEdgeDistance[1] = vec3(0,area/length(v1),0);
vertexEdgeDistance[2] = vec3(0,0,area/length(v2));


  // suppress warning by assigning this to something to start...
  gl_TexCoord[0] = gl_TexCoordIn[0][0];

  // LOOP for each vertex in the primitive...
  // .. gl_verticiesIn holds the count
  for(int i = 0; i < 3; i++) {
     f_lightVec = lightVec[i];
     f_halfVec = halfVec[i];
     f_eyeVec = eyeVec[i];
     f_n = n[i];
     f_VV = VV[i];
     f_vertexNormal = vertexNormal[i];

     f_dist = vertexEdgeDistance[i];
               
     gl_TexCoord[0] = gl_TexCoordIn[i][0];
     gl_FrontColor = gl_FrontColorIn[i];
     gl_Position = gl_PositionIn[i];
     EmitVertex();
  }
  EndPrimitive(); // not necessary as we only handle triangles
}
");

			// https://wiki.engr.illinois.edu/display/graphics/Geometry+Shader+Hello+World
			
			GL.Ext.ProgramParameter(ProgramID,ExtGeometryShader4.GeometryInputTypeExt,(int)All.Triangles);
			GL.Ext.ProgramParameter(ProgramID,ExtGeometryShader4.GeometryOutputTypeExt,(int)All.TriangleStrip);
			GL.Ext.ProgramParameter(ProgramID,ExtGeometryShader4.GeometryVerticesOutExt,3);
			
			GL.AttachShader(ProgramID,geometryShader.ShaderID);
						
			GL.LinkProgram(ProgramID);
			Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

			this.shaderPgm = new SSShaderProgram(ProgramID);
		}
	}
}


			
						
												
			
