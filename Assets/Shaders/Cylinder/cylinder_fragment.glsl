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

const int NUM_LAYERS = 3;
const int OUTER = 0;
const int OUTER_OFFSET = OUTER * 2;
const int FADE = 1;
const int FADE_OFFSET = FADE * 2;
const int INNER = 2;
const int INNER_OFFSET = INNER * 2;

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

void cylindersIntersections(float radiusesSq[NUM_LAYERS],
                            float halfLength,
                            vec3 localPixelPos,
                            vec3 localPixelRay,
                            vec3 localPrevJointAxis,
                            vec3 localNextJointAxis,
                            out vec3 intersections[NUM_LAYERS*2],
                            out int intersectionCounts[NUM_LAYERS],
                            out vec3 fadePivot,
                            out vec4 debugColor[NUM_LAYERS])
{
    vec3 prevJointPos = vec3(0, 0, -halfLength);
    vec3 nextJointPos = vec3(0, 0, halfLength);
 
    fadePivot = vec3(0, 0, 0);
    if (abs(localPixelRay.x) > 0 || abs(localPixelRay.y) > 0) {
        // view ray is not parallel to cylinder axis so it may intersect the sides
        // solve: (p_x + dir_x * t)^2 + (p_y + dir_y * t)^2 = r^2; in quadratic form:
        // (dir_x^2 + dir_y^2) * t^2 + [2(p_x * dir_x + py * dir_y)] * t
        //                                                  + (p_x^2 + p_y^2 - r^2) == 0
        float a = dot(localPixelRay.xy, localPixelRay.xy);
        float b = 2 * dot(localPixelPos.xy, localPixelRay.xy);
        for (int i = 0; i < NUM_LAYERS; ++i) {
            int intrOffset = i * 2;
            debugColor[i] = vec4(0, 0, 0, 1);
            int intersectionCount = 0;
            float c = dot(localPixelPos.xy, localPixelPos.xy) - radiusesSq[i];
            float D = b*b - 4*a*c;
            if (D >= 0) { // two solutions (or one as two)
                float Dsqrt = sqrt(D);
                {
                    float t1 = (-b - Dsqrt) / (2*a);
                    vec3 intrPos1 = localPixelPos + localPixelRay * t1;
                    if (i == FADE) {
                        fadePivot += (intrPos1 / 2);
                    }
                    // check against the bounding planes
                    if (dot(localPrevJointAxis, intrPos1 - prevJointPos) <= 0
                     && dot(localNextJointAxis, intrPos1 - nextJointPos) <= 0) {
                        intersections[intrOffset + intersectionCount] = intrPos1;
                        intersectionCount++;
                        debugColor[i].r += 0.5;
                    }
                }
                {
                    float t2 = (-b + Dsqrt) / (2*a);
                    vec3 intrPos2 = localPixelPos + localPixelRay * t2;
                    if (i == FADE) {
                        fadePivot += (intrPos2 / 2);
                    }
                    // check against the bounding planes
                    if (dot(localPrevJointAxis, intrPos2 - prevJointPos) <= 0
                     && dot(localNextJointAxis, intrPos2 - nextJointPos) <= 0) {
                        intersections[intrOffset + intersectionCount] = intrPos2;
                        intersectionCount++;
                    }
                    debugColor[i].r += 0.5;
                }
                //gl_FragColor = varCylColor;
                debugColor[i].g = 1;
                intersectionCounts[i] = intersectionCount;
            } else {
                // no intersection for a wider infinite cylinder means no
                // intersections for a smaller infinite cylinder
                break;
            }
        }
    }   
    if (abs(localPixelRay.z) > 0) {
        // dont have the two intersections yet and the pixel ray is not parallel to
        // cylinder planes; test for cylinder's plane #1 and/or #2
        // solve: n . (p0 + dir * t - r0) == 0
        float t3 = (localPrevJointAxis.z * -halfLength - dot(localPrevJointAxis, localPixelPos))
            / dot(localPrevJointAxis, localPixelRay);
        vec3 intrPos3 = localPixelPos + localPixelRay * t3;
        float dist3 = dot(intrPos3.xy, intrPos3.xy);

        float t4 = (localNextJointAxis.z * halfLength - dot(localNextJointAxis, localPixelPos))
            / dot(localNextJointAxis, localPixelRay);
        vec3 intrPos4 = localPixelPos + localPixelRay * t4;
        float dist4 = dot(intrPos4.xy, intrPos4.xy);
        
        for (int i = 0; i < NUM_LAYERS; ++i) {
            int intersectionCount = intersectionCounts[i];
            int intrOffset = i * 2;
            if (intersectionCount < 2 && dist3 <= radiusesSq[i]) {
                intersections[intrOffset + intersectionCount] = intrPos3;
                intersectionCount++;
                debugColor[i].b = 1;
            }
            if (intersectionCount < 2 && dist4 <= radiusesSq[i]) {
                intersections[intrOffset + intersectionCount] = intrPos4;
                intersectionCount++;
                debugColor[i].b = 1;
            }
            intersectionCounts[i] = intersectionCount;
            if (intersectionCount == 0) {
                // larger bounded cylinder not intersected? means neither are smaller ones
                break; 
            }
        }       
    }
}
                              
