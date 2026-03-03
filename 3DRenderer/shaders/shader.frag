#version 330 core
in vec3 fNormal;
in vec2 fUv;

uniform sampler2D uTexture0;
uniform vec3 uLightDir;
uniform float uAmbient;

out vec4 FragColor;

void main()
{
    vec3 norm = normalize(fNormal);
    vec3 lightDir = normalize(-uLightDir); // negate because we want direction toward light

    float diff = max(dot(norm, lightDir), 0.0); // lambert calculation
    float lighting = uAmbient + diff;           // ambient + diffuse combined

    vec4 texColor = texture(uTexture0, fUv);
    FragColor = vec4(texColor.rgb * lighting, texColor.a);
}