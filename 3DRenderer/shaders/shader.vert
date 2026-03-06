#version 300 es
precision mediump float;
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec3 vTangent;
layout (location = 3) in vec3 vBitangent;
layout (location = 4) in vec2 vUv;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 fNormal;
out vec2 fUv;
out mat3 fTBN;
out vec3 fWorldPos;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
    fUv = vUv;
    fWorldPos = vec3(uModel * vec4(vPos, 1.0));

    mat3 normalMatrix = mat3(transpose(inverse(uModel)));
    vec3 T = normalize(normalMatrix * vTangent);
    vec3 N = normalize(normalMatrix * vNormal);
    T = normalize(T - dot(T, N) * N); // re-orthogonalize
    vec3 B = cross(N, T);

    fTBN = mat3(T, B, N);
    fNormal = N;
}