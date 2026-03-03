#version 330 core
layout (location = 0) in vec3 vPos;

uniform mat4 uView;
uniform mat4 uProjection;

out vec3 fTexCoords;

void main()
{
    fTexCoords = vPos;
    gl_Position = uProjection * uView * vec4(vPos, 1.0);
}