// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120

flat int f_shadowMapIndex = 0;

vec2 boundaries[4] = vec2[](
    vec2(0f, 0f),
    vec2(1024f, 0f),
    vec2(0f, 1024f),
    vec2(1024f, 1024f)
);

void main()
{
    //vec2 bmin = boundaries[f_shadowMapIndex];
    vec2 bmin = boundaries[0];
    vec2 bmax = bmin + vec2(1024f);
    if (gl_FragCoord.x < bmin.x || gl_FragCoord.x >= bmax.x
     || gl_FragCoord.y < bmin.y || gl_FragCoord.y >= bmax.y) {
        discard;
    }
    gl_FragColor = vec4(1);
}
