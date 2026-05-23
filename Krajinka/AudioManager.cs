using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Krajinka;

/// <summary>
/// Přehrává krátké zvukové efekty ze souborů WAV.
/// </summary>
internal sealed class AudioManager : IDisposable
{
    private const uint SndAsync = 0x0001; // Přehrává zvuk asynchronně, aby nedošlo k zablokování hlavního vlákna.
    private const uint SndNodefault = 0x0002; // Pokud se zvuk nenajde, nehraje žádný zvuk (nepoužívá výchozí systémový zvuk).
    private const uint SndFilename = 0x00020000; // Označuje, že první parametr je název souboru.

    private readonly string footstepSoundPath;
    private readonly string footstepRockSoundPath;
    private readonly string waterSplashSoundPath;
    private readonly string flowerTrampleSoundPath;

    /// <summary>
    /// Vytvoří správce zvuků a ověří, že všechny soubory existují.
    /// </summary>
    /// <param name="soundsDirectory">Relativní cesta ke složce se zvuky.</param>
    public AudioManager(string soundsDirectory)
    {
        footstepSoundPath = GetRequiredSoundPath(soundsDirectory, "footstep.wav");
        footstepRockSoundPath = GetRequiredSoundPath(soundsDirectory, "footstep_rock.wav");
        waterSplashSoundPath = GetRequiredSoundPath(soundsDirectory, "water_splash.wav");
        flowerTrampleSoundPath = GetRequiredSoundPath(soundsDirectory, "flower_trample.wav");
    }

    /// <summary>
    /// Pustí zvuk kroku.
    /// </summary>
    public void PlayFootstep()
    {
        PlaySoundFile(footstepSoundPath);
    }

    /// <summary>
    /// Pustí zvuk kroku po kameni.
    /// </summary>
    public void PlayFootstepRock()
    {
        PlaySoundFile(footstepRockSoundPath);
    }

    /// <summary>
    /// Pustí zvuk dopadu do vody.
    /// </summary>
    public void PlayWaterSplash()
    {
        PlaySoundFile(waterSplashSoundPath);
    }

    /// <summary>
    /// Pustí zvuk zašlápnutí květiny.
    /// </summary>
    public void PlayFlowerTrample()
    {
        PlaySoundFile(flowerTrampleSoundPath);
    }

    /// <summary>
    /// Uvolní přehrávání zvuků.
    /// </summary>
    public void Dispose()
    {
        PlaySound(null, IntPtr.Zero, 0);
    }

    private static string GetRequiredSoundPath(string soundsDirectory, string fileName)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, soundsDirectory, fileName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Zvukový soubor nebyl nalezen: {fileName}", fullPath);
        }

        return fullPath;
    }

    private static void PlaySoundFile(string filePath)
    {
        if (!PlaySound(filePath, IntPtr.Zero, SndAsync | SndFilename | SndNodefault))
        {
            throw new InvalidOperationException($"Zvuk se nepodařilo přehrát: {filePath}");
        }
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string? pszSound, IntPtr hmod, uint fdwSound);
}