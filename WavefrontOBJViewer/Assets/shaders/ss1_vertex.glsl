// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

#version 120
				
	// in eye-space/camera space
	varying vec3 f_vertexNormal;
	varying vec3 f_lightPosition;
	varying vec3 f_eyeVec;

	varying vec3 n;  // vertex normal
	varying vec3 VV; // vertex position

    varying vec3 f_vertexPosition_objectspace;

void main()
{
	gl_TexCoord[0] =  gl_MultiTexCoord0;  // output base UV coordinates

    f_vertexPosition_objectspace = gl_Vertex.xyz;

	// transform into eye-space
	f_vertexNormal = n = normalize (gl_NormalMatrix * gl_Normal);
	vec4 vertexPosition = gl_ModelViewMatrix * gl_Vertex;
	VV = vec3(vertexPosition);
	f_lightPosition = (gl_LightSource[0].position - vertexPosition).xyz;
	f_eyeVec = -normalize(vertexPosition).xyz;

	gl_Position = ftransform();
}	