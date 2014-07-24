// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

#version 120
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
		float edgeWidth = 1.5; // in screenspace pixels
		float nearD = min(min(f_dist[0],f_dist[1]),f_dist[2]);
        float curIntensity = max(max(outputColor.r,outputColor.g),outputColor.b);
		float edgeIntensity = exp2((-1 / edgeWidth)*nearD*nearD * 2);		
        vec4 edgeColor = vec4(vec3( (curIntensity < 0.4) ? 0.6 : 0.3 ),1.0);
		// vec4 edgeColor = vec4( clamp( (1.7 - length(outputColor.rgb) ),0.3,0.7) );			
        outputColor = mix(edgeColor,outputColor,1.0-edgeIntensity);
    }


	// finally, output the fragment color
    gl_FragColor = outputColor;    
}			

	