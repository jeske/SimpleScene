// Copyright(C) David W. Jeske, 2014, All Rights Reserved.

#version 120
#extension GL_EXT_gpu_shader4 : enable

// edge distance from geometry shader..
noperspective varying vec3 f_dist;

uniform sampler2D diffTex;
uniform sampler2D specTex;
uniform sampler2D ambiTex;
uniform sampler2D bumpTex;

uniform int diffTexEnabled;
uniform int specTexEnabled;
uniform int ambiTexEnabled;
uniform int bumpTexEnabled;
uniform int lighted;

uniform int directionalLightIndex; // -1 = no directional light
uniform int lightingMode;

uniform int showWireframes;
uniform float animateSecondsOffset;

// eye-space/cameraspace coordinates
varying vec3 f_VV;
varying vec3 f_vertexNormal;
varying vec3 f_lightPosition;
varying vec3 f_eyeVec;
varying vec3 f_vertexPosition_objectspace;
#ifdef INSTANCE_DRAW
varying vec4 f_instanceColor;
#endif

// tangent space vectors for bump mapping
varying vec3 surfaceLightVector;   
varying vec3 surfaceViewVector;
varying vec3 surfaceNormalVector;

// shadowmap related
uniform int receivesShadow;
uniform bool poissonSamplingEnabled;
uniform int numPoissonSamples;
uniform int numShadowMaps;
uniform sampler2D shadowMapTexture;
const int MAX_NUM_SMAP_SPLITS = 4;
uniform float shadowMapViewSplits[MAX_NUM_SMAP_SPLITS];
uniform vec2 poissonScale[MAX_NUM_SMAP_SPLITS];

varying vec4 f_shadowMapCoords[MAX_NUM_SMAP_SPLITS];

const float maxLitReductionByShade = 0.7;

vec2 poissonDisk[16] = vec2[] (
    vec2(-0.04770581, 0.1478396),
    vec2(0.3363894, 0.4504989),
    vec2(-0.2229154, 0.66614),
    vec2(0.2214093, -0.218469),
    vec2(0.6464681, 0.0007115538),
    vec2(-0.4084882, -0.2793796),
    vec2(-0.6255119, 0.1134195),
    vec2(0.5613756, -0.4556728),
    vec2(0.7622108, 0.4088926),
    vec2(-0.01734736, -0.7944852),
    vec2(-0.8065997, -0.4794133),
    vec2(0.4379586, 0.8250926),
    vec2(0.3348362, -0.9189008),
    vec2(-0.3919142, -0.9179187),
    vec2(0.02141847, 0.9828521),
    vec2(-0.6499051, 0.5785806)
);

//float rand(vec2 co){
//    return fract(sin(dot(co.xy ,vec2(12.9898,78.233))) * 43758.5453);
//}

float rand(vec4 seed4) {
    float dot_product = dot(seed4, vec4(12.9898, 78.233, 45.164, 94.673));
    return fract(sin(dot_product) * 43758.5453);
}

vec4 linearTest(vec4 outputColor) {
       float PI = 3.14159265358979323846264;
       vec4 effectColor = vec4(0.9);
	   float speedDivisor = 4.0;
	   float pulseSpeedDivisor = 0.8;
	   float pulseWidthDivisor = 3.0;

	   float intensity = sin(mod (((f_vertexPosition_objectspace.z / pulseWidthDivisor) + (animateSecondsOffset / pulseSpeedDivisor)), PI));

       float proximity = mod(f_vertexPosition_objectspace.z + (animateSecondsOffset / speedDivisor), 1);
       if (proximity < 0.08) {
         outputColor = mix(effectColor,outputColor,clamp(proximity * 7.0,intensity,1));
       }
	return outputColor;
}

vec4 spiralTest(vec4 outputColor) {
  float time = animateSecondsOffset;
  vec2 resolution = vec2(2,2);
  vec2 aspect = vec2(2,2);
  vec4 effectColor = vec4(0.2);
  vec2 currentLocation = f_vertexPosition_objectspace.xy;
   
  vec2 position =  currentLocation / resolution.xy * aspect.xy;
  float angle = 0.0 ;
  float radius = length(position) ;
  if (position.x != 0.0 && position.y != 0.0){
    angle = degrees(atan(position.y,position.x)) ;
  }
  float amod = mod(angle+30.0*time-120.0*log(radius), 30.0) ;
  if (amod<15.0){
    outputColor += effectColor * clamp(log(radius), 0.0, 1.0) * clamp (amod / 30.0, 0, 1);
  } 
  return outputColor;
}

