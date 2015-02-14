#version 120

varying vec4 varInstanceColor;

uniform sampler2D primaryTexture;

void main()
{
    gl_FragColor = vec4(0);
    gl_FragColor += texture2D(primaryTexture, gl_TexCoord[0].st);
    gl_FragColor *= varInstanceColor;
}
