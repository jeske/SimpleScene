// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120
#extension GL_EXT_gpu_shader4 : enable

const int MAX_NUM_SMAP_SPLITS = 4;

// input
uniform int numShadowMaps;
uniform vec4 shadowMapSplits;
uniform mat4 objWorldTransform;

uniform mat4 shadowMapVPs[MAX_NUM_SMAP_SPLITS];

varying float splitOverlapMask_Float;

const vec2 boundaries[4] = vec2[](
    vec2(-1.0f, -1.0f),
    vec2( 0.0f, -1.0f),
    vec2(-1.0f,  0.0f),
    vec2( 0.0f,  0.0f)
);

#ifdef INSTANCE_DRAW
attribute vec3 instancePos;
attribute vec2 instanceOrientationXY;
attribute float instanceOrientationZ;
attribute float instanceMasterScale;
attribute vec2 instanceComponentScaleXY;
attribute float instanceComponentScaleZ;

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
    return mat3(1.0, 0.0, 0.0,
                0, cos(angle), -sin(angle),
                0.0, sin(angle), cos(angle));
}

mat3 orientY(float angle)
{
    return mat3(cos(angle), 0.0, sin(angle),
                0.0, 1.0, 0.0,
                -sin(angle), 0.0, cos(angle));
}

mat3 orientZ(float angle)
{
    return mat3(cos(angle), -sin(angle), 0.0,
                sin(angle), cos(angle), 0.0,
                0.0, 0.0, 1.0);
}

bool isNaN(float value)
{
    return value != value;
}
#endif

void main()
{
    vec3 combinedPos = gl_Vertex.xyz;
    
#ifdef INSTANCE_DRAW
    vec3 instanceComponentScale = vec3(instanceComponentScaleXY, instanceComponentScaleZ);

    combinedPos = instanceComponentScale * combinedPos * vec3(instanceMasterScale);
    if (isNaN(instanceOrientationXY.x) || isNaN(instanceOrientationXY.y)) { // billboardXY?
        vec4 rotation = extractRotationQuat(gl_ModelViewMatrix, false);
        rotation *= -1; // inverse rotation

        combinedPos = orientZ(instanceOrientationZ) * combinedPos;
        combinedPos = quatTransform(rotation, combinedPos);
    }
    else {
        combinedPos = orientX(instanceOrientationXY.x) * combinedPos;
        combinedPos = orientY(instanceOrientationXY.y) * combinedPos;
        combinedPos = orientZ(instanceOrientationZ) * combinedPos;
    }
    combinedPos += instancePos;  
#endif   

    gl_Position = objWorldTransform * vec4(combinedPos, 1);

    int splitOverlapMask = 0;
    int submask;
    for (int i = 0; i < numShadowMaps; ++i) {
        vec2 bmin = boundaries[i];
        vec2 bmax = bmin + vec2(1.0f, 1.0f);      
        vec4 test = shadowMapVPs[i] * gl_Position;

        // figure out how the point relates to the split's rectangle
        if (test.x < bmin.x) {
            submask = 0x1; // to the left
        } else if (test.x > bmax.x) {
            submask = 0x2; // to the right
        } else {
            submask = 0x3; // implies horizontal overlap
        }

        if (test.y < bmin.y) {
            submask |= 0x4; // below
        } else if (test.y > bmax.y) {
            submask |= 0x8; // above
        } else {
            submask |= 0xC; // implies vertical overlap
        }
        splitOverlapMask |= (submask << (i * 4));
    }
	splitOverlapMask_Float = splitOverlapMask;
}    