// contribution = dist * avgColorRatio
float linearFadeContribution(vec3 fadeIntr1, vec3 fadeIntr2,
                             float rInner, float rFadeEnd)
{  
    vec3 middle = (fadeIntr1 + fadeIntr2) / 2;
    //float avgRate = 1 - (length(middle.xy) - rInner) / (rFadeEnd - rInner);
    float avgRate = 1 - (length(middle.xy) - rInner) / (rFadeEnd - rInner);
    return avgRate * distance(fadeIntr1, fadeIntr2);
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

    float cylHalfLength = varCylLength / 2;        
    float cylRadius = varCylWidth / 2;
    float cylRadiusFade = cylRadius * (1 - varOuterColorRatio);
    float cylRadiusInner = cylRadius * varInnerColorRatio;
    float radiusesSq[NUM_LAYERS];
    vec4 debugColor[NUM_LAYERS];
    vec3 intersections[NUM_LAYERS * 2];
    int intersectionCounts[NUM_LAYERS];
    vec3 fadePivot;
    
    radiusesSq[OUTER] = cylRadius * cylRadius;
    radiusesSq[FADE] = cylRadiusFade * cylRadiusFade;
    radiusesSq[INNER] = cylRadiusInner * cylRadiusInner;     
    intersectionCounts[OUTER] = 0;
    intersectionCounts[FADE] = 0;
    intersectionCounts[INNER] = 0;
    
    cylindersIntersections(
        radiusesSq, cylHalfLength, localPixelPos, localPixelRay,
        localPrevJointAxis, localNextJointAxis,
        intersections, intersectionCounts, fadePivot, debugColor);

    if (intersectionCounts[OUTER] == 2) {
        // outer, uniform color cylinder intersected
        float outerDist = distance(intersections[OUTER_OFFSET], intersections[OUTER_OFFSET+1]);
        float innerContribution = 0;
        float outerContribution = outerDist;
        
        if (intersectionCounts[FADE] == 2) {
            // fade cylinder is intersected
            if (intersectionCounts[INNER] == 2) {
                // inner, uniform color cylinder is intersected               
                vec3 seg1 = intersections[INNER_OFFSET] - intersections[FADE_OFFSET];
                vec3 seg2 = intersections[INNER_OFFSET + 1] - intersections[FADE_OFFSET];
                if (dot(seg2, seg2) < dot(seg1, seg1)) {
                    // ensure intersections are matched by closeness (distance squared)
                    vec3 temp = intersections[FADE_OFFSET];
                    intersections[FADE_OFFSET] = intersections[FADE_OFFSET + 1];
                    intersections[FADE_OFFSET + 1] = temp;
                }
                
                innerContribution = distance(intersections[INNER_OFFSET], intersections[INNER_OFFSET + 1])
                  + linearFadeContribution(intersections[FADE_OFFSET], intersections[INNER_OFFSET],
                                           cylRadiusInner, cylRadiusFade)
                  + linearFadeContribution(intersections[FADE_OFFSET + 1], intersections[INNER_OFFSET + 1],
                                           cylRadiusInner, cylRadiusFade);
            } else if (dot(fadePivot, fadePivot) > 0 &&
                       dot(fadePivot - intersections[FADE_OFFSET], 
                           intersections[FADE_OFFSET + 1] - fadePivot) > 0) {
                // ray through fade cylinder:
                // pivot divides non-linear fade contribution into two linear regions
                innerContribution
                    = linearFadeContribution(intersections[FADE_OFFSET], fadePivot,
                                             cylRadiusInner, cylRadiusFade)
                    + linearFadeContribution(fadePivot, intersections[FADE_OFFSET + 1],
                                             cylRadiusInner, cylRadiusFade);
            } else {
                // ray through fade cylinder:
                // fade pivot outside the segment between intersections. simple linear fade is ok
                innerContribution
                    = linearFadeContribution(intersections[FADE_OFFSET], intersections[FADE_OFFSET + 1],
                                             cylRadiusInner, cylRadiusFade);                
            }
            outerContribution = outerDist - innerContribution;
        }

        float innerFactor = innerContribution * varCylInnerColor.a;
        float outerFactor = outerContribution * varCylColor.a;
        float totalFactor = innerFactor + outerFactor;
        float colorRatio = innerFactor / totalFactor;
        vec3 colorBase = mix(varCylColor.rgb, varCylInnerColor.rgb, colorRatio);
        float alpha = clamp(totalFactor * distanceToAlpha, alphaMin, alphaMax);
        gl_FragColor = vec4(colorBase, alpha);
        //gl_FragColor = vec4(debugColor4.rgb, 0.5);
    } else {
         // outer cylinder not intersected; nothing to do
         discard;
         // gl_FragColor = vec4(varCylColor.rgb, 0.1); // sem-transparent debugging default
    }
}


