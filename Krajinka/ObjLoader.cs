using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Načítá OBJ soubor a vrací vrcholy a trojúhelníky.
/// </summary>
public struct ObjMeshData
{
    /// <summary>
    /// Vrcholy části modelu.
    /// </summary>
    public VertexNormalTexCoord[] Vertices;

    /// <summary>
    /// Trojúhelníky části modelu.
    /// </summary>
    public Triangle[] Triangles;

    /// <summary>
    /// Absolutní cesta k textuře části modelu.
    /// </summary>
    public string TexturePath;

    /// <summary>
    /// Vytvoří data části modelu.
    /// </summary>
    public ObjMeshData(VertexNormalTexCoord[] vertices, Triangle[] triangles, string texturePath)
    {
        Vertices = vertices;
        Triangles = triangles;
        TexturePath = texturePath;
    }
}

/// <summary>
/// Načítá OBJ soubor a vrací části modelu podle materiálu.
/// </summary>
public static class ObjLoader
{
    /// <summary>
    /// Načte OBJ soubor řádek po řádku.
    /// </summary>
    /// <param name="filename">Cesta k OBJ souboru.</param>
    /// <returns>Vrací části modelu rozdělené podle textury.</returns>
    public static ObjMeshData[] Load(string filename)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("OBJ soubor nebyl nalezen.", filename);
        }

        string[] lines = File.ReadAllLines(filename);

        List<Vector3> positions = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> texCoords = new List<Vector2>();
        Dictionary<string, List<VertexNormalTexCoord>> verticesByMaterial = new Dictionary<string, List<VertexNormalTexCoord>>();
        Dictionary<string, List<Triangle>> trianglesByMaterial = new Dictionary<string, List<Triangle>>();

        string currentMaterial = string.Empty;
        Dictionary<string, string> materialTextureMap = new Dictionary<string, string>();

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            if (line.StartsWith("v "))
            {
                ParseVertex(line, positions);
                continue;
            }

            if (line.StartsWith("vn "))
            {
                ParseNormal(line, normals);
                continue;
            }

            if (line.StartsWith("vt "))
            {
                ParseTexCoord(line, texCoords);
                continue;
            }

            if (line.StartsWith("f "))
            {
                if (currentMaterial.Length == 0)
                {
                    continue;
                }

                if (!verticesByMaterial.ContainsKey(currentMaterial))
                {
                    verticesByMaterial[currentMaterial] = new List<VertexNormalTexCoord>();
                    trianglesByMaterial[currentMaterial] = new List<Triangle>();
                }

                ParseFace(line, positions, normals, texCoords, verticesByMaterial[currentMaterial], trianglesByMaterial[currentMaterial]);
                continue;
            }

            if (line.StartsWith("mtllib "))
            {
                string mtlFileName = line.Substring(7).Trim();
                string objDirectory = Path.GetDirectoryName(filename) ?? string.Empty;
                string mtlPath = Path.Combine(objDirectory, mtlFileName);
                materialTextureMap = LoadMaterialTextureMap(mtlPath);
                continue;
            }

            if (line.StartsWith("usemtl "))
            {
                currentMaterial = line.Substring(7).Trim();
            }
        }

        List<ObjMeshData> parts = new List<ObjMeshData>();

        foreach (KeyValuePair<string, List<VertexNormalTexCoord>> part in verticesByMaterial)
        {
            List<Triangle> partTriangles = trianglesByMaterial[part.Key];

            if (partTriangles.Count == 0)
            {
                continue;
            }

            string texturePath = string.Empty;
            if (materialTextureMap.TryGetValue(part.Key, out string? textureFileName) && !string.IsNullOrWhiteSpace(textureFileName))
            {
                string objDirectory = Path.GetDirectoryName(filename) ?? string.Empty;
                texturePath = Path.GetFullPath(Path.Combine(objDirectory, textureFileName));
            }

            parts.Add(new ObjMeshData(part.Value.ToArray(), partTriangles.ToArray(), texturePath));
        }

        return parts.ToArray();
    }

    /// <summary>
    /// Načte vrchol z řádku v.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="positions">Seznam pozic vrcholů.</param>
    private static void ParseVertex(string line, List<Vector3> positions)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            return;

        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

        positions.Add(new Vector3(x, y, z));
    }

    /// <summary>
    /// Načte texturovou souřadnici z řádku vt.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="texCoords">Seznam UV souřadnic.</param>
    private static void ParseTexCoord(string line, List<Vector2> texCoords)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
            return;

        float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float v = float.Parse(parts[2], CultureInfo.InvariantCulture);
        texCoords.Add(new Vector2(u, v));
    }

    /// <summary>
    /// Načte normálu z řádku vn.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="normals">Seznam normál.</param>
    private static void ParseNormal(string line, List<Vector3> normals)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            return;

        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

        normals.Add(new Vector3(x, y, z));
    }

    /// <summary>
    /// Načte trojúhelník z řádku f.
    /// </summary>
    /// <param name="line">Řádek OBJ souboru.</param>
    /// <param name="positions">Seznam pozic vrcholů.</param>
    /// <param name="normals">Seznam normál.</param>
    /// <param name="texCoords">Seznam UV souřadnic.</param>
    /// <param name="vertices">Výstupní seznam vrcholů pro OpenGL.</param>
    /// <param name="triangles">Seznam trojúhelníků.</param>
    private static void ParseFace(
        string line,
        List<Vector3> positions,
        List<Vector3> normals,
        List<Vector2> texCoords,
        List<VertexNormalTexCoord> vertices,
        List<Triangle> triangles)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 4)
            return;

        int i0 = CreateVertexIndex(parts[1], positions, normals, texCoords, vertices);
        if (i0 < 0)
            return;

        for (int i = 2; i + 1 < parts.Length; i++)
        {
            int i1 = CreateVertexIndex(parts[i], positions, normals, texCoords, vertices);
            int i2 = CreateVertexIndex(parts[i + 1], positions, normals, texCoords, vertices);

            if (i1 < 0 || i2 < 0)
                continue;

            triangles.Add(new Triangle(i0, i1, i2));
        }
    }

    /// <summary>
    /// Vrátí index vrcholu ve výstupním seznamu, případně vrchol vytvoří.
    /// </summary>
    private static int CreateVertexIndex(
        string token,
        List<Vector3> positions,
        List<Vector3> normals,
        List<Vector2> texCoords,
        List<VertexNormalTexCoord> vertices)
    {
        ParseFaceVertex(token, out int rawPositionIndex, out int rawTexCoordIndex, out int rawNormalIndex);

        // OBJ indexy začínají od 1, záporné indexují od konce
        int positionIndex = rawPositionIndex > 0 ? rawPositionIndex - 1 : -1;
        int texCoordIndex = rawTexCoordIndex > 0 ? rawTexCoordIndex - 1 : -1;
        int normalIndex = rawNormalIndex > 0 ? rawNormalIndex - 1 : -1;

        if (positionIndex < 0 || positionIndex >= positions.Count)
            return -1;

        Vector3 position = positions[positionIndex];
        Vector3 normal = Vector3.UnitY;
        Vector2 uv = Vector2.Zero;

        if (normalIndex >= 0 && normalIndex < normals.Count)
            normal = normals[normalIndex];

        if (texCoordIndex >= 0 && texCoordIndex < texCoords.Count)
            uv = texCoords[texCoordIndex];

        int newIndex = vertices.Count;
        vertices.Add(new VertexNormalTexCoord(position, normal, uv));

        return newIndex;
    }

    /// <summary>
    /// Rozparsuje token vrcholu ve tvaru v/vt/vn.
    /// </summary>
    /// <param name="token">Token vrcholu z řádku f.</param>
    /// <param name="vertexIndex">Výstupní index vrcholu.</param>
    /// <param name="texCoordIndex">Výstupní index tex coord.</param>
    /// <param name="normalIndex">Výstupní index normály.</param>
    private static void ParseFaceVertex(string token, out int vertexIndex, out int texCoordIndex, out int normalIndex)
    {
        string[] values = token.Split('/');

        vertexIndex = 0;
        texCoordIndex = 0;
        normalIndex = 0;

        if (values.Length > 0)
            int.TryParse(values[0], out vertexIndex);

        if (values.Length > 1)
            int.TryParse(values[1], out texCoordIndex);

        if (values.Length > 2)
            int.TryParse(values[2], out normalIndex);
    }

    /// <summary>
    /// Načte mapování materiál -> mapa_Kd z MTL souboru.
    /// </summary>
    /// <param name="mtlPath">Cesta k MTL souboru.</param>
    /// <returns>Slovník mapování materiálu na texturu.</returns>
    private static Dictionary<string, string> LoadMaterialTextureMap(string mtlPath)
    {
        Dictionary<string, string> materialTextureMap = new Dictionary<string, string>();

        if (!File.Exists(mtlPath))
            return materialTextureMap;

        string currentMaterial = string.Empty;

        foreach (string rawLine in File.ReadAllLines(mtlPath))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#"))
                continue;

            if (line.StartsWith("newmtl "))
            {
                currentMaterial = line.Substring(7).Trim();
                continue;
            }

            if (line.StartsWith("map_Kd ") && currentMaterial.Length > 0)
            {
                string textureFileName = line.Substring(7).Trim();
                if (textureFileName.Length > 0)
                    materialTextureMap[currentMaterial] = textureFileName;
            }
        }

        return materialTextureMap;
    }
}