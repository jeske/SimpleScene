// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

uniform float screenWidth;
uniform float screenHeight;
uniform mat4 viewMatrixInverse;
uniform float distanceToAlpha;
uniform float alphaMin;
uniform float alphaMax;

//varying vec3 varViewRay;
varying vec3 varCylCenter;
varying vec3 varCylXAxis;
varying vec3 varCylYAxis;
varying vec3 varCylZAxis;
varying vec3 varPrevJointAxis;
varying vec3 varNextJointAxis;
varying float varCylLength;
varying float varCylWidth;
varying vec4 varCylColor;
varying vec4 varCylInnerColor;
varying float varInnerColorRatio;
varying float varOuterColorRatio;

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

void cylinderIntersections(float radiusSq,
                           float halfLength,
                           vec3 localPixelPos,
                           vec3 localPixelRay,
                           vec3 localPrevJointAxis,
                           vec3 localNextJointAxis,
                           out vec3 intersections[2],
                           out int intersectionCount,
                           out vec4 debugColor)
{
    //vec3 intersections[2];
    intersectionCount = 0;
    vec3 prevJointPos = vec3(0, 0, -halfLength);
    vec3 nextJointPos = vec3(0, 0, halfLength);
 
    debugColor = vec4(0, 0, 0, 1);
    if (abs(localPixelRay.x) > 0.00001 || abs(localPixelRay.y) > 0.00001) {
        // view ray is not parallel to cylinder axis so it may intersect the sides
        // solve: (p_x + dir_x * t)^2 + (p_y + dir_y * t)^2 = r^2; in quadratic form:
        // (dir_x^2 + dir_y^2) * t^2 + [2(p_x * dir_x + py * dir_y)] * t
        //                                                  + (p_x^2 + p_y^2 - r^2) == 0
        float a = dot(localPixelRay.xy, localPixelRay.xy);
        float b = 2 * dot(localPixelPos.xy, localPixelRay.xy);
        float c = dot(localPixelPos.xy, localPixelPos.xy) - radiusSq;
        float D = b*b - 4*a*c;
        if (D > 0) { // two solutions
            float Dsqrt = sqrt(D);
            {
                float t1 = (-b - Dsqrt) / (2*a);
                vec3 intrPos1 = localPixelPos + localPixelRay * t1;
                // check against the bounding planes
                if (dot(localPrevJointAxis, intrPos1 - prevJointPos) < 0
                 && dot(localNextJointAxis, intrPos1 - nextJointPos) < 0) {
                    intersections[intersectionCount] = intrPos1;
                    intersectionCount++;
                    debugColor.r = 1;
                }
            }
            {
                float t2 = (-b + Dsqrt) / (2*a);
                vec3 intrPos2 = localPixelPos + localPixelRay * t2;
                // check against the bounding planes
                if (dot(localPrevJointAxis, intrPos2 - prevJointPos) < 0
                 && dot(localNextJointAxis, intrPos2 - nextJointPos) < 0) {
                    intersections[intersectionCount] = intrPos2;
                    intersectionCount++;
                }
                debugColor.r = 1;
            }
            //gl_FragColor = varCylColor;
            debugColor.g = 1;
        }
        // D < 0 means no solutions; D == 0 means one solution: the ray is "scraping" the
        // cylinder; we can probably ignore this case
        #if 0
        else if (abs(D) < 0.00001) {
            float t1 = (-b) / (2*a);
            vec3 intrPos1 = localPixelPos + localPixelRay * t1;
            // check against the bounding planes
            if (dot(localPrevJointAxis, intrPos1 - prevJointPos) < 0
             && dot(localNextJointAxis, intrPos1 - nextJointPos) < 0) {
                intersections[intersectionCount] = intrPos1;
                intersectionCount++;
                debugColor.r = 1;            
            }
        }
        #endif
    }
    if (intersectionCount < 2 && abs(localPixelRay.z) > 0.00001) {
        // dont have the two intersections yet and the pixel ray is not parallel to
        // cylinder planes; test for cylinder's plane #1 and/or #2
        // solve: n . (p0 + dir * t - r0) == 0
        //float t3 = (cylHalfLength - localPixelPos.z) / localPixelRay.z;
        float t3 = (localPrevJointAxis.z * -halfLength - dot(localPrevJointAxis, localPixelPos))
            / dot(localPrevJointAxis, localPixelRay);
        vec3 intrPos3 = localPixelPos + localPixelRay * t3;
        if (dot(intrPos3.xy, intrPos3.xy) < radiusSq) {
            intersections[intersectionCount] = intrPos3;
            intersectionCount++;
            debugColor.b = 1;
        }
        if (intersectionCount < 2) {
            //float t4 = (-cylHalfLength - localPixelPos.z) / localPixelRay.z;
            float t4
                = (localNextJointAxis.z * halfLength - dot(localNextJointAxis, localPixelPos))
            / dot(localNextJointAxis, localPixelRay);
            vec3 intrPos4 = localPixelPos + localPixelRay * t4;
            if (dot(intrPos4.xy, intrPos4.xy) < radiusSq) {
                intersections[intersectionCount] = intrPos4;
                intersectionCount++;
                debugColor.b = 1;
            }
        }
    }
    //
    //if (intersectionCount == 2) { 
    //    return distance(intersections[0], intersections[1]);
    //} else {
    //    return 0;
    //}
}

