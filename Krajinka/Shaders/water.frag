#version 330 core
in vec3 vNormal;
in vec3 fragmentWorld;
in vec2 vUV;
out vec4 outColor;

uniform vec3 sunDirectionWorld;
uniform vec3 lightColor;
uniform float lightIntensity;
uniform sampler2D texWater;
uniform sampler2D surfaceTypeMap;
uniform vec2 terrainMaxXZ;
uniform vec3 cameraPosWorld;

void main()
{
    vec3 normal = normalize(vNormal);
    vec3 lightDir = normalize(sunDirectionWorld);
    vec3 viewDir = normalize(cameraPosWorld - fragmentWorld);

    float NdotL = max(0.0, dot(normal, lightDir));
    vec3 baseColor = texture(texWater, vUV).rgb;

    vec2 mapUv = fragmentWorld.xz / terrainMaxXZ;
    float typeValue = texture(surfaceTypeMap, mapUv).r * 255.0;
    float waterEdgeBlend = 1.0 - smoothstep(0.25, 1.25, typeValue);

    if (waterEdgeBlend <= 0.001)
    {
        discard;
    }

    vec3 ambient = vec3(0.3) * baseColor;
    vec3 diffuse = lightColor * lightIntensity * NdotL * baseColor;

    vec3 reflectedLightDir = reflect(-lightDir, normal);
    float specularStrength = pow(max(dot(viewDir, reflectedLightDir), 0.0), 64.0);
    vec3 specular = lightColor * lightIntensity * specularStrength * 0.8;

    outColor = vec4(ambient + diffuse + specular, 0.45 * waterEdgeBlend);
}
