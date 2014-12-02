// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120

flat int f_shadowMapIndex = 0;
const float SPLIT_WIDTH = 1024;
const float SPLIT_HEIGHT = 1024;

vec2 boundaries[4] = vec2[](
    vec2(0f, 0f),
    vec2(SPLIT_WIDTH, 0f),
    vec2(0f, SPLIT_HEIGHT),
    vec2(SPLIT_WIDTH, SPLIT_HEIGHT)
);

void main()
{
    vec2 bmin = boundaries[f_shadowMapIndex];
    vec2 bmax = bmin + vec2(SPLIT_WIDTH, SPLIT_HEIGHT);
    if (gl_FragCoord.x < bmin.x || gl_FragCoord.x >= bmax.x
     || gl_FragCoord.y < bmin.y || gl_FragCoord.y >= bmax.y) {
        discard;
    }
    gl_FragColor = vec4(1);
}
