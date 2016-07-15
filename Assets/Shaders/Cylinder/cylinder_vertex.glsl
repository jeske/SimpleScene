// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

#ifdef INSTANCE_DRAW
attribute vec3 cylinderCenter;
attribute float cylinderWidth;
attribute float cylinderLength;
attribute vec4 cylinderColorIn;
attribute vec3 cylinderAxisIn; // must be normalized
#else
uniform vec3 cylinderCenter;
uniform vec3 cylinderAxisIn; // must be normalized
#endif

varying vec3 cylinderAxisOut;
varying vec3 viewRay;
#ifdef INSTANCE_DRAW
varying vec4 cylinderColorOut;
#endif

void main()
{

    vec2 centerInView = (gl_ModelViewMatrix * cylinderCenter).xy;
    vec2 axisInView = (gl_ModelViewMatrix * cylinderAxis).xy;   
    vec2 perpAxisInView = vec2(axisInView.y, -axisInView.x);

    gl_Position = centerinView
        + axisInView * gl_Vertex.x * cylinderWidth
        + perpAxisInView*gl_Vertex.y * cylinderLength / 2;

    
    cylinderAxisOut = cylinderAxisIn;
    viewRay = gl_ModelViewMatrixInverse * vec3(0, 0, -1);

}
