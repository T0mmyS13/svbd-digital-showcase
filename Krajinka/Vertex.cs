using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Vrchol s pozicí, normálou a UV souřadnicí.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexNormalTexCoord
{
    /// <summary>
    /// Pozice vrcholu.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Normála vrcholu.
    /// </summary>
    public Vector3 Normal;

    /// <summary>
    /// UV souřadnice vrcholu.
    /// </summary>
    public Vector2 UV;

    /// <summary>
    /// Vytvoří vrchol s pozicí, normálou a UV souřadnicí.
    /// </summary>
    /// <param name="position">Pozice vrcholu.</param>
    /// <param name="normal">Normála vrcholu.</param>
    /// <param name="uv">UV souřadnice.</param>
    public VertexNormalTexCoord(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Normal = normal;
        UV = uv;
    }

    /// <summary>
    /// Vrátí velikost struktury v bajtech.
    /// </summary>
    /// <returns>Velikost v bajtech.</returns>
    public static int GetSizeInBytes()
    {
        return Marshal.SizeOf<VertexNormalTexCoord>();
    }
}
