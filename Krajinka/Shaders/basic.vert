#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aUV;

out vec3 vNormal;
out vec3 fragmentWorld;
out vec2 vUV;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    vec4 worldPosition = model * vec4(aPos, 1.0);
    gl_Position = projection * view * worldPosition;

    fragmentWorld = worldPosition.xyz;
    vUV = aUV;
    vNormal = normalize(mat3(model) * aNormal);
}
