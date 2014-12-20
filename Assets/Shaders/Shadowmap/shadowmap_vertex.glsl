// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120
#extension GL_EXT_gpu_shader4 : enable

const int MAX_NUM_SHADOWMAPS = 4;

// input
uniform int numShadowMaps;
uniform vec4 shadowMapSplits;
uniform mat4 objWorldTransform;

uniform mat4 shadowMapVPs0;
uniform mat4 shadowMapVPs1;
uniform mat4 shadowMapVPs2;
uniform mat4 shadowMapVPs3;

varying float splitOverlapMask_Float;

vec2 boundaries[4] = vec2[](
    vec2(-1.0f, -1.0f),
    vec2( 0.0f, -1.0f),
    vec2(-1.0f,  0.0f),
    vec2( 0.0f,  0.0f)
);

void main()
{
    gl_Position = objWorldTransform * gl_Vertex;	

    int splitOverlapMask = 0;
    int submask;
    for (int i = 0; i < numShadowMaps; ++i) {
        vec2 bmin = boundaries[i];
        vec2 bmax = bmin + vec2(1.0f, 1.0f);
        
        mat4 vp;
        if      (i == 0) { vp = shadowMapVPs0; }
        else if (i == 1) { vp = shadowMapVPs1; }
        else if (i == 2) { vp = shadowMapVPs2; }
        else             { vp = shadowMapVPs3; }
        
        vec4 test = vp * gl_Position;

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
