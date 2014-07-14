// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK.Graphics.OpenGL;


// shader doc references..
//
// http://fabiensanglard.net/bumpMapping/index.php
// http://www.geeks3d.com/20091013/shader-library-phong-shader-with-multiple-lights-glsl/
// http://en.wikibooks.org/wiki/GLSL_Programming/GLUT/Specular_Highlights


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
				
	// in eye-space/camera space
	varying vec3 vertexNormal;
	varying vec3 n;  // vertex normal
	varying vec3 VV; // vertex position
	varying vec3 lightPosition;
	varying vec3 eyeVec;

    varying vec3 vertexPosition_objectspace;

void main()
{
	gl_TexCoord[0] =  gl_MultiTexCoord0;  // output base UV coordinates

    vertexPosition_objectspace = gl_Vertex.xyz;

	// transform into eye-space
	vertexNormal = n = normalize (gl_NormalMatrix * gl_Normal);
	vec4 vertexPosition = gl_ModelViewMatrix * gl_Vertex;
	VV = vec3(vertexPosition);
	lightPosition = (gl_LightSource[0].position - vertexPosition).xyz;
	eyeVec = -normalize(vertexPosition).xyz;

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

uniform int showWireframes;
uniform float animateSecondsOffset;

// eye-space/cameraspace coordinates
varying vec3 f_VV;
varying vec3 f_vertexNormal;
varying vec3 f_lightPosition;
varying vec3 f_eyeVec;
varying vec3 f_vertexPosition_objectspace;

// tangent space vectors for bump mapping
varying vec3 surfaceLightVector;   
varying vec3 surfaceViewVector;
varying vec3 surfaceNormalVector;

// http://www.clockworkcoders.com/oglsl/tutorial5.htm

float rand(vec2 co){
    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
}


    // http://www.ozone3d.net/tutorials/bump_mapping_p4.php

vec4 linearTest(vec4 outputColor) {
       vec4 effectColor = vec4(0.9);

       float proximity = mod(f_vertexPosition_objectspace.z + (animateSecondsOffset / 3.5), 1);
       if (proximity < 0.2) {
         outputColor = mix(effectColor,outputColor,clamp(proximity * 7.0,0,1));
       }
	return outputColor;
}

vec4 spiralTest(vec4 outputColor) {
  float time = animateSecondsOffset;
  vec2 resolution = vec2(2,2);
  vec2 aspect = vec2(2,2);
  vec4 effectColor = vec4(0.2);
  vec2 currentLocation = f_vertexPosition_objectspace.xy;
   
  vec2 position =  currentLocation / resolution.xy * aspect.xy;
  float angle = 0.0 ;
  float radius = length(position) ;
  if (position.x != 0.0 && position.y != 0.0){
    angle = degrees(atan(position.y,position.x)) ;
  }
  float amod = mod(angle+30.0*time-120.0*log(radius), 30.0) ;
  if (amod<15.0){
    outputColor += effectColor * clamp(log(radius), 0.0, 1.0) * clamp (amod / 30.0, 0, 1);
  } 
  return outputColor;
}



void main()
{
	vec4 outputColor = vec4(0.0);

	// lighting strength
	vec4 ambientStrength = gl_FrontMaterial.ambient;
	vec4 diffuseStrength = gl_FrontMaterial.diffuse;
	vec4 specularStrength = gl_FrontMaterial.specular;
	// specularStrength = vec4(0.7,0.4,0.4,0.0);  // test red
	vec3 lightPosition = surfaceLightVector;

	// load texels...
	vec4 ambientColor = texture2D (diffTex, gl_TexCoord[0].st);
	vec4 diffuseColor = texture2D (diffTex, gl_TexCoord[0].st);
	vec4 glowColor = texture2D (ambiTex, gl_TexCoord[0].st);
	vec4 specTex = texture2D (specTex, gl_TexCoord[0].st);

	if (true) {
	   // eye space shading
	   outputColor = ambientColor * ambientStrength;
	   outputColor += glowColor * gl_FrontMaterial.emission;

	   float diffuseIllumination = clamp(dot(f_vertexNormal, f_lightPosition), 0, 1);
	   // boost the diffuse color by the glowmap .. poor mans bloom
	   float glowFactor = length(gl_FrontMaterial.emission.xyz) * 0.2;
	   outputColor += diffuseColor * max(diffuseIllumination, glowFactor);

	   // compute specular lighting
	   if (dot(f_vertexNormal, f_lightPosition) > 0.0) {   // if light is front of the surface
	  
	      vec3 R = reflect(-normalize(f_lightPosition), normalize(f_vertexNormal));
	      float shininess = pow (max (dot(R, normalize(f_eyeVec)), 0.0), gl_FrontMaterial.shininess);

	      // outputColor += specularStrength * shininess;
	      outputColor += specTex * specularStrength * shininess;      
       } 


	} else {  // tangent space shading (with bump) 
       // lookup normal from normal map, move from [0,1] to  [-1, 1] range, normalize
       vec3 bump_normal = normalize( texture2D (bumpTex, gl_TexCoord[0].st).rgb * 2.0 - 1.0);
	   float distSqr = dot(surfaceLightVector,surfaceLightVector);
	   vec3 lVec = surfaceLightVector * inversesqrt(distSqr);

       // ambient ...
	   outputColor = ambientColor * ambientStrength;
	   outputColor += glowColor * gl_FrontMaterial.emission;
	          
       // diffuse...       
       float diffuseIllumination = clamp(dot(bump_normal,surfaceLightVector), 0,1);
       float glowFactor = length(gl_FrontMaterial.emission.xyz) * 0.2;
       outputColor += diffuseColor * max(diffuseIllumination, glowFactor);

	   if (dot(bump_normal, surfaceLightVector) > 0.0) {   // if light is front of the surface

          // specular...
          vec3 R = reflect(-lVec,bump_normal);
          float shininess = pow (clamp (dot(R, normalize(surfaceViewVector)), 0,1), gl_FrontMaterial.shininess);
          outputColor += specTex * specularStrength * shininess;      
       }

    }

    // ---- object space shader effect tests ----
    // outputColor = linearTest(outputColor);
    // outputColor = spiralTest(outputColor);

	// single-pass wireframe calculation
	// .. compute distance from fragment to closest edge
	if (showWireframes == 1) { 
		float nearD = min(min(f_dist[0],f_dist[1]),f_dist[2]);
		float edgeIntensity = exp2(-1.0*nearD*nearD * 2);		
        vec4 edgeColor = vec4( (length(outputColor.rgb) < 0.5) ? 0.6 : 0.3 );
		// vec4 edgeColor = vec4( clamp( (1.7 - length(outputColor.rgb) ),0.3,0.7) );			
        outputColor = mix(edgeColor,outputColor,1.0-edgeIntensity);
    }


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
varying in vec3 VV[3];
varying in vec3 vertexNormal[3];
varying in vec3 lightPosition[3];
varying in vec3 eyeVec[3];
varying in vec3 vertexPosition_objectspace[3];

// non-uniform blocks are not supported until GLSL 330?
varying out vec3 f_VV;
varying out vec3 f_vertexNormal;
varying out vec3 f_lightPosition;
varying out vec3 f_eyeVec;
varying out vec3 f_vertexPosition_objectspace;

varying out vec3 surfaceLightVector;
varying out vec3 surfaceViewVector;
varying out vec3 surfaceNormalVector;

noperspective varying out vec3 f_dist;


// http://www.slideshare.net/Mark_Kilgard/geometryshaderbasedbumpmappingsetup
// http://www.terathon.com/code/tangent.html

void main(void)
{
    // compute tangent
    //vec3 dXYZdU   = vec3(gl_PositionIn[1] - gl_PositionIn[0]);
    //float  dSdU   = gl_TexCoordIn[1][0].s - gl_TexCoordIn[0][0].s;
    //vec3 dXYZdV   = vec3(gl_PositionIn[2] - gl_PositionIn[0]);
    //float  dSdV   = gl_TexCoordIn[2][0].s - gl_TexCoordIn[0][0].s;
    //vec3 tangent  = normalize(dSdV * dXYZdU - dSdU * dXYZdV);

    vec3 dXYZp1 = vec3(gl_PositionIn[1] - gl_PositionIn[0]);
    vec3 dXYZp2 = vec3(gl_PositionIn[2] - gl_PositionIn[0]);
    vec2 dUVp1 = vec2(gl_TexCoordIn[1][0] - gl_TexCoordIn[0][0]);
    vec2 dUVp2 = vec2(gl_TexCoordIn[2][0] - gl_TexCoordIn[0][0]);
    float r = 1.0 / (dUVp1.s * dUVp2.t - dUVp2.s * dUVp1.t);
    vec3 tangent = normalize(r * ( (dUVp2.t * dXYZp1) - (dUVp1.t * dXYZp2) ));
    vec3 bitangent = normalize(r * ( (dUVp1.s * dXYZp2) - (dUVp2.s * dXYZp1) ));
    vec3 tsn = normalize(cross(tangent,bitangent));
    mat3 tangentSpaceMatrix = mat3( tangent, bitangent, tsn );

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

	    // bump tangent-space calculations..
        surfaceLightVector  = normalize(tangentSpaceMatrix * lightPosition[i]);
        surfaceViewVector   = normalize(tangentSpaceMatrix * eyeVec[i]);
        surfaceNormalVector = normalize(tangentSpaceMatrix * vertexNormal[i]);

        // single-pass wireframe information
		f_dist = vertexEdgeDistance[i];
        
        // pass through data
		f_VV = VV[i];
		f_vertexNormal = vertexNormal[i];
		f_lightPosition = lightPosition[i];
		f_eyeVec = eyeVec[i];
        f_vertexPosition_objectspace = vertexPosition_objectspace[i];
		       
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

			GL.UseProgram (ProgramID);
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "showWireframes"), (int)1);			
			GL.Uniform1 (GL.GetUniformLocation (ProgramID, "animateSecondsOffset"), (float)0.0f);			

			this.shaderPgm = new SSShaderProgram(ProgramID);

			{
				ErrorCode glerr;
				if ((glerr = GL.GetError ()) != ErrorCode.NoError) {
					throw new Exception (String.Format ("GL Error: {0}", glerr));
				}
			}
		}

	}
}


			
						
												
			
