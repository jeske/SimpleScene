// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

uniform vec3 viewRay;

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
    vec3 intersections[2];
    int intersectionCount = 0;
    
    vec3 fragmentWorldPos = (gl_ModelViewProjectionMatrixInverse * gl_FragCoord).xyz;
    float cylRadius = varCylinderWidth / 2;

    vec3 proj1 = fragmentWorldPos - dot(fragmentWorldPos, varCylinderAxis) * varCylinderAxis; // todo consoldate
    vec3 proj2 = viewRay - varCylinderAxis - dot(varViewRay - varCylinderAxis, varCylinderAxis) * varCylinderAxis;
    float a = dot(proj1, proj1);
    float b = 2 * dot(proj1, proj2);
    float c = dot(proj2, proj2) - cylRadius * cylRadius;
    float D = b*b - 4*a*c;

    if (D > 0) {
        float Dsqrt = sqrt(D);
        float t1 = (-b - Dsqrt)/2/a;
        vec3 pt1 = fragmentWorldPos + t1 * varViewRay;
        if (abs(dot(pt1 - varCylinderCenter, varCylinderAxis)) < varCylinderLength) {
            intersections[intersectionCount] = pt1;
            intersectionCount++;
        }
        
        float t2 = (-b + Dsqrt)/2/a;
        vec3 pt2 = fragmentWorldPos + t2 * varViewRay;
        if (abs(dot(pt2 - varCylinderCenter, varCylinderAxis)) < varCylinderLength) {
            intersections[intersectionCount] = pt2;
            intersectionCount++;
        }
    }
    //if (intersectionCount == 2) {
    if (abs(dot(varCylinderAxis, varViewRay)) < 0.5) {
        
#ifdef INSTANCE_DRAW
        gl_FragColor = varCylinderColor;
#else
        gl_FragColor = vec4(1, 1, 1, 1);
#endif
    } else {
        gl_FragColor = vec4(varCylinderColor.r, varCylinderColor.g, varCylinderColor.b, 0.1);
    }
    
}
