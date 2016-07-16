#version 120

varying vec3 varViewRay;
varying vec3 varCylinderCenter;
varying vec3 varCylinderAxis;
varying float varCylinderLength;
varying float varCylinderWidth;
#ifdef INSTANCE_DRAW
varying vec4 varCylinderColor;
#endif

void main()
{
#ifdef INSTANCE_DRAW
    gl_FragColor = varCylinderColor;
#else
    gl_FragColor = vec4(1, 1, 1, 1);
#endif
}
