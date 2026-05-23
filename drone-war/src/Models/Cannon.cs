using SkiaSharp;
using System;
using System.IO;
using Svg.Skia;

namespace DroneWar.Models;

/// <summary>
/// Třída reprezentující dělo (tank) hráče.
/// Dělo má pozici na mapě, směr střelby (azimut, zenit) a sílu výstřelu.
/// </summary>
public class Cannon
{
    // ============================================================
    // KONSTANTY
    // ============================================================
    
    /// <summary>Maximální rychlost střely (m/s).</summary>
    private const double MaxVelocity = 500;
    
    // ============================================================
    // POZICE A SMĚR
    // ============================================================
    
    /// <summary>X souřadnice děla na mapě (v metrech).</summary>
    public readonly double X;
    
    /// <summary>Y souřadnice děla na mapě (v metrech).</summary>
    public readonly double Y;
    
    /// <summary>Výška terénu pod dělem (v metrech).</summary>
    public readonly double Elevation;
    
    /// <summary>Azimut - horizontální úhel otočení (0-360 stupňů).</summary>
    public double Azimuth;
    
    /// <summary>Zenit - vertikální úhel hlavně (0-90 stupňů).</summary>
    public double Zenith;
    
    /// <summary>Počáteční rychlost střely (m/s).</summary>
    public double InitialVelocity;
    
    // ============================================================
    // STAV
    // ============================================================
    
    /// <summary>Zda bylo dělo zničeno.</summary>
    public bool IsDestroyed;
    
    /// <summary>Aktuální animace exploze (null = žádná).</summary>
    public Explosion? CurrentExplosion;
    
    // ============================================================
    // GRAFIKA
    // ============================================================
    
    /// <summary>SVG obrázek těla tanku.</summary>
    private static SKSvg? _bodySvg;
    
    /// <summary>SVG obrázek hlavně.</summary>
    private static SKSvg? _barrelSvg;
    
    /// <summary>Zda už byly obrázky načteny.</summary>
    private static bool _imageLoaded;
        
    // ============================================================
    // KONSTRUKTOR
    // ============================================================
    
    /// <summary>
    /// Vytvoří nové dělo s danou pozicí a nastavením.
    /// </summary>
    /// <param name="x">X pozice (v metrech).</param>
    /// <param name="y">Y pozice (v metrech).</param>
    /// <param name="elevation">Výška terénu (v metrech).</param>
    /// <param name="azimuth">Počáteční azimut (stupně).</param>
    /// <param name="zenith">Počáteční zenit (stupně).</param>
    /// <param name="initialVelocity">Počáteční rychlost střely (m/s).</param>
    public Cannon(double x, double y, double elevation, double azimuth, double zenith, double initialVelocity)
    {
        X = x;
        Y = y;
        Elevation = elevation;
        
        // Azimut normalizujeme na 0-360
        Azimuth = azimuth % 360;
        
        // Zenit omezíme na 0-90
        Zenith = Math.Clamp(zenith, 0, 90);

        // Rychlost nesmí být záporná
        InitialVelocity = Math.Max(initialVelocity, 0);
        
        LoadResources();
    }
    
    // ============================================================
    // NAČÍTÁNÍ ZDROJŮ
    // ============================================================
    
