using System;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace DroneWar.Models;

/// <summary>
/// Třída reprezentující nepřátelský dron.
/// Dron automaticky letí k dělu a snaží se ho zničit.
/// </summary>
public class Drone
{
    // ============================================================
    // KONSTANTY
    // ============================================================
    
    /// <summary>Rychlost pohybu dronu (m/s).</summary>
    private const double DroneSpeed = 5.0;
    
    /// <summary>Vzdálenost pro útok na cíl (m).</summary>
    private const double AttackDistance = 5.0;
    
    /// <summary>Minimální výška nad terénem (m).</summary>
    private const double MinHeightAboveTerrain = 1.0;

    // ============================================================
    // POZICE
    // ============================================================
    
    /// <summary>X souřadnice dronu (v metrech).</summary>
    public double X;
    
    /// <summary>Y souřadnice dronu (v metrech).</summary>
    public double Y;
    
    /// <summary>Z souřadnice (výška) dronu (v metrech).</summary>
    public double Z;

    // ============================================================
    // STAV
    // ============================================================
    
    /// <summary>Zda byl dron zničen.</summary>
    public bool IsDestroyed;

    /// <summary>Aktuální animace exploze.</summary>
    public Explosion? CurrentExplosion;

    // ============================================================
    // CÍL A SMĚR
    // ============================================================
    
    /// <summary>Cílová pozice X.</summary>
    private double _targetX;
    
    /// <summary>Cílová pozice Y.</summary>
    private double _targetY;
    
    /// <summary>Cílová pozice Z.</summary>
    private double _targetZ;

    /// <summary>Normalizovaný směr letu (X složka).</summary>
    private double _directionX;
    
    /// <summary>Normalizovaný směr letu (Y složka).</summary>
    private double _directionY;
    
    /// <summary>Normalizovaný směr letu (Z složka).</summary>
    private double _directionZ;

    // ============================================================
    // GRAFIKA
    // ============================================================
    
    /// <summary>SVG obrázek dronu.</summary>
    private static SKSvg? _droneSvg;
    
    /// <summary>Zda už byl načten obrázku.</summary>
    private static bool _imageLoaded;

    // ============================================================
    // KONSTRUKTOR
    // ============================================================
    
    /// <summary>
    /// Vytvoří nový dron na zadané pozici.
    /// </summary>
    /// <param name="x">X pozice (v metrech).</param>
    /// <param name="y">Y pozice (v metrech).</param>
    /// <param name="z">Výška (v metrech).</param>
    public Drone(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
        LoadImageResource();
    }

    /// <summary>
    /// Načte SVG obrázek dronu.
    /// </summary>
    private static void LoadImageResource()
    {
        if (_imageLoaded) return;
        _imageLoaded = true;

        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Img", "Drone.svg");
            if (File.Exists(path))
            {
                _droneSvg = new SKSvg();
                _droneSvg.Load(path);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba načítání tanku dronu: {ex.Message}");
        }
    }

    // ============================================================
    // NASTAVENÍ CÍLE
    // ============================================================
    
    /// <summary>
    /// Nastaví cíl, ke kterému se má dron pohybovat.
    /// </summary>
    /// <param name="targetX">Cílová X souřadnice.</param>
    /// <param name="targetY">Cílová Y souřadnice.</param>
    /// <param name="targetZ">Cílová Z souřadnice (výška).</param>
    public void SetTarget(double targetX, double targetY, double targetZ)
    {
        _targetX = targetX;
        _targetY = targetY;
        _targetZ = targetZ;
        RecalculateDirection();
        Console.WriteLine($"Dron cíl: [{_targetX:F1}, {_targetY:F1}, {_targetZ:F1}]");
    }

