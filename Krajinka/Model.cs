using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// Obecný objekt scény načtený z OBJ souboru.
/// </summary>
internal class Model : SceneObject
{
    /// <summary>
    /// Části modelu rozdělené podle textur.
    /// </summary>
    private readonly List<ModelPart> parts = new List<ModelPart>();

    /// <summary>
    /// Jedna vykreslovací část modelu.
    /// </summary>
    private struct ModelPart
    {
        public int VAO;
        public int VBO;
        public int IBO;
        public int IndexCount;
        public Texture? Texture;
    }

    /// <summary>
    /// Indikuje, zda byl model už uvolněn.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Vytvoří objekt z OBJ souboru.
    /// </summary>
    /// <param name="objRelativePath">Relativní cesta k OBJ souboru.</param>
    /// <param name="hastexture">Povolí načtení částí modelu s map_Kd texturou.</param>
    public Model(string objRelativePath, bool hastexture = true)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, objRelativePath);
        ObjMeshData[] meshParts = ObjLoader.Load(fullPath);

        for (int i = 0; i < meshParts.Length; i++)
        {
            ObjMeshData meshPart = meshParts[i];
            Texture? texture = null;

            if (!string.IsNullOrWhiteSpace(meshPart.TexturePath))
            {
                texture = new Texture(meshPart.TexturePath, TextureSetting.Default);
            }
            else if (hastexture)
            {
                continue;
            }

            ModelPart part = CreateModelPart(meshPart.Vertices, meshPart.Triangles, texture);
            parts.Add(part);
        }

        if (parts.Count == 0)
        {
            throw new InvalidOperationException("Model neobsahuje žádnou vykreslitelnou část s map_Kd texturou.");
        }
    }

    /// <summary>
    /// Vytvoří OpenGL buffery pro model.
    /// </summary>
    /// <param name="vertices">Vrcholová data.</param>
    /// <param name="triangles">Indexová data trojúhelníků.</param>
    private ModelPart CreateModelPart(VertexNormalTexCoord[] vertices, Triangle[] triangles, Texture? texture)
    {
        ModelPart part = new ModelPart();
        part.Texture = texture;
        part.IndexCount = triangles.Length * 3;

        part.VAO = GL.GenVertexArray();
        GL.BindVertexArray(part.VAO);

        part.VBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, part.VBO);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * VertexNormalTexCoord.GetSizeInBytes(), vertices, BufferUsageHint.StaticDraw);

        part.IBO = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, part.IBO);
        GL.BufferData(BufferTarget.ElementArrayBuffer, triangles.Length * 3 * sizeof(int), triangles, BufferUsageHint.StaticDraw);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexNormalTexCoord.GetSizeInBytes(), IntPtr.Zero);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, VertexNormalTexCoord.GetSizeInBytes(), (IntPtr)Vector3.SizeInBytes);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, VertexNormalTexCoord.GetSizeInBytes(), 2 * (IntPtr)Vector3.SizeInBytes);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        return part;
    }

    /// <summary>
    /// Vykreslí model.
    /// </summary>
    public override void Draw()
    {
        for (int i = 0; i < parts.Count; i++)
        {
            ModelPart part = parts[i];
            if (part.Texture != null)
            {
                part.Texture.Bind(1);
            }

            GL.BindVertexArray(part.VAO);
            GL.DrawElements(PrimitiveType.Triangles, part.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
        }

        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Uvolní OpenGL prostředky modelu.
    /// </summary>
    public override void Dispose()
    {
        if (disposed)
        {
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            ModelPart part = parts[i];
            GL.DeleteBuffer(part.VBO);
            GL.DeleteBuffer(part.IBO);
            GL.DeleteVertexArray(part.VAO);
            if (part.Texture != null)
            {
                part.Texture.Dispose();
            }
        }

        parts.Clear();
        disposed = true;
    }
}