    /// <summary>
    /// Načte SVG obrázky tanku (voláno jednou).
    /// </summary>
    private static void LoadResources()
    {
        if (_imageLoaded) return;
        _imageLoaded = true;

        try 
        {
            string imgFolder = Path.Combine(AppContext.BaseDirectory, "Img");
            
            // Načteme tělo tanku
            string bodyPath = Path.Combine(imgFolder, "TankBody.svg");
            if (File.Exists(bodyPath)) 
            {
                _bodySvg = new SKSvg();
                _bodySvg.Load(bodyPath);
            }

            // Načteme hlaveň
            string barrelPath = Path.Combine(imgFolder, "TankBarrel.svg");
            if (File.Exists(barrelPath)) 
            {
                _barrelSvg = new SKSvg();
                _barrelSvg.Load(barrelPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba načítání tanku: {ex.Message}");
        }
    }

    // ============================================================
    // ÚPRAVY PARAMETRŮ
    // ============================================================
    
    /// <summary>
    /// Upraví azimut děla.
    /// </summary>
    /// <param name="deltaDegrees">Změna ve stupních.</param>
    public void AdjustAzimuth(double deltaDegrees)
    {
        Azimuth = (Azimuth + deltaDegrees) % 360.0;
        if (Azimuth < 0) Azimuth += 360;
    }

    /// <summary>
    /// Upraví zenit hlavně.
    /// </summary>
    /// <param name="deltaDegrees">Změna ve stupních.</param>
    public void AdjustZenith(double deltaDegrees)
    {
        Zenith = Math.Clamp(Zenith + deltaDegrees, 0, 90);
    }

    /// <summary>
    /// Upraví rychlost střely.
    /// </summary>
    /// <param name="delta">Změna v m/s.</param>
    public void AdjustVelocity(double delta)
    {
        InitialVelocity = Math.Clamp(InitialVelocity + delta, 0, MaxVelocity);
    }
    
    // ============================================================
    // VYKRESLOVÁNÍ
    // ============================================================
    
    /// <summary>
    /// Vykreslí dělo na canvas.
    /// </summary>
    /// <param name="canvas">Skia canvas pro kreslení.</param>
    /// <param name="scale">Měřítko (pixelů na metr).</param>
    public void Draw(SKCanvas canvas, float scale)
    {
        if (CurrentExplosion != null)
        {
            CurrentExplosion.Draw(canvas, scale);
            return;
        }
        
        if (IsDestroyed) 
            return;
        
        float screenX = (float)X * scale;
        float screenY = (float)Y * scale;
        
        float tankSize = Math.Max(30, 5.0f * scale);
        
        canvas.Save();
        canvas.Translate(screenX, screenY);
        
        DrawVelocityIndicator(canvas, tankSize, scale);
        
        canvas.RotateDegrees((float)-Azimuth);
        
        DrawTankBody(canvas, tankSize);
        
        DrawTankBarrel(canvas, tankSize);

        canvas.Restore();
    }
    
    /// <summary>
    /// Vykreslí tělo tanku.
    /// </summary>
    private void DrawTankBody(SKCanvas canvas, float tankSize)
    {
        if (_bodySvg?.Picture != null)
        {
            DrawSvgCentered(canvas, _bodySvg, tankSize);
        }
        else
        {
            // Náhradní šedý čtverec
            using var paint = new SKPaint
            {
                Color = SKColors.Gray
            };
            canvas.DrawRect(-tankSize/2, -tankSize/2, tankSize, tankSize, paint);
        }
    }
    
    /// <summary>
    /// Vykreslí hlaveň tanku (zkrácená podle zenitu).
    /// </summary>
    private void DrawTankBarrel(SKCanvas canvas, float tankSize)
    {
        if (_barrelSvg?.Picture == null) return;
        
        canvas.Save();

        // Zkrácení hlavně podle zenitu
        float shortening = (float)Math.Cos(Zenith * Math.PI / 180.0);
        float barrelScale = Math.Max(0.2f, shortening);
        
        canvas.Scale(barrelScale, 1.0f);

        DrawSvgCentered(canvas, _barrelSvg, tankSize);

        canvas.Restore();
    }
    
    /// <summary>
    /// Vykreslí SVG obrázek vycentrovaný na pozici 0,0.
    /// </summary>
    private void DrawSvgCentered(SKCanvas canvas, SKSvg svg, float size)
    {
        if (svg.Picture != null)
        {
            var svgBounds = svg.Picture.CullRect; 
            float svgScale = size / Math.Max(svgBounds.Width, svgBounds.Height);
        
            canvas.Save();
            canvas.Scale(svgScale, svgScale);
            canvas.Translate(-svgBounds.MidX, -svgBounds.MidY);
            
            canvas.DrawPicture(svg.Picture);
            canvas.Restore();
        }

        
    }
    
    /// <summary>
    /// Vykreslí indikátor síly výstřelu.
    /// </summary>
    private void DrawVelocityIndicator(SKCanvas canvas, float tankSize, float scale)
    {
        const float barWidth = 50.0f;
        const float barHeight = 6.0f;
        
        // Rozhodneme, zda kreslit nad nebo pod tankem
        float pixelsFromTop = (float)Y * scale;
        float neededSpace = tankSize * 0.5f + 15;
        bool drawBelow = pixelsFromTop < neededSpace;

        float offset = tankSize * 0.5f; 
        float yPos = drawBelow ? (offset + barHeight) : (-offset - barHeight * 2);

        // Oblast pro bar
        var barRect = SKRect.Create(-barWidth / 2, yPos, barWidth, barHeight);

        // Procento naplnění (0-1)
        float percent = (float)Math.Min(InitialVelocity / MaxVelocity, 1.0f);

        // Pozadí baru
        using (var bgPaint = new SKPaint
               {
                   Color = SKColors.DarkGray, 
                   IsAntialias = true
               })
            canvas.DrawRect(barRect, bgPaint);

        // Barva podle síly (zelená -> oranžová -> červená)
        SKColor fillColor;
        if (percent < 0.5f)
            fillColor = SKColors.LimeGreen;
        else if (percent < 0.8f)
            fillColor = SKColors.Orange;
        else
            fillColor = SKColors.Red;
        
        // Výplň baru
        using (var fillPaint = new SKPaint
               {
                   Color = fillColor, 
                   IsAntialias = true
               })
            canvas.DrawRect(barRect.Left, barRect.Top, barRect.Width * percent, barRect.Height, fillPaint);

        // Okraj baru
        using (var borderPaint = new SKPaint
               {
                   Color = SKColors.Black, 
                   Style = SKPaintStyle.Stroke, 
                   StrokeWidth = 1, 
                   IsAntialias = true
               })
            canvas.DrawRect(barRect, borderPaint);

        // Text s rychlostí
        DrawVelocityText(canvas, barRect, drawBelow);
    }
    
    /// <summary>
    /// Vykreslí text s aktuální rychlostí.
    /// </summary>
    private void DrawVelocityText(SKCanvas canvas, SKRect barRect, bool drawBelow)
    {
        using var textPaint = new SKPaint 
        { 
            Color = SKColors.Black, 
            IsAntialias = true, 
            TextSize = 11,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        string text = $"{InitialVelocity:F0} m/s";
        
        // Změříme velikost textu
        SKRect textBounds = new SKRect();
        textPaint.MeasureText(text, ref textBounds);
        float textWidth = textBounds.Width;
        float textHeight = textBounds.Height;

        // Pozice textu
        float textY = drawBelow 
            ? barRect.Bottom + textHeight + 4 
            : barRect.Top - 5;

        // Pozadí pro text
        const float padding = 2;
        var bgRect = SKRect.Create(
            -textWidth / 2 - padding,
            textY - textHeight - padding,
            textWidth + 2 * padding,
            textHeight + 2 * padding
        );

        using var bgPaint = new SKPaint
        {
            Color = SKColors.White, 
            IsAntialias = true
        };
        canvas.DrawRect(bgRect, bgPaint);

        // Text
        canvas.DrawText(text, -textWidth / 2, textY, textPaint);
    }
}