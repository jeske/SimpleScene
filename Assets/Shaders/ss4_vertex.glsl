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
uniform bool instanceBillboardingEnabled;

attribute vec3 instancePos;
attribute float instanceMasterScale;
attribute vec3 instanceComponentScale;
attribute vec4 instanceColor;

attribute float instanceSpriteIndex;
attribute float instanceSpriteOffsetU;
attribute float instanceSpriteOffsetV;
attribute float instanceSpriteSizeU;
attribute float instanceSpriteSizeV;

// todo: uniform sprite rect presets

// in eye-space/camera space
varying vec3 vertexNormal;
varying vec3 n;  // vertex normal
varying vec3 VV; // vertex position
varying vec3 lightPosition;
varying vec3 eyeVec;

varying vec3 vertexPosition_objectspace;
varying vec4 shadowMapCoords[MAX_NUM_SHADOWMAPS];
varying vec4 varInstanceColor;

// returns a quaternion representing rotation
// http://github.com/opentk/opentk/blob/c29509838d340bd292bc0113fe65a2e4b5aed0e8/Source/OpenTK/Math/Matrix4.cs
vec4 extractRotationQuat(mat4 input, bool row_normalise)
{
    vec3 row0 = input[0].xyz;
    vec3 row1 = input[1].xyz;
    vec3 row2 = input[2].xyz;
    if (row_normalise) {
        row0 = normalize(row0);
        row1 = normalize(row1);
        row2 = normalize(row2);
    }
    // code below adapted from Blender
    vec4 q;
    float trace = 0.25 * (row0[0] + row1[1] + row2[2] + 1.0);
    if (trace > 0) {
        float sq = sqrt(trace);
        q.w = sq;
        sq = 1.0 / (4.0 * sq);
        q.x = (row1[2] - row2[1]) * sq;
        q.y = (row2[0] - row0[2]) * sq;
        q.z = (row0[1] - row1[0]) * sq;
    } else if (row0[0] > row1[1] && row0[0] > row2[2]) {
        float sq = 2.0 * sqrt(1.0 + row0[0] - row1[1] - row2[2]);
        q.x = 0.25 * sq;
        sq = 1.0 / sq;
        q.w = (row2[1] - row1[2]) * sq;
        q.y = (row1[0] + row0[1]) * sq;
        q.z = (row2[0] + row0[2]) * sq;
    } else if (row1[1] > row2[2]) {
        float sq = 2.0 * sqrt(1.0 + row1[1] - row0[0] - row2[2]);
        q.y = 0.25 * sq;
        sq = 1.0 / sq;
        q.w = (row2[0] - row0[2]) * sq;
        q.x = (row1[0] + row0[1]) * sq;
        q.z = (row2[1] + row1[2]) * sq;
    } else {
        float sq = 2.0 * sqrt(1.0 + row2[2] - row0[0] - row1[1]);
        q.z = 0.25 * sq;
        sq = 1.0 / sq;
        q.w = (row1[0] - row0[1]) * sq;
        q.x = (row2[0] + row0[2]) * sq;
        q.y = (row2[1] + row1[2]) * sq;
    }
    q = normalize(q);
    return q;
}

// http://www.opengl.org/discussion_boards/showthread.php/160134-Quaternion-functions-for-GLSL
vec3 quatTransform(vec4 q, vec3 v)
{
    return v + 2.0*cross(cross(v, q.xyz ) + q.w*v, q.xyz);
}

void main()
{   
    vertexPosition_objectspace = gl_Vertex.xyz;

	// transform into eye-space
	vertexNormal = n = normalize (gl_NormalMatrix * gl_Normal);
	vec4 vertexPosition_viewspace = gl_ModelViewMatrix * gl_Vertex;
	VV = vec3(vertexPosition_viewspace);
	lightPosition = (gl_LightSource[0].position - vertexPosition_viewspace).xyz;
	eyeVec = -normalize(vertexPosition_viewspace).xyz;

    if (instanceDrawEnabled) {
        // TODO fix scale
        // TODO orientation, texture offset
        varInstanceColor = instanceColor;
        
        vec3 combinedPos = instanceComponentScale * gl_Vertex.xyz * vec3(instanceMasterScale);
        if (instanceBillboardingEnabled) {
            // this could have been done on the CPU side but only if we don't want
            // per-instance scale
            vec4 rotation = extractRotationQuat(gl_ModelViewMatrix, false);
            rotation *= -1; // inverse rotation
            combinedPos = quatTransform(rotation, combinedPos);
        }
        combinedPos += instancePos;
        //vec3 combinedPos = instancePos + gl.Vertex.xyz;
        gl_Position = gl_ModelViewProjectionMatrix * vec4(combinedPos, 1.0);
        gl_TexCoord[0].xy = gl_MultiTexCoord0.xy
                          * vec2(instanceSpriteSizeU, instanceSpriteSizeV);
        gl_TexCoord[0].xy += vec2(instanceSpriteOffsetU, instanceSpriteOffsetV);
        gl_TexCoord[0].zw = gl_MultiTexCoord0.zw;
        
    } else {
        gl_Position = ftransform();
        gl_TexCoord[0] = gl_MultiTexCoord0;  // output base UV coordinates
    }
    
    // shadowmap transform
    vec4 objPos = objWorldTransform * vec4(gl_Vertex.xyz, 1.0);
    for (int i = 0; i < numShadowMaps; ++i) {
        mat4 vp;

		if      (i == 0) { vp = shadowMapVPs0; }
        else if (i == 1) { vp = shadowMapVPs1; }
        else if (i == 2) { vp = shadowMapVPs2; }
        else             { vp = shadowMapVPs3; }

        shadowMapCoords[i] = vp * objPos;
    }
}	