    /// <summary>
    /// Přepočítá směr letu k cíli.
    /// </summary>
    private void RecalculateDirection()
    {
        double dx = _targetX - X;
        double dy = _targetY - Y;
        double dz = _targetZ - Z;
        double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 0.001)
        {
            _directionX = dx / distance;
            _directionY = dy / distance;
            _directionZ = dz / distance;
        }
    }

    /// <summary>
    /// Vypočítá vzdálenost k zadanému bodu.
    /// </summary>
    /// <param name="tx">Cílová X souřadnice.</param>
    /// <param name="ty">Cílová Y souřadnice.</param>
    /// <param name="tz">Cílová Z souřadnice (výška).</param>
    private double GetDistanceTo(double tx, double ty, double tz)
    {
        return Math.Sqrt(
            Math.Pow(tx - X, 2) + 
            Math.Pow(ty - Y, 2) + 
            Math.Pow(tz - Z, 2));
    }

    // ============================================================
    // AKTUALIZACE
    // ============================================================
    
    /// <summary>
    /// Aktualizuje logiku dronu (pohyb, kolize).
    /// </summary>
    /// <param name="deltaTime">Čas od posledního volání (v sekundách).</param>
    /// <param name="scenario">Data scénáře (pro kontrolu terénu).</param>
    /// <param name="cannon">Dělo (cíl útoku).</param>
    public void Update(double deltaTime, ScenarioData scenario, Cannon cannon)
    {
        if (IsDestroyed) return;

        // 1. Kontrola dosažení cíle (sebevražedný útok)
        double distanceToTarget = GetDistanceTo(_targetX, _targetY, _targetZ);

        if (distanceToTarget < AttackDistance)
        {
            Explode(cannon);
            return;
        }

        // 2. Pohyb směrem k cíli
        X += _directionX * DroneSpeed * deltaTime;
        Y += _directionY * DroneSpeed * deltaTime;
        Z += _directionZ * DroneSpeed * deltaTime;

        // 3. Kontrola terénu
        double terrainHeight = scenario.GetElevation(X, Y);
        if (terrainHeight >= 0 && Z < terrainHeight + MinHeightAboveTerrain)
        {
            Z = terrainHeight + MinHeightAboveTerrain;
        }
    }

    /// <summary>
    /// Provede explozi dronu a zničí dělo.
    /// </summary>
    private void Explode(Cannon cannon)
    {
        IsDestroyed = true;
        CurrentExplosion = new Explosion(X, Y);
        
        cannon.IsDestroyed = true;
        cannon.CurrentExplosion = new Explosion(cannon.X, cannon.Y);

        Console.WriteLine("Dron zasáhl cíl! Hra končí.");
    }

    // ============================================================
    // VYKRESLOVÁNÍ
    // ============================================================
    
    /// <summary>
    /// Vykreslí drona na canvas.
    /// </summary>
    /// <param name="canvas">Skia canvas.</param>
    /// <param name="cannon">Dělo (pro výpočet vzdálenosti).</param>
    /// <param name="scale">Měřítko (pixelů na metr).</param>
    public void Draw(SKCanvas canvas, Cannon cannon, float scale)
    {
        // Pokud běží exploze, vykreslíme ji místo dronu
        if (CurrentExplosion != null)
        {
            CurrentExplosion.Draw(canvas, scale);
            return;
        }

        if (IsDestroyed) return;
        
        float screenX = (float)X * scale;
        float screenY = (float)Y * scale;
        float targetSize = Math.Max(30, 4.0f * scale);
        
        DrawDrone(canvas, screenX, screenY, targetSize);
        
        DrawInfo(canvas, cannon, screenX, screenY);
    }

    /// <summary>
    /// Vykreslí obrázek dronu nebo náhradní kruh.
    /// </summary>
    private void DrawDrone(SKCanvas canvas, float screenX, float screenY, float targetSize)
    {
        if (_droneSvg?.Picture != null)
        {
            canvas.Save();
            canvas.Translate(screenX, screenY);

            // Měřítko pro SVG
            var svgBounds = _droneSvg.Picture.CullRect;
            float svgScale = targetSize / Math.Max(svgBounds.Width, svgBounds.Height);
            canvas.Scale(svgScale, svgScale);
            canvas.Translate(-svgBounds.MidX, -svgBounds.MidY);

            canvas.DrawPicture(_droneSvg.Picture);
            canvas.Restore();
        }
        else
        {
            // Náhradní červený kruh
            using var paint = new SKPaint
            {
                Color = SKColors.Red, 
                IsAntialias = true
            };
            canvas.DrawCircle(screenX, screenY, targetSize / 2, paint);
        }
    }

    /// <summary>
    /// Vykreslí informační popisky (vzdálenost, výška).
    /// </summary>
    private void DrawInfo(SKCanvas canvas, Cannon cannon, float screenX, float screenY)
    {
        double distanceToCannon = GetDistanceTo(cannon.X, cannon.Y, cannon.Elevation);

        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            TextSize = 12,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        using var bgPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 200),
            Style = SKPaintStyle.Fill
        };
        
        void DrawLabel(string text, float offsetY)
        {
            float textWidth = textPaint.MeasureText(text);
            var bgRect = SKRect.Create(
                screenX - textWidth / 2 - 3, 
                screenY + offsetY - 12, 
                textWidth + 6, 
                16);

            canvas.DrawRect(bgRect, bgPaint);
            canvas.DrawText(text, screenX - textWidth / 2, screenY + offsetY, textPaint);
        }

        // Vzdálenost k dělu
        DrawLabel($"{distanceToCannon:F0}m", 30);
        
        // Výška dronu
        DrawLabel($"Z: {Z:F0}m", 48);
    }
}