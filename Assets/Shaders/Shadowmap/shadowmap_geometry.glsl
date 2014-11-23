
#version 120
#extension GL_EXT_gpu_shader4 : enable
#extension GL_EXT_geometry_shader4 : enable

const int MAX_NUM_SHADOWMAPS = 4;

uniform mat4 shadowMapVPs[MAX_NUM_SHADOWMAPS];
uniform int numShadowMaps;

varying in int shadowMapIndexMask[3];

flat int f_shadowMapIndex;

void main()
{
    int combinedMask = 0;
    for (int i = 0; i < 3; ++i) {
        combinedMask |= shadowMapIndexMask[i];
    }

    for (int m = 0; m < numShadowMaps; ++m) {
        if ((combinedMask & (1 << m)) != 0) {
            f_shadowMapIndex = m;
            for (int i = 0; i < 3; ++i) {
                gl_Position = shadowMapVPs[m] * gl_PositionIn[i];
                EmitVertex();
            }
        }
    }
    EndPrimitive(); // not necessary?
}
