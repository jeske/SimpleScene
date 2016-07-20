// Copyright(C) David W. Jeske, 2014, All Rights Reserved.
#version 120

uniform vec4 rotationQuat;
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

// http://www.opengl.org/discussion_boards/showthread.php/160134-Quaternion-functions-for-GLSL
vec3 quatTransform(vec4 q, vec3 v)
{
    return v + 2.0*cross(cross(v, q.xyz ) + q.w*v, q.xyz);
}

void main()
{
   
    #if 1
    vec3 varViewRay = normalize(gl_ModelViewMatrixInverse * vec4(0, 0, -1, 1)).xyz;
    vec3 varViewRayX = normalize(gl_ModelViewMatrixInverse * vec4(1, 0, 0, 1)).xyz;
   
    vec3 centerInView = (gl_ModelViewMatrix * vec4(cylinderCenter, 1)).xyz;
    vec3 scaledAxis = (cylinderLength/2 + cylinderWidth) * cylinderAxis;
    
    vec3 endInView = (gl_ModelViewMatrix * vec4(cylinderCenter + scaledAxis, 1)).xyz;
    vec3 scaledAxisInView = endInView - centerInView;
    vec3 startInView = centerInView - scaledAxisInView;
    vec2 axisInViewPerp = normalize(vec2(-scaledAxisInView.y, scaledAxisInView.x)) * cylinderWidth;
    #endif

    #if 0
    float expand = (gl_ModelViewMatrix * vec4(cylinderCenter + varViewRayX * cylinderWidth, 1.0)).x - centerInView.x;
    if (abs(scaledAxisInView.x) < 0.001 && abs(scaledAxisInView.y) < 0.001) {
        scaledAxisInView = vec2(expand, expand);
    }
    #endif

    

    #if 1
    float theta = -atan(scaledAxisInView.y / scaledAxisInView.x);
    float cosine = cos(theta);
    float sine = sin(theta);
    mat3 oriZ = mat3(cosine, sine, 0, 
                     -sine, cosine, 0, 
                     0, 0, 1);
    #endif

    #if 1
    vec3 combinedPos = gl_Vertex.xyz;
    combinedPos.x *= length(scaledAxisInView.xy);
    combinedPos.y *= cylinderWidth;
    //combinedPos = oriZ * combinedPos;
    combinedPos = quatTransform(rotationQuat, combinedPos);
    combinedPos += cylinderCenter;
    gl_Position = gl_ModelViewProjectionMatrix * vec4(combinedPos, 1);
    #endif

   
    #if 0
    vec3 viewPos = centerInView 
        + gl_Vertex.x * vec3(scaledAxisInView.xy, 0)
        + gl_Vertex.y * vec3(axisInViewPerp, 0);
    gl_Position = gl_ProjectionMatrix * vec4(viewPos, 1);
    #endif

    #if 0
    vec3 viewPos = centerInView
        + oriZ * vec3(gl_Vertex.x * length(scaledAxisInView),
               gl_Vertex.y * cylinderWidth,
               0);
    gl_Position = gl_ProjectionMatrix * vec4(viewPos, 1);
    #endif

    
    varCylinderCenter = cylinderCenter;
    varCylinderAxis = cylinderAxis;
    varCylinderWidth = cylinderWidth;
    varCylinderLength = cylinderLength;
    #ifdef INSTANCE_DRAW
    //varCylinderColor = vec4(0, 0, 0, 1);
    //varCylinderColor.x = length(viewPos - startInView)/cylinderLength;
    //varCylinderColor.y = length(viewPos - endInView)/cylinderLength;
    varCylinderColor = cylinderColor;
    #endif
}

    #if 0
    gl_Position = gl_ProjectionMatrix * vec4(centerInView + gl_Vertex.xyz,
                                            1);

    #endif
    
    #if 0
    vec3 pos = gl_Vertex.xyz;
    pos.x *= cylinderLength / 2;
    pos.y *= cylinderWidth;
    gl_Position = gl_ModelViewProjectionMatrix * vec4(pos + cylinderCenter, 1) ;
    #endif
 
