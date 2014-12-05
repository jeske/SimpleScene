
#version 120
#extension GL_EXT_gpu_shader4 : enable
#extension GL_EXT_geometry_shader4 : enable

const int MAX_NUM_SHADOWMAPS = 4;

uniform int numShadowMaps;

uniform mat4 shadowMapVPs0;
uniform mat4 shadowMapVPs1;
uniform mat4 shadowMapVPs2;
uniform mat4 shadowMapVPs3;

varying in int indexMask[3];

void main()
{
    int combinedMask = 0;
    for (int i = 0; i < 3; ++i) {
        combinedMask |= indexMask[i];
    }

    for (int m = 0; m < numShadowMaps; ++m) {       
        if ((combinedMask & (1 << m)) != 0)  {
            mat4 vp;
            switch(m) {
            case 0: vp = shadowMapVPs0; break;
            case 1: vp = shadowMapVPs1; break;
            case 2: vp = shadowMapVPs2; break;
            default: vp = shadowMapVPs3; break;
            }
            
            gl_PrimitiveID = m;
            for (int i = 0; i < 3; ++i) {
                gl_Position = vp * gl_PositionIn[i];
                EmitVertex();
            }
        }
        EndPrimitive();
    }
}
