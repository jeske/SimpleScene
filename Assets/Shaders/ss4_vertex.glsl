// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

// shadow-map related
const int MAX_NUM_SMAP_SPLITS = 4;
uniform int numShadowMaps;
uniform mat4 shadowMapVPs[MAX_NUM_SMAP_SPLITS];
uniform mat4 objWorldTransform;

// instanced drawing
// #define INSTANCE_DRAW 

#ifdef INSTANCE_DRAW
attribute vec3 instancePos;
attribute vec2 instanceOrientationXY;
attribute float instanceOrientationZ;
attribute float instanceMasterScale;
attribute vec2 instanceComponentScaleXY;
attribute float instanceComponentScaleZ;
attribute vec4 instanceColor;

//attribute float instanceSpriteIndex;
attribute float instanceSpriteOffsetU;
attribute float instanceSpriteOffsetV;
attribute float instanceSpriteSizeU;
attribute float instanceSpriteSizeV;

attribute vec2 attrTexCoord;
attribute vec3 attrNormal;
varying vec4 varInstanceColor;
#else
uniform vec4 spriteOffsetAndSize;
#endif

// todo: uniform sprite rect presets

// in eye-space/camera space
varying vec3 vertexNormal;
varying vec3 n;  // vertex normal
varying vec3 VV; // vertex position
varying vec3 lightPosition;
varying vec3 eyeVec;

varying vec3 vertexPosition_objectspace;
varying vec4 shadowMapCoords[MAX_NUM_SMAP_SPLITS];

// returns a quaternion representing rotation
// http://github.com/opentk/opentk/blob/c29509838d340bd292bc0113fe65a2e4b5aed0e8/Source/OpenTK/Math/Matrix4.cs
vec4 extractRotationQuat(mat4 src, bool row_normalise)
{
    vec3 row0 = src[0].xyz;
    vec3 row1 = src[1].xyz;
    vec3 row2 = src[2].xyz;
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

mat3 orientX(float angle)
{
    float cosine = cos(angle);
    float sine = sin(angle);

    return mat3(1.0, 0.0, 0.0,
                0.0, cosine, sine,
                0.0, -sine, cosine);
}

mat3 orientY(float angle)
{
    float cosine = cos(angle);
    float sine = sin(angle);

    return mat3(cosine, 0.0, -sine,
                0.0, 1.0, 0.0,
                sine, 0.0, cosine);
}

mat3 orientZ(float angle)
{
    float cosine = cos(angle);
    float sine = sin(angle);
    return mat3(cosine, sine, 0.0,
                -sine, cosine, 0.0,
                0.0, 0.0, 1.0);
}

bool isNaN(float value)
{
    return value != value;
}

void main()
{   
	// transform into eye-space
    vec3 combinedPos = gl_Vertex.xyz;

    #ifdef INSTANCE_DRAW
    vec3 combinedNormal = attrNormal;
    vec3 instanceComponentScale = vec3(instanceComponentScaleXY, instanceComponentScaleZ);

    combinedPos = instanceComponentScale * combinedPos * vec3(instanceMasterScale);
    if (isNaN(instanceOrientationXY.x) || isNaN(instanceOrientationXY.y)) { // billboardXY?
        vec4 rotation = extractRotationQuat(gl_ModelViewMatrix, false);
        rotation *= -1; // inverse rotation

        combinedPos = orientZ(instanceOrientationZ) * combinedPos;
        combinedPos = quatTransform(rotation, combinedPos);

        combinedNormal = orientZ(instanceOrientationZ) * combinedNormal;
        combinedNormal = quatTransform(rotation, combinedNormal);
    } else {
        combinedPos = orientX(instanceOrientationXY.x) * combinedPos;
        combinedPos = orientY(instanceOrientationXY.y) * combinedPos;
        combinedPos = orientZ(instanceOrientationZ) * combinedPos;

        combinedNormal = orientX(instanceOrientationXY.x) * combinedNormal;
        combinedNormal = orientY(instanceOrientationXY.y) * combinedNormal;
        combinedNormal = orientZ(instanceOrientationZ) * combinedNormal;
    }
    combinedPos += instancePos;
    gl_TexCoord[0].xy = attrTexCoord * vec2(instanceSpriteSizeU, instanceSpriteSizeV);
    gl_TexCoord[0].xy += vec2(instanceSpriteOffsetU, instanceSpriteOffsetV);
    gl_TexCoord[0].zw = vec2(0);
    varInstanceColor = instanceColor;
    //varInstanceColor = new vec4(0f, 1f, 0f, 1f);
    //gl_ModelViewMatrix *= -1;
    #else
    vec3 combinedNormal = gl_Normal;
    gl_TexCoord[0].xy = spriteOffsetAndSize.xy 
        + gl_MultiTexCoord0.xy * spriteOffsetAndSize.zw;
    #endif

    vertexNormal = n = normalize (gl_NormalMatrix * combinedNormal);
    vertexPosition_objectspace = combinedPos;
	vec4 vertexPosition_viewspace = gl_ModelViewMatrix * vec4(combinedPos, 1);
	VV = vec3(vertexPosition_viewspace);
	lightPosition = (gl_LightSource[0].position - vertexPosition_viewspace).xyz;
	eyeVec = -normalize(vertexPosition_viewspace).xyz;
    gl_Position = gl_ModelViewProjectionMatrix * vec4(combinedPos, 1.0);
    
    // shadowmap transform
    vec4 objPos = objWorldTransform * vec4(combinedPos, 1.0);
    for (int i = 0; i < numShadowMaps; ++i) {
        shadowMapCoords[i] = shadowMapVPs[i] * objPos;
    }
}	
