#version 300 es
precision mediump float;
in vec2 fUv;

uniform sampler2D uScreenTexture;
uniform float uTime;

out vec4 FragColor;

void main()
{
    // Curvature
    vec2 uv = fUv * 2.0 - 1.0; // remap to -1 to 1
    vec2 offset = uv.yx / 6.0; // curvature strength
    uv = uv + uv * offset * offset;
    uv = uv * 0.5 + 0.5; // remap back to 0 to 1

    // If uvs are outside screen after curvature show black
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
        FragColor = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    vec4 color = texture(uScreenTexture, uv);

    // Scrolling scanlines
    float scanline = sin((uv.y + uTime * 0.1) * 800.0) * 0.04;
    color.rgb -= scanline;

    // Slight vignette to darken edges
    vec2 vigUv = fUv * (1.0 - fUv.yx);
    float vignette = pow(vigUv.x * vigUv.y * 15.0, 0.25);
    color.rgb *= vignette;

    FragColor = color;
}