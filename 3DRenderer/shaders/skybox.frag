#version 300 es
precision mediump float;
in vec3 fTexCoords;

uniform samplerCube uSkybox;

out vec4 FragColor;

void main()
{
    FragColor = texture(uSkybox, fTexCoords);
    gl_FragDepth = 0.9999;
}