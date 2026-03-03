#version 300 es
precision mediump float;
in vec2 fUv;

uniform sampler2D uScreenTexture;

out vec4 FragColor;

void main()
{
    FragColor = texture(uScreenTexture, fUv);
}