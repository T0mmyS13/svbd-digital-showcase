using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Krajinka;

/// <summary>
/// OpenGL shader program s cache uniform proměnných.
/// </summary>
public class Shader : IDisposable
{
    /// <summary>
    /// Identifikátor OpenGL programu.
    /// </summary>
    public int ProgramId;

    /// <summary>
    /// Cache umístění uniform proměnných.
    /// </summary>
    private readonly Dictionary<string, int> uniforms = new Dictionary<string, int>();

    /// <summary>
    /// Indikuje, zda už byl shader uvolněn.
    /// </summary>
    private bool disposed;

    /// <summary>
    /// Vytvoří shader program z vertex a fragment shaderu.
    /// </summary>
    /// <param name="vertexPath">Relativní cesta k vertex shaderu.</param>
    /// <param name="fragmentPath">Relativní cesta k fragment shaderu.</param>
    public Shader(string vertexPath, string fragmentPath)
    {
        int vertexShader = CompileShader(vertexPath, ShaderType.VertexShader);
        int fragmentShader = CompileShader(fragmentPath, ShaderType.FragmentShader);

        LinkShader(vertexShader, fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        LoadUniforms();
    }

    /// <summary>
    /// Aktivuje shader program.
    /// </summary>
    public void Use()
    {
        GL.UseProgram(ProgramId);
    }

    /// <summary>
    /// Nastaví uniform proměnnou v shaderu.
    /// </summary>
    /// <typeparam name="T">Datový typ uniformu.</typeparam>
    /// <param name="name">Název uniformu.</param>
    /// <param name="value">Hodnota uniformu.</param>
    public void SetUniform<T>(string name, T value)
    {
        int location = GetUniformLocation(name);
        if (location == -1)
        {
            return;
        }

        switch (value)
        {
            case int uniformInt:
                GL.Uniform1(location, uniformInt);
                break;
            case float uniformFloat:
                GL.Uniform1(location, uniformFloat);
                break;
            case Vector3 uniformVector3:
                GL.Uniform3(location, uniformVector3);
                break;
            case Vector2 uniformVector2:
                GL.Uniform2(location, uniformVector2);
                break;
            case Vector4 uniformVector4:
                GL.Uniform4(location, uniformVector4);
                break;
            case Matrix4 uniformMatrix4:
                GL.UniformMatrix4(location, false, ref uniformMatrix4);
                break;
            default:
                throw new NotSupportedException($"Uniform type {typeof(T)} is not supported.");
        }
    }

    /// <summary>
    /// Načte a zkompiluje shader daného typu.
    /// </summary>
    /// <param name="relativePath">Relativní cesta k souboru shaderu.</param>
    /// <param name="shaderType">Typ shaderu.</param>
    /// <returns>ID zkompilovaného shaderu.</returns>
    private int CompileShader(string relativePath, ShaderType shaderType)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        string source = File.ReadAllText(fullPath);

        int shader = GL.CreateShader(shaderType);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            string shaderLog = GL.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"Chyba kompilace shaderu '{relativePath}': {shaderLog}");
        }

        return shader;
    }

    /// <summary>
    /// Vytvoří a nalinkuje shader program.
    /// </summary>
    /// <param name="vertexShader">ID vertex shaderu.</param>
    /// <param name="fragmentShader">ID fragment shaderu.</param>
    private void LinkShader(int vertexShader, int fragmentShader)
    {
        ProgramId = GL.CreateProgram();
        GL.AttachShader(ProgramId, vertexShader);
        GL.AttachShader(ProgramId, fragmentShader);
        GL.LinkProgram(ProgramId);

        GL.GetProgram(ProgramId, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            string programLog = GL.GetProgramInfoLog(ProgramId);
            throw new InvalidOperationException($"Chyba linkování shader programu: {programLog}");
        }
    }

    /// <summary>
    /// Vrátí umístění uniform proměnné z cache.
    /// </summary>
    /// <param name="name">Název uniform proměnné.</param>
    /// <returns>Umístění uniform proměnné, nebo -1 pokud neexistuje.</returns>
    private int GetUniformLocation(string name)
    {
        if (uniforms.TryGetValue(name, out int location))
        {
            return location;
        }
        
        Console.WriteLine($"Warning: Uniform '{name}' not found.");
        return -1;
    }

    /// <summary>
    /// Načte všechny aktivní uniform proměnné programu do cache.
    /// </summary>
    private void LoadUniforms()
    {
        GL.GetProgram(ProgramId, GetProgramParameterName.ActiveUniforms, out int uniformCount);
        for (int i = 0; i < uniformCount; i++)
        {
            GL.GetActiveUniform(ProgramId, i, 256, out _, out _, out _, out string name);
            int location = GL.GetUniformLocation(ProgramId, name);
            if (location != -1)
            {
                uniforms[name] = location;
                Console.WriteLine($"Loaded uniform: {name} -> {location}");
            }
        }
    }

    /// <summary>
    /// Uvolní OpenGL shader program.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        GL.DeleteProgram(ProgramId);
        disposed = true;
    }
}
