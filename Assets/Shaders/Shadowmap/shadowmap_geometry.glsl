
#version 120
#extension GL_EXT_gpu_shader4 : enable
#extension GL_EXT_geometry_shader4 : enable

const int MAX_NUM_SHADOWMAPS = 4;

uniform mat4 shadowMapVPs0;
uniform mat4 shadowMapVPs1;
uniform mat4 shadowMapVPs2;
uniform mat4 shadowMapVPs3;

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
        mat4 mvp;
        switch(m) {
        case 0: mvp = shadowMapVPs0; break;
        case 1: mvp = shadowMapVPs1; break;
        case 2: mvp = shadowMapVPs2; break;
        default: mvp = shadowMapVPs3; break;
        }
        
        //if ((combinedMask & (1 << m)) != 0) {
        //if (m == 0) {
        {
            f_shadowMapIndex = m;
            for (int i = 0; i < 3; ++i) {
                gl_Position = mvp * gl_PositionIn[i];
                EmitVertex();
            }
        }
    }
    EndPrimitive(); // not necessary?
}
