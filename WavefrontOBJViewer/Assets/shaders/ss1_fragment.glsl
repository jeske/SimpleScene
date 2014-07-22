#version 120
 
// varying vec3 lightDir,normal;

uniform sampler2D diffTex;
uniform sampler2D specTex;
uniform sampler2D ambiTex;
uniform sampler2D bumpTex;
 
void main()
{
    vec3 lightDir = gl_LightSource[0].position;
    vec3 normal = gl_Normal;
    
    vec3 ct,cf;
    vec4 texel;
    float intensity,at,af;
    intensity = max(dot(lightDir,normalize(normal)),0.0);
 
    cf = intensity * (gl_FrontMaterial.diffuse).rgb +
                      gl_FrontMaterial.ambient.rgb;
    af = gl_FrontMaterial.diffuse.a;
    
    // lookup both textures
    texel = texture2D(diffTex,gl_TexCoord[0].st)+
            texture2D(specTex,gl_TexCoord[0].st); 
            
            
    ct = texel.rgb;
    at = texel.a;
    gl_FragColor = vec4(ct * cf, at * af);
 
}	