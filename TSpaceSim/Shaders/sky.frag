#version 460 core
layout (location = 0) out vec4 FragColor;
layout (location = 1) out vec4 FragPos;

in vec3 fragPos;

void main()
{
    FragColor = vec4(0, 0, 0, 1);
    FragPos = vec4(fragPos,1);
}