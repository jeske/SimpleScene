// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120

// shadow-map related
const int MAX_NUM_SHADOWMAPS = 4;
uniform int numShadowMaps;
uniform mat4 shadowMapVPs0;
uniform mat4 shadowMapVPs1;
uniform mat4 shadowMapVPs2;
uniform mat4 shadowMapVPs3;
uniform mat4 objWorldTransform;

// instanced drawing
uniform bool instanceDrawEnabled;
attribute vec3 instancePos;
attribute vec4 instanceColor;

// in eye-space/camera space
varying vec3 vertexNormal;
varying vec3 n;  // vertex normal
varying vec3 VV; // vertex position
varying vec3 lightPosition;
varying vec3 eyeVec;

varying vec3 vertexPosition_objectspace;
varying vec4 shadowMapCoords[MAX_NUM_SHADOWMAPS];
varying vec4 varInstanceColor;

void main()
{   
	gl_TexCoord[0] =  gl_MultiTexCoord0;  // output base UV coordinates

    vertexPosition_objectspace = gl_Vertex.xyz;

	// transform into eye-space
	vertexNormal = n = normalize (gl_NormalMatrix * gl_Normal);
	vec4 vertexPosition_viewspace = gl_ModelViewMatrix * gl_Vertex;
	VV = vec3(vertexPosition_viewspace);
	lightPosition = (gl_LightSource[0].position - vertexPosition_viewspace).xyz;
	eyeVec = -normalize(vertexPosition_viewspace).xyz;

    if (instanceDrawEnabled) {
        // TODO more instance attributes
        varInstanceColor = vec4(instanceColor[0], instanceColor[1], instanceColor[2], instanceColor[3]);
        gl_Position = gl_ModelViewProjectionMatrix * (gl_Vertex + vec4(instancePos, 0f));
    } else {
        gl_Position = ftransform();
    }
    
    // shadowmap transform
    vec4 objPos = objWorldTransform * vec4(gl_Vertex.xyz, 1);
    for (int i = 0; i < numShadowMaps; ++i) {
        mat4 vp;

		if      (i == 0) { vp = shadowMapVPs0; }
        else if (i == 1) { vp = shadowMapVPs1; }
        else if (i == 2) { vp = shadowMapVPs2; }
        else             { vp = shadowMapVPs3; }

        shadowMapCoords[i] = vp * objPos;
    }
}	
