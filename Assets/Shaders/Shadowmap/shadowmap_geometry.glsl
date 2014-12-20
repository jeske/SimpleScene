
#version 120
#extension GL_EXT_gpu_shader4 : enable
#extension GL_EXT_geometry_shader4 : enable

const int MAX_NUM_SHADOWMAPS = 4;

uniform int numShadowMaps;

uniform mat4 shadowMapVPs0;
uniform mat4 shadowMapVPs1;
uniform mat4 shadowMapVPs2;
uniform mat4 shadowMapVPs3;

varying in float splitOverlapMask_Float[3];

void main()
{
    int combinedMask = 0;
    for (int i = 0; i < 3; ++i) {
        combinedMask |= int(splitOverlapMask_Float[i]);
    }

    int submask;
    bool horizOverlap, vertOverlap;
    for (int m = 0; m < numShadowMaps; ++m) {
        submask = combinedMask >> (m * 4);
        horizOverlap = (submask & 0x3) == 0x3;
        vertOverlap = (submask & 0xC) == 0xC;
        if (horizOverlap && vertOverlap) {
            mat4 vp;
            if      (m == 0) { vp = shadowMapVPs0; }
            else if (m == 1) { vp = shadowMapVPs1; }
            else if (m == 2) { vp = shadowMapVPs2; }
            else             { vp = shadowMapVPs3; }
            
            gl_PrimitiveID = m;
            for (int i = 0; i < 3; ++i) {
                gl_Position = vp * gl_PositionIn[i];
                EmitVertex();
            }
			EndPrimitive();
        }        
    }
}