#if 0
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
    vec4 debugColor, debugColor2, debugColor3, debugColor4;
    
    float outerDist = cylinderIntersectionDist(
       cylRadiusSq, cylHalfLength, localPixelPos, localPixelRay,
       localPrevJointAxis, localNextJointAxis, debugColor);    
    if (outerDist > 0) {
        float innerRadius = cylRadius * varInnerColorRatio;
        float fadeEndRadius = cylRadius * (1 - varOuterColorRatio);
        float innerRadiusSq = innerRadius * innerRadius;
        float fadeEndRadiusSq = fadeEndRadius * fadeEndRadius;

        float innerContribution = 0;
        float outerContribution = outerDist;
        
        vec3 fadeEndIntersections[2];
        int fadeEndIntersectionCount;
        vec3 fadePivot, fadePivotDummy;
        cylinderIntersections(
            fadeEndRadiusSq, cylHalfLength, localPixelPos, localPixelRay,
            localPrevJointAxis, localNextJointAxis,
            fadeEndIntersections, fadeEndIntersectionCount, fadePivot, debugColor3);
        if (fadeEndIntersectionCount == 2) {
            vec3 innerIntersections[2];
            int innerIntersectionCount;
            cylinderIntersections(
                innerRadiusSq, cylHalfLength, localPixelPos, localPixelRay,
                localPrevJointAxis, localNextJointAxis,
                innerIntersections, innerIntersectionCount, fadePivotDummy, debugColor4);
            if (innerIntersectionCount == 2) {
                // inner, uniform density cylinder is intersected
                if (distance(fadeEndIntersections[0], innerIntersections[1]) // TODO use distance-squared?
                  < distance(fadeEndIntersections[0], innerIntersections[0])) {
                    // ensure intersections are pro
                        vec3 temp = fadeEndIntersections[0];
                        fadeEndIntersections[0] = fadeEndIntersections[1];
                        fadeEndIntersections[1] = temp;
                    }
                
                innerContribution = distance(innerIntersections[0], innerIntersections[1])
                  + linearFadeContribution(fadeEndIntersections[0], innerIntersections[0],
                                           innerRadius, fadeEndRadius)
                  + linearFadeContribution(fadeEndIntersections[1], innerIntersections[1],
                                           innerRadius, fadeEndRadius);
            } else if (dot(fadePivot,fadePivot) > 0 &&
                       dot(fadePivot-fadeEndIntersections[0], 
                           fadeEndIntersections[1]-fadePivot) > 0) {
                // ray through fade cylinder:
                // pivot divides non-linear fade contribution into two linear regions
                innerContribution
                    = linearFadeContribution(fadeEndIntersections[0], fadePivot,
                                             innerRadius, fadeEndRadius)
                    + linearFadeContribution(fadePivot, fadeEndIntersections[1],
                                             innerRadius, fadeEndRadius);
            } else {
                // ray through fade cylinder:
                // fade pivot outside the segment between intersections. simple linear fade is ok
                innerContribution
                    = linearFadeContribution(fadeEndIntersections[0], fadeEndIntersections[1],
                                             innerRadius, fadeEndRadius);                
               
            }
            outerContribution = outerDist - innerContribution;
        }

        //float alpha = clamp(outerDist * distanceToAlpha, alphaMin, alphaMax);
        float innerFactor = innerContribution * varCylInnerColor.a;
        float outerFactor = outerContribution * varCylColor.a;
        float totalFactor = innerFactor + outerFactor;
        float colorRatio = innerFactor / totalFactor;
        vec3 colorBase = mix(varCylColor.rgb, varCylInnerColor.rgb, colorRatio);
        float alpha = clamp(totalFactor * distanceToAlpha, alphaMin, alphaMax);
        gl_FragColor = vec4(colorBase, alpha);
        //gl_FragColor = vec4(debugColor4.rgb, 0.5);
    } else {
         discard;
         // gl_FragColor = vec4(varCylColor.rgb, 0.1); // sem-transparent debugging default
    }
}
#endif

