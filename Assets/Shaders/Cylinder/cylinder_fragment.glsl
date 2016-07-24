#version 120

varying vec3 varViewRay;
varying vec3 varCylinderCenter;
varying vec3 varCylinderAxis;
varying float varCylinderLength;
varying float varCylinderWidth;
#ifdef INSTANCE_DRAW
varying vec4 varCylinderColor;
#endif

// https://community.eveonline.com/news/dev-blogs/re-inventing-the-trails/

void main()
{
    vec4 intesections[2];
    int intersectionCount = 0;
    
    vec3 fragmentWorldPos = (gl_ModelViewProjectionMatrixInverse * gl_FragCoord).xyz;
    float cylRadius = varCylinderWidth / 2;

    vec3 proj1 = fragmentWorldPos - dot(fragmentWorldPos, varCylinderAxis) * varCylinderAxis; // todo consoldate
    vec3 proj2 = varViewRay - varCylinderAxis - dot(varViewRay - varCylinderAxis, varCylinderAxis) * varCylinderAxis;
    float a = dot(proj1, proj1);
    float b = 2 * dot(proj1, proj2);
    float c = dot(proj2, proj2) - cylRadius * cylRadius;
    float D = b*b - 4*a*c;

    if (D > 0) {
#ifdef INSTANCE_DRAW
        gl_FragColor = varCylinderColor;
#else
        gl_FragColor = vec4(1, 1, 1, 1);
#endif
    } else {
        discard;
    }
    
}
