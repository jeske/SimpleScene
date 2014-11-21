// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120

varying f_shadowMapIndex;

const vec2 boundaries[] = {
    vec2(-1.0, -1.0),
    vec2(0.0, -1.0),
    vec2(-1.0, 0.0),
    vec2(0.0, 0.0),
};

void main()
{
    vec2 bmin = boundaries[f_shadowMapIndex];
    vec2 bmax = bmin + vec2(1.0);
    if (gl_FragCoord.x < bmin.x || glFragCoord.x >= bmax.x
     || gl_FragCoord.y < bmin.y || glFragCoord.y >= bmax.y) {
        discard;
    }
    gl_FragColor = gl_FragCoord.z;
}