#if 0
// works for finding the maxima for the fade rate, which divides the distance into
// two linear regions
float nonLinearFadeContribution(vec3 fadeIntr1, vec3 fadeIntr2, float rInner, float rFadeEnd)
{
    // extend to edges
    #if 1
    vec3 pivot;
    pivot = (fadeIntr2 + fadeIntr1) / 2;
    #else
    vec3 diff = fadeIntr2 - fadeIntr1;
    pivot.xy = fadeIntr1.xy - dot(diff.xy, fadeIntr1.xy) * diff.xy / length(diff.xy);
    pivot.z = fadeIntr1.z + distance(pivot.xy, fadeIntr1.xy) / length(diff.xy) * diff.z;
    #endif
    // TODO handle diff.x == 0
    
    return linearFadeContribution(fadeIntr1, pivot, rInner, rFadeEnd)
         + linearFadeContribution(pivot, fadeIntr2, rInner, rFadeEnd);
}
#endif


#if 0
    cylinderIntersectionDist(
                               float radiusSq,
                               float halfLength,
                               vec3 localPixelPos,
                               vec3 localPixelRay,
                               vec3 localPrevJointAxis,
                               vec4 localNextJointAxis,
                               out vec4 debugColor);
#endif


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

#if 0
void cylinderIntersections(float radiusSq,
                           float halfLength,
                           vec3 localPixelPos,
                           vec3 localPixelRay,
                           vec3 localPrevJointAxis,
                           vec3 localNextJointAxis,
                           out vec3 intersections[2],
                           out int intersectionCount,
                           out vec3 fadePivot,
                           out vec4 debugColor)
{
    //vec3 intersections[2];
    intersectionCount = 0;
    vec3 prevJointPos = vec3(0, 0, -halfLength);
    vec3 nextJointPos = vec3(0, 0, halfLength);
 
    debugColor = vec4(0, 0, 0, 1);
    fadePivot = vec3(0, 0, 0);
    if (abs(localPixelRay.x) > 0 || abs(localPixelRay.y) > 0) {
        // view ray is not parallel to cylinder axis so it may intersect the sides
        // solve: (p_x + dir_x * t)^2 + (p_y + dir_y * t)^2 = r^2; in quadratic form:
        // (dir_x^2 + dir_y^2) * t^2 + [2(p_x * dir_x + py * dir_y)] * t
        //                                                  + (p_x^2 + p_y^2 - r^2) == 0
        float a = dot(localPixelRay.xy, localPixelRay.xy);
        float b = 2 * dot(localPixelPos.xy, localPixelRay.xy);
        float c = dot(localPixelPos.xy, localPixelPos.xy) - radiusSq;
        float D = b*b - 4*a*c;
        if (D >= 0) { // two solutions (or one as two)
            float Dsqrt = sqrt(D);
            {
                float t1 = (-b - Dsqrt) / (2*a);
                vec3 intrPos1 = localPixelPos + localPixelRay * t1;
                fadePivot += (intrPos1 / 2);
                // check against the bounding planes
                if (dot(localPrevJointAxis, intrPos1 - prevJointPos) < 0
                 && dot(localNextJointAxis, intrPos1 - nextJointPos) < 0) {
                    intersections[intersectionCount] = intrPos1;
                    intersectionCount++;
                    debugColor.r += 0.5;
                }
            }
            {
                float t2 = (-b + Dsqrt) / (2*a);
                vec3 intrPos2 = localPixelPos + localPixelRay * t2;
                fadePivot += (intrPos2 / 2);
                // check against the bounding planes
                if (dot(localPrevJointAxis, intrPos2 - prevJointPos) < 0
                 && dot(localNextJointAxis, intrPos2 - nextJointPos) < 0) {
                    intersections[intersectionCount] = intrPos2;
                    intersectionCount++;
                }
                debugColor.r += 0.5;
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
#endif

#if 0
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
    vec3 fadePivotDummy; // not used
    cylinderIntersections(radiusSq, halfLength, localPixelPos, localPixelRay, localPrevJointAxis,
                          localNextJointAxis, intersections, intersectionCount,
                          fadePivotDummy, debugColor);
    if (intersectionCount == 2) {
        return distance(intersections[0], intersections[1]);
    } else {
        return 0;
    }
}
#endif
