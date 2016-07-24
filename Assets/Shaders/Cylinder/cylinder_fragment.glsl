// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

uniform vec3 cameraPos;
uniform vec3 viewRay;
uniform vec3 viewX;
uniform vec3 viewY;
uniform float screenWidth;
uniform float screenHeight;

//varying vec3 varViewRay;
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

    float normalizedX = gl_FragCoord.x / screenWidth - 0.5;
    float normalizedY = gl_FragCoord.y / screenHeight - 0.5;
    //vec3 fragmentWorldPos = cameraPos + viewRay +  normalizedX * viewX + normalizedY * viewY;
    vec3 fragmentWorldPos = cameraPos + normalizedX * viewX + normalizedY * viewY;
    //vec3 pixelRay = fragmentWorldPos - cameraPos;
    vec3 pixelRay = viewRay;
    
    
    //vec3 fragmentWorldPos = (gl_ProjectionMatrixInverse * vec4(gl_FragCoord.xy, 0, 1)).xyz;
    //vec3 fragmentWorldPos = (gl_FragCoord / gl_FragCoord.w).xyz;
    float cylRadius = varCylinderWidth / 2;

    vec3 proj1 = fragmentWorldPos - dot(fragmentWorldPos, varCylinderAxis) * varCylinderAxis; // todo consoldate
    vec3 proj2 = pixelRay - varCylinderAxis - dot(pixelRay - varCylinderAxis, varCylinderAxis) * varCylinderAxis;
    float a = dot(proj1, proj1);
    float b = 2 * dot(proj1, proj2);
    float c = dot(proj2, proj2) - cylRadius * cylRadius;
    float D = b*b - 4*a*c;

    if (D > 0) {
        float Dsqrt = sqrt(D);
        float t1 = (-b - Dsqrt)/2/a;
        vec3 pt1 = fragmentWorldPos + t1 * pixelRay;
        if (abs(dot(pt1 - varCylinderCenter, varCylinderAxis)) < varCylinderLength) {
            intersections[intersectionCount] = pt1;
            intersectionCount++;
        }
        
        float t2 = (-b + Dsqrt)/2/a;
        vec3 pt2 = fragmentWorldPos + t2 * pixelRay;
        if (abs(dot(pt2 - varCylinderCenter, varCylinderAxis)) < varCylinderLength) {
            intersections[intersectionCount] = pt2;
            intersectionCount++;
        }
    }
    //if (D > 0) {
    //if (abs(dot(varCylinderAxis, pixelRay)) < 0.5) {
    if (true) {  
#ifdef INSTANCE_DRAW
        //gl_FragColor = varCylinderColor;
        gl_FragColor = vec4(normalizedX + 0.5, 0, normalizedY + 0.5, 1);
#else
        gl_FragColor = vec4(1, 1, 1, 1);
#endif
    } else {
        gl_FragColor = vec4(varCylinderColor.r, varCylinderColor.g, varCylinderColor.b, 0.1);
    }
    
}
