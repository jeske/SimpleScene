// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120
#extension GL_EXT_gpu_shader4 : enable

const float SPLIT_WIDTH = 1024;
const float SPLIT_HEIGHT = 1024;

vec2 boundaries[4] = vec2[](
    vec2(0.0f, 0.0f),
    vec2(SPLIT_WIDTH, 0.0f),
    vec2(0.0f, SPLIT_HEIGHT),
    vec2(SPLIT_WIDTH, SPLIT_HEIGHT)
);

void main()
{
    vec2 bmin = boundaries[gl_PrimitiveID];
    vec2 bmax = bmin + vec2(SPLIT_WIDTH, SPLIT_HEIGHT);
    if (gl_FragCoord.x < bmin.x || gl_FragCoord.x >= bmax.x
     || gl_FragCoord.y < bmin.y || gl_FragCoord.y >= bmax.y) {
        discard;
    }
    gl_FragColor = vec4(1);
}
