#version 330 core
in vec2 fUv;

uniform sampler2D uScreenTexture;
uniform vec2 uResolution;
uniform float uPixelSize;

out vec4 FragColor;

void main()
{
    vec2 pixelated = floor(fUv * uResolution / uPixelSize) * uPixelSize / uResolution;
    FragColor = texture(uScreenTexture, pixelated);
}