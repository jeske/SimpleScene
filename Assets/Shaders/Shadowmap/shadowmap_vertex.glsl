// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120
#extension GL_EXT_gpu_shader4 : enable

const int MAX_NUM_SHADOWMAPS = 4;

// input
uniform int numShadowMaps;
uniform float shadowMapSplits[MAX_NUM_SHADOWMAPS];
uniform mat4 objWorldTransform;

// output
flat int shadowMapIndexMask;

void main()
{
    vec4 v = vec4(gl_Vertex.xyz, 1);
    float viewZ = -(gl_ModelViewMatrix * v).z;
    for (int i = 0; i < numShadowMaps; ++i) {
        if (viewZ < shadowMapSplits[i]) {
            shadowMapIndexMask = 1 << i;
            break;
        }
    }
    gl_Position = objWorldTransform * v;
}    
