using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Jednoduchá reprezentace světla.
/// </summary>
internal class Light : SceneObject
{
    /// <summary>
    /// Barva světla.
    /// </summary>
    public Vector3 Color;

    /// <summary>
    /// Intenzita světla.
    /// </summary>
    public float Intensity;

    /// <summary>
    /// Vrátí pozici světla ve světě.
    /// </summary>
    /// <returns>Pozice světla jako Vector4.</returns>
    public Vector4 GetPositionWorld()
    {
        return new Vector4(GetPosition(), 1.0f);
    }

    /// <summary>
    /// Vytvoří bodové světlo.
    /// </summary>
    /// <param name="position">Pozice světla.</param>
    /// <param name="color">Barva světla.</param>
    /// <param name="intensity">Intenzita světla.</param>
    /// <returns>Nové bodové světlo.</returns>
    public static Light CreatePoint(Vector3 position, Vector3 color, float intensity)
    {
        Light light = new Light();
        light.SetPosition(position);
        light.Color = color;
        light.Intensity = intensity;
        return light;
    }
}
