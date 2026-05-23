#version 330 core

out vec3 skyDirectionWorld;

uniform mat4 invProjection;
uniform mat4 invView;

void main()
{
    vec2 position;
    if (gl_VertexID == 0) {
        position = vec2(-1.0, -1.0);
    } else if (gl_VertexID == 1) {
        position = vec2(3.0, -1.0);
    } else {
        position = vec2(-1.0, 3.0);
    }

    vec4 clip = vec4(position, 0.0, 1.0);
    vec4 viewDir = invProjection * clip;
    viewDir.w = 0.0;
    skyDirectionWorld = (invView * viewDir).xyz;

    gl_Position = vec4(position, 1.0, 1.0);
}