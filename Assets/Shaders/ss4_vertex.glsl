// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120

// shadow-map related
const int MAX_NUM_SHADOWMAPS = 4;
uniform int numShadowMaps;
uniform mat4 shadowMapVPs[MAX_NUM_SHADOWMAPS];
uniform mat4 objWorldTransform;
varying vec4 shadowMapCoords[MAX_NUM_SHADOWMAPS];

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

    // shadowmap transform
    vec4 objPos = objWorldTransform * vec4(gl_Vertex.xyz, 1);
    for (int i = 0; i < numShadowMaps; ++i) {
        shadowMapCoords[i] = shadowMapVPs[i] * objPos;
    }
}	
