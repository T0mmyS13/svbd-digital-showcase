using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;

namespace Krajinka;

/// <summary>
/// Nastavení parametrů textury (zabalení, filtrování).
/// </summary>
internal class TextureSetting : Dictionary<TextureParameterName, int>
{
    /// <summary>
    /// Výchozí nastavení textury: opakování s lineárním filtrováním.
    /// </summary>
    public static readonly TextureSetting Default = new TextureSetting()
    {
        { TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat },
        { TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat },
        { TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear },
        { TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear },
    };
}

/// <summary>
/// Představuje texturu načtenou z PNG/JPG souboru.
/// </summary>
internal class Texture
{
    /// <summary>
    /// ID textury v OpenGL.
    /// </summary>
    private int textureID;

    /// <summary>
    /// Indikuje, zda už byla textura uvolněna.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Vytvoří texturu ze souboru.
    /// </summary>
    /// <param name="filename">Cesta k souboru textury.</param>
    /// <param name="settings">Nastavení parametrů textury (zabalení, filtrování).</param>
    public Texture(string filename, TextureSetting settings)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Cesta k textuře nesmí být prázdná.", nameof(filename));
        }

        string fullPath = Path.Combine(AppContext.BaseDirectory, filename);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Soubor textury nebyl nalezen.", fullPath);
        }

        using Stream stream = File.OpenRead(fullPath);
        using SKBitmap? bitmap = SKBitmap.Decode(stream);

        if (bitmap == null)
        {
            throw new InvalidOperationException("Soubor textury se nepodařilo načíst.");
        }

        byte[] imageData = new byte[bitmap.Width * bitmap.Height * 4];
        int dataIndex = 0;

        for (int y = bitmap.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                imageData[dataIndex++] = pixel.Red;
                imageData[dataIndex++] = pixel.Green;
                imageData[dataIndex++] = pixel.Blue;
                imageData[dataIndex++] = pixel.Alpha;
            }
        }

        textureID = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, textureID);
        GL.TexImage2D(
            TextureTarget.Texture2D, 
            0, 
            PixelInternalFormat.Rgba, 
            bitmap.Width,
            bitmap.Height,
            0, 
            PixelFormat.Rgba,
            PixelType.UnsignedByte, 
            imageData);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        foreach (KeyValuePair<TextureParameterName, int> kvp in settings)
        {
            GL.TexParameter(TextureTarget.Texture2D, kvp.Key, kvp.Value);
        }

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    /// <summary>
    /// Připojí texturu na danou texturapu jednotku.
    /// </summary>
    /// <param name="unit">Číslo jednotky (0 = Texture0, 1 = Texture1, atd.).</param>
    public void Bind(int unit)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2D, textureID);
    }

    /// <summary>
    /// Uvolní OpenGL texturu.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        GL.DeleteTexture(textureID);
        disposed = true;
    }
}
