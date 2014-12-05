// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120
#extension GL_EXT_gpu_shader4 : enable

const int MAX_NUM_SHADOWMAPS = 4;

// input
uniform int numShadowMaps;
uniform vec4 shadowMapSplits;
uniform mat4 objWorldTransform;

varying int indexMask;

void main()
{
    float viewZ = -(gl_ModelViewMatrix * gl_Vertex).z;
    for (int i = 0; i < numShadowMaps; ++i) {
        if (viewZ < shadowMapSplits[i]) {
            indexMask = 1 << i;
            break;
        }
    }
    
    gl_Position = objWorldTransform * gl_Vertex;
}    
