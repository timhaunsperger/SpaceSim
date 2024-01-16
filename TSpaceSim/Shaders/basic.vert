#version 460 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;

out vec3 fragPos;

uniform mat4 view;
uniform mat4 projection;


void main()
{
    fragPos = aPosition;
    gl_Position = vec4(aPosition, 1.0) * view * projection;
}