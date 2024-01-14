#version 460 core
layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 FragPos;
layout (location = 2) out vec4 Normal;

in vec3 normal;
in vec3 fragPos;

uniform sampler2D tex;
uniform vec3 viewPos;
uniform vec3 center;
uniform float oceanRadius;

void main()
{
    float elevation = length(fragPos - center);
    vec3 fragDir = (fragPos - center) / elevation;
    
    vec3 viewDirection = normalize(fragPos - viewPos);
    float steepness = (1 / dot(normal, fragDir));
    
    if(steepness > 1.5){
        FragColor = vec4(0.5, 0.5, 0.5, 1);
    } else if(elevation < oceanRadius * 1.01 && steepness < 1.02){
        FragColor = vec4(0.7, 0.6, 0.1, 1);
    } else if(steepness < 1.01){
        FragColor = vec4(0.1, 1, 0.1, 1);
    } else {
        FragColor = vec4(0, 0.7, 0.1, 1);
    }
    FragPos = vec4(fragPos,1);
    Normal = vec4(normal, 1);
}