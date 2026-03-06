#version 300 es
precision mediump float;
in vec3 fNormal;
in vec2 fUv;
in mat3 fTBN;
in vec3 fWorldPos;

uniform sampler2D uTexture0;
uniform sampler2D uNormalMap;
uniform int uHasNormalMap;
uniform vec3 uLightDir;
uniform float uAmbient;
uniform float uNormalStrength;

out vec4 FragColor;

void main()
{
    vec3 norm;
    if (uHasNormalMap == 1) {
    vec3 sampledNormal = texture(uNormalMap, fUv).rgb;
    sampledNormal = normalize(sampledNormal * 2.0 - 1.0);
    vec3 mappedNormal = normalize(fTBN * sampledNormal);
    norm = mix(fNormal, mappedNormal, uNormalStrength);
    } else {
        norm = normalize(fNormal);
    }

    vec3 lightDir = normalize(-uLightDir);
    float diff = max(dot(norm, lightDir), 0.0);
    float lighting = uAmbient + diff;

    vec4 texColor = texture(uTexture0, fUv);
    FragColor = vec4(texColor.rgb * lighting, texColor.a);
}