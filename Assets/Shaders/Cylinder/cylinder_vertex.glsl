// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

#ifdef INSTANCE_DRAW
attribute vec3 cylinderCenter;
attribute vec3 cylinderAxis; // must be normalized
attribute float cylinderWidth;
attribute float cylinderLength;
attribute vec4 cylinderColor;
#else
uniform vec3 cylinderCenter;
uniform vec3 cylinderAxis; // must be normalized
uniform float cylinderWidth;
uniform float cylinderLength;
#endif
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
    vec3 varViewRay = normalize(gl_ModelViewMatrixInverse * vec4(0, 0, -1, 1)).xyz;
    vec3 varViewRayX = normalize(gl_ModelViewMatrixInverse * vec4(1, 0, 0, 1)).xyz;

    vec2 centerInView = (gl_ModelViewMatrix * vec4(cylinderCenter, 1)).xy;
    vec3 scaledAxis = cylinderAxis * (cylinderLength/2 + cylinderWidth);
    
    vec2 endInView = (gl_ModelViewMatrix * vec4(cylinderCenter + scaledAxis, 1)).xy;
    vec2 scaledAxisInView = endInView - centerInView;
    vec2 startInView = centerInView - scaledAxisInView;

    float expand = (gl_ModelViewMatrix * vec4(cylinderCenter + varViewRayX * cylinderWidth, 1.0)).x - centerInView.x;
    if (abs(scaledAxisInView.x) < 0.001 && abs(scaledAxisInView.y) < 0.001) {
        scaledAxisInView = vec2(expand, expand);
    }
    vec2 axisInViewPerp = normalize(vec2(scaledAxisInView.y, -scaledAxisInView.x));

    gl_Position = gl_ProjectionMatrix * vec4(
       centerInView + gl_Vertex.x*scaledAxisInView + gl_Vertex.y*axisInViewPerp*expand,
       0.0, 1.0);                
    //gl_Position = gl_ProjectionMatrix * vec4(0, 0, 0, 1);

        //gl_Position /= gl_Position.w;
    
    varCylinderCenter = cylinderCenter;
    varCylinderAxis = cylinderAxis;
    varCylinderWidth = cylinderWidth;
    varCylinderLength = cylinderLength;
    #ifdef INSTANCE_DRAW
    varCylinderColor = cylinderColor;
    #endif

}
