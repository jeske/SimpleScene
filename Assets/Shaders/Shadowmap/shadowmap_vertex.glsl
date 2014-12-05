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

varying int indexMask;

vec2 boundaries[4] = vec2[](
    vec2(-1f, -1f),
    vec2(0f, -1f),
    vec2(-1f, 0f),
    vec2(0f, 0f)
);

void main()
{
    /*
    float viewZ = -(gl_ModelViewMatrix * gl_Vertex).z;
    for (int i = 0; i < numShadowMaps; ++i) {
        if (viewZ < shadowMapSplits[i]) {
            indexMask = 1 << i;
            break;
        }
    }
    */
    
    gl_Position = objWorldTransform * gl_Vertex;

    indexMask = 0;
    for (int i = 0; i < numShadowMaps; ++i) {
        vec2 bmin = boundaries[i];
        vec2 bmax = bmin + vec2(1f, 1f);
        
        mat4 vp;
        switch(i) {
        case 0: vp = shadowMapVPs0; break;
        case 1: vp = shadowMapVPs1; break;
        case 2: vp = shadowMapVPs2; break;
        default: vp = shadowMapVPs3; break;
        }
        
        vec4 test = vp * gl_Position;
        if (test.x >= bmin.x && test.x <= bmax.x
         && test.y >= bmin.y && test.y <= bmax.y) {
            indexMask |= 1 << i;
        }
    }
}    
