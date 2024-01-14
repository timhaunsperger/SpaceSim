#version 460 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in float elevation;

out vec2 texCoord;
out vec3 normal;
out vec3 fragPos;
out float fragElevation;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    normal = aNormal;
    fragPos = aPosition;
    fragElevation = elevation;
    gl_Position = vec4(aPosition, 1.0) * view * projection;
}