vec4 gridTest(vec4 outputColor) {
       float PI = 3.14159265358979323846264;
       vec4 effectColor = vec4(0.9);
       vec3 vp = f_vertexPosition_objectspace;
	   float speedDivisor = 2.0;
	   float pulseSpeedDivisor = 1.0;

	   float intensity = sin(mod ((animateSecondsOffset / pulseSpeedDivisor), PI));

       float a_prox = mod(vp.x + vp.y + (animateSecondsOffset / speedDivisor), 0.7);
	   float b_prox = mod(vp.z + vp.y + (animateSecondsOffset / speedDivisor), 0.7);

       if (a_prox < 0.07) {
         outputColor = mix(effectColor,outputColor,clamp(a_prox * 7.0,intensity,1));
       }
       
       if (b_prox < 0.07) {
         outputColor = mix(effectColor,outputColor,clamp(b_prox * 7.0,intensity,1));
       }
	   

	return outputColor;
}

float simpleLighting() {
	vec3 lightDir = gl_LightSource[0].position.xyz;
    float lightDotProd = dot(f_vertexNormal, lightDir);
    bool lightIsInFront =  lightDotProd < 0.05;
    float litFactor = 1.0;

	if (lightIsInFront) {
		litFactor = 1;
	} else {
		litFactor = 0;
	}
	return litFactor;

}

bool shadowMapTest(vec2 uv, float distanceToTexel, float depthOffset)
{
    vec4 shadowMapTexel = texture2D(shadowMapTexture, uv);
    float nearestOccluder = shadowMapTexel.x;               
    if (nearestOccluder < depthOffset
     || nearestOccluder < (distanceToTexel - depthOffset)) {
        return true;
    } else {
        return false;
    }
}

float shadowMapLighting(out vec4 debugOutputColor)  {  

	vec3 lightDir = gl_LightSource[0].position.xyz;
    float lightDotProd = dot(f_vertexNormal, lightDir);
    bool lightIsInFront =  lightDotProd < 0.05;
    float litFactor = 1.0;

    if (lightIsInFront) {   
        float cosTheta = clamp(lightDotProd, 0, 1);
        float bias = 0.005 * tan(acos(cosTheta));
        float depthOffset = clamp(bias, 0, 0.01);
        //float depthOffset = 0.005;

        int smapIndexMask = 0;
        if (numShadowMaps == 1) {
             // simple shadowmap
            smapIndexMask = 1;
        } else {
            // PSSM
            // TODO: blend between cascades by setting multiple indeces in this mask?
            // http://msdn.microsoft.com/en-us/library/windows/desktop/ee416307%28v=vs.85%29.aspx
            float viewZ = -f_VV.z;
            for (int i = 0; i < numShadowMaps; ++i) {
                if (viewZ < shadowMapViewSplits[i]) {
                    smapIndexMask = 1 << i;
                    break;
                }
            }
        }

        debugOutputColor = vec4(1.0, 1.0, 1.0, 1.0); // failsafe debugging
        for (int i = 0; i < numShadowMaps; ++i) {
            if ((smapIndexMask & (1 << i)) != 0) {
				if      (i == 0) { debugOutputColor = vec4(1.0, 0.0, 0.0, 1.0); }
                else if (i == 1) { debugOutputColor = vec4(0.0, 1.0, 0.0, 1.0); }
                else if (i == 2) { debugOutputColor = vec4(0.0, 0.0, 1.0, 1.0); }
                else             { debugOutputColor = vec4(1.0, 1.0, 0.0, 1.0); }
                
                vec4 coord = f_shadowMapCoords[i];
                vec2 uv = coord.xy;
                float distanceToTexel = clamp(coord.z, 0.0, 1.0);

                if (poissonSamplingEnabled) {
                    vec3 seed3 = floor(f_vertexPosition_objectspace.xyz * 1000.0);
                    float litReductionPerSample = maxLitReductionByShade
                                                / float(numPoissonSamples);
                    for (int p = 0; p < numPoissonSamples; ++p) {
                        int pIndex = int(16.0*rand(vec4(seed3, p)))%16;
                        vec2 uvSample = uv + poissonDisk[pIndex] / 700.0 / poissonScale[i];
                        if (shadowMapTest(uvSample, distanceToTexel, depthOffset)) {
                            litFactor -= litReductionPerSample;
                        }
                    }
                } else { // no Poisson sampling
                    if (shadowMapTest(uv, distanceToTexel, depthOffset)) {
                        litFactor = 1.0 - maxLitReductionByShade;
                    }
                }
                debugOutputColor *= litFactor;               
				return litFactor;
            } 
        }
    } else {
		// surface away from the light
	    debugOutputColor = vec4(0.5, 0.0, 0.5, 1.0);  
		return 0f;
    }
	
}

