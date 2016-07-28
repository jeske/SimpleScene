// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

uniform float screenWidth;
uniform float screenHeight;
uniform mat4 viewMatrixInverse;

//varying vec3 varViewRay;
varying vec3 varCylCenter;
varying vec3 varCylXAxis;
varying vec3 varCylYAxis;
varying vec3 varCylZAxis;
varying float varCylLength;
varying float varCylWidth;
#ifdef INSTANCE_DRAW
varying vec4 varCylColor;
#endif

vec3 toCylProj(vec3 worldVec)
{
    return vec3(dot(worldVec, varCylXAxis),
                dot(worldVec, varCylYAxis),
                dot(worldVec, varCylZAxis));
}

vec3 toCylCoords(vec3 worldCoords)
{
    return toCylProj(worldCoords - varCylCenter);
}

// https://community.eveonline.com/news/dev-blogs/re-inventing-the-trails/

vec3 unproject(vec2 screenPt, float z)
{
    vec4 vec = vec4(2 * screenPt.x / screenWidth - 1,
                    2 * screenPt.y / screenHeight - 1,
                    z, 1);
    vec = viewMatrixInverse * (gl_ProjectionMatrixInverse * vec);
    if (abs(vec.w) > 1.401298E-45) {
        vec /= vec.w;
    }
    return vec.xyz;
}  

void main()
{
    vec3 intersections[2];
    int intersectionCount = 0;
    gl_FragColor = vec4(varCylColor.rgb, 0.1); // sem-transparent debugging default   

    vec3 pixelWorldPos1 = unproject(gl_FragCoord.xy, -1.5);
    vec3 pixelWorldPos2 = unproject(gl_FragCoord.xy, 1.0);
    vec3 pixelRay = normalize(pixelWorldPos2 - pixelWorldPos1);
    vec3 localPixelRay = toCylProj(pixelRay);
    vec3 localPixelPos = toCylCoords(pixelWorldPos2);
    //vec3 localPixelRay = vec3(0, 1, 0);
    //vec3 localPixelPos = vec3(-100, 0, 0);
    float cylRadius = varCylWidth / 2;
    float cylHalfLength = varCylLength / 2;
    if (abs(localPixelRay.x) > 0.0001 || abs(localPixelRay.y) > 0.0001) {
        // view ray is not parallel to cylinder axis so it may intersect the sides
        // solve: (p_x + dir_x * t)^2 + (p_y + dir_y * t)^2 = r^2; in quadratic form:
        // (dir_x^2 + dir_y^2) * t^2 + [2(p_x * dir_x + py * dir_y)] * t
        //                                                  + (p_x^2 + p_y^2 - r^2) == 0
        float a = dot(localPixelRay.xy, localPixelRay.xy);
        float b = 2 * dot(localPixelPos.xy, localPixelRay.xy);
        float c = dot(localPixelPos.xy, localPixelPos.xy) - cylRadius*cylRadius;
        float D = b*b - 4*a*c;
        if (D > 0) { // two solutions
            float Dsqrt = sqrt(D);
            {
                float t1 = (-b - Dsqrt) / (2*a);
                vec3 intrPos1 = localPixelPos * localPixelRay * t1;
                if (abs(intrPos1.z) <= cylHalfLength) { // check against cylinder bounds
                //if (true) {
                    intersections[intersectionCount] = intrPos1;
                    intersectionCount++;
                }
            }
            {
                float t2 = (-b + Dsqrt) / (2*a);
                vec3 intrPos2 = localPixelPos * localPixelRay * t2;
                if (abs(intrPos2.z) <= cylHalfLength) { // check against cylinder  bounds
                //if (true) {
                    intersections[intersectionCount] = intrPos2;
                    intersectionCount++;
                }
            }
            gl_FragColor = varCylColor;
        } else if (D == 0) {
            gl_FragColor = varCylColor;

        }
        // D < 0 means no solutions; D == 0 means one solution: the ray is "scraping" the
        // cylinder; we can probably ignore this case
    }
}

    #if 0
    vec3 proj1 = pixelWorldPos - dot(pixelWorldPos, varCylAxis) * varCylAxis;
    vec3 proj2 = pixelRay - varCylAxis - dot(pixelRay - varCylAxis, varCylAxis) * varCylAxis;
    float a = dot(proj1, proj1);
    float b = 2 * dot(proj1, proj2);
    float c = dot(proj2, proj2) - cylRadius * cylRadius;
    float D = b*b - 4*a*c;

    if (D > 0) {
        float Dsqrt = sqrt(D);
        float t1 = (-b - Dsqrt)/2/a;
        vec3 pt1 = pixelWorldPos + t1 * pixelRay;
        if (abs(dot(pt1 - varCylCenter, varCylAxis)) < varCylLength) {
            intersections[intersectionCount] = pt1;
            intersectionCount++;
        }
        
        float t2 = (-b + Dsqrt)/2/a;
        vec3 pt2 = pixelWorldPos + t2 * pixelRay;
        if (abs(dot(pt2 - varCylCenter, varCylAxis)) < varCylLength) {
            intersections[intersectionCount] = pt2;
            intersectionCount++;
        }
    }
    //if (D > 0) {
    if (intersectionCount == 2) {
    //if (abs(dot(varCylAxis, pixelRay)) < 0.5) {
    //if (true) {  
#ifdef INSTANCE_DRAW
        gl_FragColor = varCylColor;
        //gl_FragColor = vec4(viewRay, 1);
#else
            gl_FragColor = vec4(1, 1, 1, 1);
#endif
    } else {
        gl_FragColor = vec4(varCylColor.rgb, 0.1);
    }
#endif
