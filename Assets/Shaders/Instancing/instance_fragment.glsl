#version 120
#extension GL_EXT_gpu_shader4 : enable

varying vec4 varInstanceColor;

uniform samplerBuffer tboSampler;
uniform int textureWidth;

vec4 texelFetch(ivec2 coords)
{
    vec4 pixel = texelFetchBuffer(tboSampler, int((coords.x * textureWidth)+coords.y));
    return pixel;
}

void main()
{
    gl_FragColor = vec4(0);
    //gl_FragColor += texture2D(primaryTexture, gl_TexCoord[0].st);
    gl_FragColor = texelFetch(ivec2(gl_TexCoord[0].st));
    gl_FragColor *= varInstanceColor;
}
