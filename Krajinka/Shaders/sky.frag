#version 330 core
in vec3 skyDirectionWorld;
out vec4 outColor;

uniform vec3 sunDirectionWorld;
uniform float lightIntensity;
uniform samplerCube skybox;

const float DayTransitionStartY = -0.12;
const float DayTransitionEndY = 0.16;

const float SunDiscStart = 0.9992;
const float SunDiscEnd = 0.99985;
const float SunGlowStart = 0.97;
const float SunGlowEnd = 1.0;

const float SunGlowStrength = 0.35;
const float SunDiscStrength = 2.1;
const float SunColorHeightScale = 2.0;

const float SunStrengthScale = 0.85;
const float SunStrengthBase = 0.15;

const float CubeBlendStart = -0.05;
const float CubeBlendEnd = 0.20;

void main()
{
    vec3 direction = normalize(skyDirectionWorld);
    vec3 sunDirection = normalize(sunDirectionWorld);

    float vertical = clamp((direction.y * 0.5) + 0.5, 0.0, 1.0);
    float dayAmount = smoothstep(DayTransitionStartY, DayTransitionEndY, sunDirection.y);

    vec3 dayHorizon = vec3(0.78, 0.88, 1.0);
    vec3 dayTop = vec3(0.20, 0.45, 0.85);
    vec3 nightHorizon = vec3(0.06, 0.08, 0.16);
    vec3 nightTop = vec3(0.01, 0.02, 0.07);

    vec3 daySky = mix(dayHorizon, dayTop, vertical);
    vec3 nightSky = mix(nightHorizon, nightTop, vertical);
    vec3 skyColor = mix(nightSky, daySky, dayAmount);

    float sunDot = dot(direction, sunDirection);
    float sunDisc = smoothstep(SunDiscStart, SunDiscEnd, sunDot);
    float sunGlow = smoothstep(SunGlowStart, SunGlowEnd, sunDot) * SunGlowStrength;

    vec3 sunColor = mix(vec3(1.0, 0.75, 0.35), vec3(1.0, 0.98, 0.9), clamp(sunDirection.y * SunColorHeightScale, 0.0, 1.0));
    float sunStrength = (lightIntensity * SunStrengthScale) + SunStrengthBase;

    skyColor += sunColor * ((sunDisc * SunDiscStrength) + sunGlow) * sunStrength;

    vec3 cubeColor = texture(skybox, direction).rgb;
    cubeColor *= mix(0.12, 1.0, dayAmount);

    float blendFactor = smoothstep(CubeBlendStart, CubeBlendEnd, direction.y);
    skyColor = mix(cubeColor, skyColor, blendFactor);

    outColor = vec4(skyColor, 1.0);
}