vec4 shadowMapTestLighting(vec4 outputColor) {
    vec4 shadowMapDebugColor;
    float litFactor = shadowMapLighting(shadowMapDebugColor);
	return shadowMapDebugColor;
}

// http://www.clockworkcoders.com/oglsl/tutorial5.htm
// http://www.ozone3d.net/tutorials/bump_mapping_p4.php
vec4 BlinnPhongLighting(vec4 outputColor) {
		// eye space lighting

		// lighting strength
		vec4 ambientStrength = gl_FrontMaterial.ambient;
		vec4 diffuseStrength = gl_FrontMaterial.diffuse;
		vec4 specularStrength = gl_FrontMaterial.specular;
		vec4 glowStrength = gl_FrontMaterial.emission;
		float matShininess = gl_FrontMaterial.shininess;

		// specularStrength = vec4(0.7,0.4,0.4,0.0);  // test red

		// load texels...
		vec4 ambientColor = (ambiTexEnabled == 1) ? texture2D (ambiTex, gl_TexCoord[0].st) : vec4(1);
		vec4 diffuseColor = (diffTexEnabled == 1) ? texture2D (diffTex, gl_TexCoord[0].st) : vec4(0.1);
		vec4 glowColor = (ambiTexEnabled == 1) ? texture2D (ambiTex, gl_TexCoord[0].st) : vec4(1);
		vec4 specColor = (specTexEnabled == 1) ? texture2D (specTex, gl_TexCoord[0].st) : vec4(0.1);
       #ifdef INSTANCE_DRAW
       ambientColor *= f_instanceColor;
       diffuseColor *= f_instanceColor;
       glowColor *= f_instanceColor;
       specColor *= f_instanceColor;
       #endif
	   
       // 1. ambient lighting term
	   //outputColor = ambientColor * ambientStrength * vec4(0.6);
       outputColor = ambientColor * ambientStrength * vec4(1);

       // 2. glow/emissive lighting term
	   outputColor += glowColor * glowStrength;

       vec4 shadowMapDebugColor;           
       if (lighted != 0 && directionalLightIndex != -1) {
           float litFactor = receivesShadow != 0 ? shadowMapLighting(shadowMapDebugColor)
                                                 : 1.0;

            vec3 lightDir = gl_LightSource[directionalLightIndex].position.xyz;
            float lightDotProd = dot(f_vertexNormal, lightDir);
            bool lightIsInFront =  lightDotProd < 0.0;

            if (lightIsInFront) {
                // 3. diffuse reflection lighting term
                float diffuseIllumination = clamp(-lightDotProd, 0, 1);
                // boost the diffuse color by the glowmap .. poor mans bloom
                // float glowFactor = length(glowStrength.xyz) * 0.2; 
                // outputColor += litFactor * diffuseColor * diffuseStrength * max(diffuseIllumination, glowFactor);
                outputColor += litFactor * diffuseColor * diffuseStrength * diffuseIllumination * vec4(1.5);

                // add the specular highlight
                // 4. specular reflection lighting term
                vec3 R = reflect(normalize(gl_LightSource[0].position.xyz), normalize(f_vertexNormal));
                float shininess = pow (max (dot(R, normalize(f_eyeVec)), 0.0), matShininess);
                outputColor += litFactor * specColor * specularStrength * shininess; 
            }
       }
	   return outputColor;
}

