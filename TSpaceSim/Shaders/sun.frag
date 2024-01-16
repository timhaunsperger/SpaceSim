#version 460 core

layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 FragPos;

in vec3 fragPos;

void main()
{
    FragColor = vec4(1, 1, 0.95, 1);
    FragPos = vec4(fragPos,1);
}