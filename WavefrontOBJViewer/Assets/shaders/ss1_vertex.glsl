#version 120
 
 void main(void) {
    // normal MVP transform       
    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;       
    
    vec3 N = normalize(gl_NormalMatrix * gl_Normal);       
    vec4 V = gl_ModelViewMatrix * gl_Vertex;       
    vec3 L = normalize(gl_LightSource[0].position - V.xyz);       
    
    // output the diffuse color       
    float NdotL = dot(N, L);       
    gl_FrontColor = gl_Color * vec4(max(0.0, NdotL)); 
}	