// TODO: NEEDS MAINTENANCE AND CODE CONSOLIDATION (if possible)
vec4 BumpMapBlinnPhongLighting(vec4 outputColor) {
	   // tangent space shading with bump map...	
	      
		vec3 lightDir = gl_LightSource[0].position.xyz;
        float lightDotProd = dot(f_vertexNormal, lightDir);
        bool lightIsInFront =  lightDotProd < 0.005;

		vec4 shadowMapDebugColor;
		float litFactor = shadowMapLighting(shadowMapDebugColor);

	   	// lighting strength
	    vec4 ambientStrength = gl_FrontMaterial.ambient;
	    vec4 diffuseStrength = gl_FrontMaterial.diffuse;
	    vec4 specularStrength = gl_FrontMaterial.specular;
	    // specularStrength = vec4(0.7,0.4,0.4,0.0);  // test red
		float matShininess = 1.0; // gl_FrontMaterial.shininess;

		// load texels...
		vec4 ambientColor = (ambiTexEnabled == 1) ? texture2D (ambiTex, gl_TexCoord[0].st) : vec4(0);
		vec4 diffuseColor = (diffTexEnabled == 1) ? texture2D (diffTex, gl_TexCoord[0].st) : vec4(0);

		vec4 glowColor = (ambiTexEnabled == 1) ? texture2D (ambiTex, gl_TexCoord[0].st) : vec4(0);
		vec4 specColor = (specTexEnabled == 1) ? texture2D (specTex, gl_TexCoord[0].st) : vec4(0);
       #ifdef INSTANCE_DRAW
       ambientColor *= f_instanceColor;
       diffuseColor *= f_instanceColor;
       #endif
        
	   // lookup normal from normal map, move from [0,1] to  [-1, 1] range, normalize
       vec3 bump_normal = normalize( texture2D (bumpTex, gl_TexCoord[0].st).rgb * 2.0 - 1.0);
	   float distSqr = dot(surfaceLightVector,surfaceLightVector);
	   vec3 lVec = surfaceLightVector * inversesqrt(distSqr);

       // ambient ...
	   outputColor = ambientColor * ambientStrength;
	   outputColor += glowColor * gl_FrontMaterial.emission;
	          
       // diffuse...       
       float diffuseIllumination = clamp(dot(bump_normal,surfaceLightVector), 0,1);
       float glowFactor = length(gl_FrontMaterial.emission.xyz) * 0.2;
       outputColor += litFactor * diffuseColor * max(diffuseIllumination, glowFactor);

	   if (lighted != 0 && dot(bump_normal, surfaceLightVector) > 0.0) {   // if light is front of the surface
          // specular...
          vec3 R = reflect(-lVec,bump_normal);
          float shininess = pow (clamp (dot(R, normalize(surfaceViewVector)), 0, 1), matShininess);
          outputColor += specColor * litFactor * specularStrength * shininess;
       }
	   return outputColor;
}

void main()
{
	vec4 outputColor = vec4(0.0);

	vec3 lightPosition = surfaceLightVector;
    
    if (lightingMode == 0) {
        outputColor = BlinnPhongLighting(outputColor);        
    } else if (lightingMode == 1) {
        outputColor = BumpMapBlinnPhongLighting(outputColor);       
    } else { // lightingMode == 2
        outputColor = shadowMapTestLighting(outputColor);
    }

    // ---- object space shader effect tests ----
    // outputColor = linearTest(outputColor);
    // outputColor = spiralTest(outputColor);
    // outputColor = gridTest(outputColor);

	// single-pass wireframe calculation
	// .. compute distance from fragment to closest edge
	if (showWireframes == 1) { 
		float edgeWidth = 1.5; // in screenspace pixels
		float nearD = min(min(f_dist[0],f_dist[1]),f_dist[2]);
        float curIntensity = max(max(outputColor.r,outputColor.g),outputColor.b);
		float edgeIntensity = exp2((-1 / edgeWidth)*nearD*nearD * 2);		
        vec4 edgeColor = vec4(vec3( (curIntensity < 0.4) ? 0.6 : 0.3 ), 1.0);
        outputColor = mix(edgeColor,outputColor,1.0-edgeIntensity);
    }

	// finally, output the fragment color
    gl_FragColor = outputColor;
}			

	
