// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120
#extension GL_EXT_gpu_shader4 : enable
#extension GL_EXT_geometry_shader4 : enable

// not supported until GLSL 150
// ... see GL.Ext.ProgramParameter(..) call
// layout(triangles) in;
// layout (triangles, max_vertices=3) out;

// these are the sigle-pass wireframe variables
uniform vec2 WIN_SCALE;
noperspective varying vec3 dist;

const int MAX_NUM_SMAP_SPLITS = 4;
uniform int numShadowMaps;

uniform bool instanceDrawEnabled;

// these are pass-through variables
varying in vec3 VV[3];
varying in vec3 vertexNormal[3];
varying in vec3 lightPosition[3];
varying in vec3 eyeVec[3];
varying in vec3 vertexPosition_objectspace[3];
varying in vec4 shadowMapCoords[3][MAX_NUM_SMAP_SPLITS];
#ifdef INSTANCE_DRAW
varying in vec4 varInstanceColor[3];
#endif

// non-uniform blocks are not supported until GLSL 330?
varying out vec3 f_VV;
varying out vec3 f_vertexNormal;
varying out vec3 f_lightPosition;
varying out vec3 f_eyeVec;
varying out vec3 f_vertexPosition_objectspace;
varying out vec4 f_shadowMapCoords[MAX_NUM_SMAP_SPLITS];
#ifdef INSTANCE_DRAW
varying out vec4 f_instanceColor;
#endif

varying out vec3 surfaceLightVector;
varying out vec3 surfaceViewVector;
varying out vec3 surfaceNormalVector;

noperspective varying out vec3 f_dist;


// http://www.slideshare.net/Mark_Kilgard/geometryshaderbasedbumpmappingsetup
// http://www.terathon.com/code/tangent.html

void main(void)
{
    // compute tangent
    //vec3 dXYZdU   = vec3(gl_PositionIn[1] - gl_PositionIn[0]);
    //float  dSdU   = gl_TexCoordIn[1][0].s - gl_TexCoordIn[0][0].s;
    //vec3 dXYZdV   = vec3(gl_PositionIn[2] - gl_PositionIn[0]);
    //float  dSdV   = gl_TexCoordIn[2][0].s - gl_TexCoordIn[0][0].s;
    //vec3 tangent  = normalize(dSdV * dXYZdU - dSdU * dXYZdV);

    vec3 dXYZp1 = vec3(gl_PositionIn[1] - gl_PositionIn[0]);
    vec3 dXYZp2 = vec3(gl_PositionIn[2] - gl_PositionIn[0]);
    vec2 dUVp1 = vec2(gl_TexCoordIn[1][0] - gl_TexCoordIn[0][0]);
    vec2 dUVp2 = vec2(gl_TexCoordIn[2][0] - gl_TexCoordIn[0][0]);
    float r = 1.0 / (dUVp1.s * dUVp2.t - dUVp2.s * dUVp1.t);
    vec3 tangent = normalize(r * ( (dUVp2.t * dXYZp1) - (dUVp1.t * dXYZp2) ));
    vec3 bitangent = normalize(r * ( (dUVp1.s * dXYZp2) - (dUVp2.s * dXYZp1) ));
    vec3 tsn = normalize(cross(tangent,bitangent));
    mat3 tangentSpaceMatrix = mat3( tangent, bitangent, tsn );

	// taken from 'Single-Pass Wireframe Rendering'
	vec2 p0 = WIN_SCALE * gl_PositionIn[0].xy/gl_PositionIn[0].w;
	vec2 p1 = WIN_SCALE * gl_PositionIn[1].xy/gl_PositionIn[1].w;
	vec2 p2 = WIN_SCALE * gl_PositionIn[2].xy/gl_PositionIn[2].w;
	vec2 v0 = p2-p1;
	vec2 v1 = p2-p0;
	vec2 v2 = p1-p0;
	float area = abs(v1.x*v2.y - v1.y * v2.x);

	vec3 vertexEdgeDistance[3];
	vertexEdgeDistance[0] = vec3(area/length(v0),0,0);
	vertexEdgeDistance[1] = vec3(0,area/length(v1),0);
	vertexEdgeDistance[2] = vec3(0,0,area/length(v2));

	// suppress warning by assigning this to something to start...
	gl_TexCoord[0] = gl_TexCoordIn[0][0];

	// LOOP for each vertex in the primitive...
	// .. gl_verticiesIn holds the count
	for(int i = 0; i < 3; i++) {

	    // bump tangent-space calculations..
        surfaceLightVector  = normalize(tangentSpaceMatrix * lightPosition[i]);
        surfaceViewVector   = normalize(tangentSpaceMatrix * eyeVec[i]);
        surfaceNormalVector = normalize(tangentSpaceMatrix * vertexNormal[i]);

        // single-pass wireframe information
		f_dist = vertexEdgeDistance[i];
        
        // pass through data
		f_VV = VV[i];
		f_vertexNormal = vertexNormal[i];
		f_lightPosition = lightPosition[i];
		f_eyeVec = eyeVec[i];
        f_vertexPosition_objectspace = vertexPosition_objectspace[i];
        f_shadowMapCoords = shadowMapCoords[i];
        #ifdef INSTANCE_DRAW
        f_instanceColor = varInstanceColor[i];
        #endif
		       
		gl_TexCoord[0] = gl_TexCoordIn[i][0];
		gl_FrontColor = gl_FrontColorIn[i];
		gl_Position = gl_PositionIn[i];
		EmitVertex();
	}
	EndPrimitive(); // not necessary as we only handle triangles
}	