float cylinderIntersectionDist(float radiusSq,
                               float halfLength,
                               vec3 localPixelPos,
                               vec3 localPixelRay,
                               vec3 localPrevJointAxis,
                               vec3 localNextJointAxis,
                               out vec4 debugColor)
{
    vec3 intersections[2];
    int intersectionCount;
    cylinderIntersections(radiusSq, halfLength, localPixelPos, localPixelRay, localPrevJointAxis, localNextJointAxis, intersections, intersectionCount, debugColor);
    if (intersectionCount == 2) {
        return distance(intersections[0], intersections[1]);
    } else {
        return 0;
    }
}

void main()
{
 
    vec3 pixelWorldPos1 = unproject(gl_FragCoord.xy, 1);
    vec3 pixelWorldPos2 = unproject(gl_FragCoord.xy, 10);
    vec3 pixelRay = normalize(pixelWorldPos2 - pixelWorldPos1);
    vec3 localPixelRay = toCylProj(pixelRay);
    vec3 localPixelPos = toCylCoords(pixelWorldPos2);
    vec3 localPrevJointAxis = toCylProj(varPrevJointAxis);
    vec3 localNextJointAxis = toCylProj(varNextJointAxis);
    //vec3 localPixelRay = vec3(0, 1, 0);
    //vec3 localPixelPos = vec3(-100, 0, 0);
    float cylRadius = varCylWidth / 2;
    float cylRadiusSq = cylRadius*cylRadius;
    float cylHalfLength = varCylLength / 2;
    vec4 debugColor, debugColor2, debugColor3;
    
    float outerDist = cylinderIntersectionDist(
       cylRadiusSq, cylHalfLength, localPixelPos, localPixelRay,
       localPrevJointAxis, localNextJointAxis, debugColor);    
    if (outerDist > 0) {
        float alpha = clamp(outerDist * distanceToAlpha, alphaMin, alphaMax);
        float innerRadius = cylRadius * varInnerColorRatio;
        float outerBoundaryRadius = cylRadius * (1 - varOuterColorRatio);
        float innerRadiusSq = innerRadius * innerRadius;
        float outerBoundaryRadiusSq = outerBoundaryRadius * outerBoundaryRadius;
        float innerDist = cylinderIntersectionDist(
                innerRadiusSq, cylHalfLength, localPixelPos, localPixelRay,
                localPrevJointAxis, localNextJointAxis, debugColor2);

        vec3 middleIntersections[2];
        int middleIntersectionCount;
        cylinderIntersections(
            outerBoundaryRadiusSq, cylHalfLength, localPixelPos, localPixelRay,
            localPrevJointAxis, localNextJointAxis,
            middleIntersections, middleIntersectionCount, debugColor3);
        float middleDist = (middleIntersectionCount < 2) ? 0.0
                           : distance(middleIntersections[0], middleIntersections[1]);
        float fadeDist = middleDist - innerDist;
        float fadeAvgRate;
        if (innerDist > 0) {
            fadeAvgRate = 0.5;            
        } else {
            vec3 middleIntrMiddle = (middleIntersections[0] + middleIntersections[1]) / 2;
            float middleIntrMiddleRate = (length(middleIntrMiddle.xy) - innerRadius)
                / (outerBoundaryRadius - innerRadius);
            fadeAvgRate = (1 - middleIntrMiddleRate)/2;
        }
        float ratio = (innerDist + fadeAvgRate * fadeDist) / outerDist;
        //float ratio = innerDist / outerDist;
        //vec4 color = mix(varCylInnerColor, varCylColor, ratio);
        vec4 color = mix(varCylColor, varCylInnerColor, ratio);
        gl_FragColor = vec4(color.rgb, color.a * alpha);
    } else {
         discard;
         // gl_FragColor = vec4(varCylColor.rgb, 0.1); // sem-transparent debugging default
    }
}

#if false
    cylinderIntersectionDist(
                               float radiusSq,
                               float halfLength,
                               vec3 localPixelPos,
                               vec3 localPixelRay,
                               vec3 localPrevJointAxis,
                               vec4 localNextJointAxis,
                               out vec4 debugColor);
#endif

