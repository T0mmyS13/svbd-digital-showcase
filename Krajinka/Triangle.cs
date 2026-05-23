using System.Runtime.InteropServices;

namespace Krajinka;

/// <summary>
/// Trojúhelník indexů do vrcholového pole.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Triangle
{
    /// <summary>
    /// První index vrcholu.
    /// </summary>
    public int i0;

    /// <summary>
    /// Druhý index vrcholu.
    /// </summary>
    public int i1;

    /// <summary>
    /// Třetí index vrcholu.
    /// </summary>
    public int i2;

    /// <summary>
    /// Vytvoří trojúhelník ze tří indexů.
    /// </summary>
    /// <param name="index0">První index.</param>
    /// <param name="index1">Druhý index.</param>
    /// <param name="index2">Třetí index.</param>
    public Triangle(int index0, int index1, int index2)
    {
        i0 = index0;
        i1 = index1;
        i2 = index2;
    }
}