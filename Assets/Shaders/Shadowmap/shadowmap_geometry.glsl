
#version 120
#extension GL_EXT_gpu_shader4 : enable
#extension GL_EXT_geometry_shader4 : enable

uniform mat4 shadowMapVPs[MAX_NUM_SHADOWMAPS];
uniform int numShadowMaps;

varying in int shadowMapIndexMask[3];

varying out int f_shadowMapIndex;

void main()
{
    int combinedMask = 0;
    for (int i = 0; i < 3; ++i) {
        combinedMask |= shadowMapIndexMask[i];
    }

    for (int m = 0; m < numShadowMaps; ++m) {
        if (combinedMask & (i << m)) {
            f_shadowMapIndex = m;
            for (int i = 0; i < 3; ++i) {
                gl_Position = shadowMapVPs[m] * glPositionIn[i];
                EmitVertex();
            }
        }
    }
    EndPrimitive(); // not necessary?
}
