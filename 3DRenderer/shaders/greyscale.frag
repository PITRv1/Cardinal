#version 330 core
in vec2 fUv;

uniform sampler2D uScreenTexture;

out vec4 FragColor;

void main()
{
    vec4 color = texture(uScreenTexture, fUv);
    float grey = dot(color.rgb, vec3(0.299, 0.587, 0.114));
    FragColor = vec4(grey, grey, grey, 1.0);
}