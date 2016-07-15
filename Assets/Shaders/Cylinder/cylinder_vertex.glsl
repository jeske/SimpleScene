// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

#ifdef INSTANCE_DRAW
attribute vec3 cylinderCenter;
attribute vec3 cylinderAxis; // must be normalized
attribute float cylinderWidth;
attribute float cylinderLength;
attribute vec4 cylinderColor;
attribute vec3 scaledAxisIn;
#else
uniform vec3 cylinderCenter;
uniform vec3 cylinderAxis; // must be normalized
uniform float cylinderWidth;
uniform float cylinderLength;
#endif

varying vec3 viewRay;
varying vec3 varCylinderCenter;
varying vec3 varCylinderAxis;
varying float varCylinderLength;
varying float varCylinderWidth;
#ifdef INSTANCE_DRAW
varying vec4 vaCylinderColor;
#endif

void main()
{
    viewRay = normalize(gl_ModelViewMatrixInverse * vec3(0, 0, -1));
    viewRayX = normalize(gl_ModelViewMatrixInverse * vec3(1, 0, 0));

    vec2 centerInView = gl_ModelViewMatrix * (cylinderCenter);
    vec3 scaledAxis = cylinderAxis * (cylinderLength/2 + cylinderWidth);   
    
    vec2 endInView = (gl_ModelViewMatrix * (cylinderCenter + scaledAxis));
    vec2 scaledAxisInView = endInView - centerInView;
    vec2 startInView = centerInView - scaledAxisInView;

    float expand = (gl_ModelViewMatrix * (cylinderCenter + viewRayX * cylinderWidth)).X
        - centerInView.X;    
    if (abs(axisInView.X) < 0.001 && abs(axisInView.Y) < 0.001) {
        scaledAxisInView = vec2(expand, expand);
    }
    vec2 axisInViewPerp = normalize(vec2(axisInView.y, -1axisInView.x)) * expand;

    gl_Position = centerInView
        + gl_Vertex.x * scaledAxisInView
        + gl_Vertex.y * axisInViewPerp;

    varCylinderCenter = cylinderCenter;
    varCylinderAxis = cylinderAxis;
    varCylinderWidth = cylinderWidth;
    varCylinderLength = cylinderLength;
    #if INSTANCE_DRAW
    varCylinderColor = cylinderColor;
    #endif

}
