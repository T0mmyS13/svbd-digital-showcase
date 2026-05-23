using System;
using SkiaSharp;

namespace DroneWar.Models
{
    /// <summary>
    /// Třída pro vykreslování výškové mapy terénu.
    /// </summary>
    public class TerrainMap : IDisposable
    {
        // ============================================================
        // KONSTANTY PRO LEGENDU
        // ============================================================
        
        /// <summary>Šířka barevného pruhu legendy.</summary>
        public const float LegendBarWidth = 40;
        
        /// <summary>Výška legendy.</summary>
        public const float LegendHeight = 200;
        
        /// <summary>Mezera mezi barem a textem.</summary>
        public const float LegendTextSpacing = 10;
        
        /// <summary>Šířka prostoru pro text.</summary>
        public const float LegendTextWidth = 60;
        
        /// <summary>Okraj legendy.</summary>
        public const float LegendPadding = 15;
        
        /// <summary>Celková šířka legendy včetně okrajů.</summary>
        public const float LegendTotalWidth = LegendBarWidth + LegendTextSpacing + LegendTextWidth + LegendPadding * 2;
        
        // ============================================================
        // DATA TERÉNU
        // ============================================================
        
        /// <summary>Reference na data scénáře.</summary>
        private readonly ScenarioData _scenarioData;
        
        /// <summary>Minimální výška terénu na mapě.</summary>
        private int _minHeight;
        
        /// <summary>Maximální výška terénu na mapě.</summary>
        private int _maxHeight;
        
        /// <summary>Šířka buňky v metrech.</summary>
        private readonly int _cellDx;
        
        /// <summary>Výška buňky v metrech.</summary>
        private readonly int _cellDy;

        // ============================================================
        // BAREVNÁ PALETA
        // ============================================================
        
        /// <summary>
        /// Barevná paleta pro vykreslení terénu.
        /// Od nejnižších (voda) po nejvyšší (sníh) body.
        /// </summary>
        private readonly SKColor[] _colorPalette =
        {
            new SKColor(28, 163, 236),   // Světle modrá (voda)
            new SKColor(144, 238, 144),  // Světle zelená (nížiny)
            new SKColor(34, 139, 34),    // Tmavě zelená (louky)
            new SKColor(107, 142, 35),   // Olivově zelená (pahorky)
            new SKColor(189, 183, 107),  // Khaki (kopce)
            new SKColor(210, 180, 140),  // Béžová (hory)
            new SKColor(139, 90, 43),    // Hnědá (vysoké hory)
            new SKColor(105, 105, 105),  // Tmavě šedá (skály)
            new SKColor(169, 169, 169),  // Šedá (vrcholky)
            new SKColor(255, 250, 250)   // Bílá (sníh)
        };
        
        /// <summary>Vygenerovaná bitmapa terénu.</summary>
        private readonly SKBitmap _terrainBitmap;
        
        // ============================================================
        // KONSTRUKTOR
        // ============================================================
        
        /// <summary>
        /// Vytvoří mapu terénu z dat scénáře.
        /// </summary>
        /// <param name="scenarioData">Data scénáře s výškami terénu.</param>
        public TerrainMap(ScenarioData scenarioData)
        {
            _scenarioData = scenarioData;
            _cellDx = scenarioData.Dx;
            _cellDy = scenarioData.Dy;
            
            _minHeight = int.MaxValue;
            _maxHeight = int.MinValue;
            
            for (int row = 0; row < scenarioData.Height; row++)
            {
                for (int col = 0; col < scenarioData.Width; col++)
                {
                    int height = scenarioData.Heights[row, col];
                    if (height < _minHeight) _minHeight = height;
                    if (height > _maxHeight) _maxHeight = height;
                }
            }
            
            // Bitmapu (1 pixel = 1 buňka)
            _terrainBitmap = new SKBitmap(scenarioData.Width, scenarioData.Height);

            for (int row = 0; row < scenarioData.Height; row++)
            {
                for (int col = 0; col < scenarioData.Width; col++)
                {
                    int height = scenarioData.Heights[row, col];
                    SKColor color = GetColorForHeight(height);
                    _terrainBitmap.SetPixel(col, row, color);
                }
            }
        }
        
        /// <summary>
        /// Aktualizuje bitmapu terénu podle aktuálních výškových dat.
        /// Voláno po vytvoření kráteru.
        /// </summary>
        public void RefreshBitmap()
        {
            for (int row = 0; row < _scenarioData.Height; row++)
            {
                for (int col = 0; col < _scenarioData.Width; col++)
                {
                    int height = _scenarioData.Heights[row, col];
                    SKColor color = GetColorForHeight(height);
                    _terrainBitmap.SetPixel(col, row, color);
                }
            }
        }
        
        // ============================================================
        // VÝPOČET BARVY
        // ============================================================
        
        /// <summary>
        /// Získá barvu pro danou výšku terénu pomocí interpolace palety.
        /// </summary>
        private SKColor GetColorForHeight(int height)
        {
            // Normalizace výšky na rozsah 0-1
            float normalized;
            if (_maxHeight == _minHeight)
                normalized = 0.5f;
            else
                normalized = (float)(height - _minHeight) / (_maxHeight - _minHeight);
            
            // Pozice v paletě
            float palettePosition = normalized * (_colorPalette.Length - 1);
            int index1 = Math.Max(0, Math.Min(_colorPalette.Length - 2, (int)palettePosition));
            int index2 = index1 + 1;
            float interpolation = palettePosition - index1;
            
            // Interpolace mezi dvěma barvami
            return InterpolateColor(_colorPalette[index1], _colorPalette[index2], interpolation);
        }
        
        /// <summary>
        /// Lineární interpolace mezi dvěma barvami.
        /// </summary>
        private SKColor InterpolateColor(SKColor color1, SKColor color2, float t)
        {
            byte r = (byte)(color1.Red + (color2.Red - color1.Red) * t);
            byte g = (byte)(color1.Green + (color2.Green - color1.Green) * t);
            byte b = (byte)(color1.Blue + (color2.Blue - color1.Blue) * t);
            return new SKColor(r, g, b);
        }
        
        // ============================================================
        // VYKRESLOVÁNÍ
        // ============================================================
        
        /// <summary>
        /// Vykreslí výškovou mapu terénu.
        /// </summary>
        /// <param name="canvas">Skia canvas.</param>
        /// <param name="currentPixelPerMeter">Aktuální měřítko.</param>
        public void DrawTerrain(SKCanvas canvas, float currentPixelPerMeter)
        {
            float scaleX = _cellDx * currentPixelPerMeter;
            float scaleY = _cellDy * currentPixelPerMeter;
            
            if (scaleX <= 0 || scaleY <= 0) return;

            canvas.Save();
            canvas.Scale(scaleX, scaleY);
            
            using var paint = new SKPaint 
            { 
                FilterQuality = SKFilterQuality.Medium,
                IsAntialias = true 
            };
            
            canvas.DrawBitmap(_terrainBitmap, 0, 0, paint);
            canvas.Restore();
        }
        
        /// <summary>
        /// Vykreslí legendu výšek terénu.
        /// </summary>
        public void DrawLegend(SKCanvas canvas, Avalonia.Rect bounds, float mapRightEdge, float mapTopEdge)
        {
            const float textSize = 12;
            const float titleSize = 13;
            
            // Pozice legendy (napravo od mapy)
            float availableSpace = (float)bounds.Right - mapRightEdge;
            const float minMargin = 5;
            float legendX = mapRightEdge + minMargin + (availableSpace - minMargin * 2 - LegendTotalWidth) / 2;
            float legendY = mapTopEdge + LegendPadding;
            
            var rect = SKRect.Create(legendX, legendY, LegendBarWidth, LegendHeight);
            
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(legendX, legendY + LegendHeight), 
                new SKPoint(legendX, legendY),
                _colorPalette,
                SKShaderTileMode.Clamp);
            
            using var paint = new SKPaint
            {
                Shader = shader
            };
            
            canvas.DrawRect(rect, paint);
            
            // Okraj legendy
            using (var borderPaint = new SKPaint 
            { 
                Color = SKColors.Black, 
                Style = SKPaintStyle.Stroke, 
                StrokeWidth = 1 
            })
            {
                canvas.DrawRect(legendX, legendY, LegendBarWidth, LegendHeight, borderPaint);
            }
            
            // Popisky (min, max, střed)
            using var textPaint = new SKPaint 
            { 
                Color = SKColors.Black, 
                TextSize = textSize, 
                IsAntialias = true, 
                Typeface = SKTypeface.FromFamilyName("Arial") 
            };
            float textX = legendX + LegendBarWidth + LegendTextSpacing;
            
            canvas.DrawText($"{_maxHeight} m", textX, legendY + textSize, textPaint);
            canvas.DrawText($"{_minHeight} m", textX, legendY + LegendHeight, textPaint);
            
            int midHeight = (_minHeight + _maxHeight) / 2;
            canvas.DrawText($"{midHeight} m", textX, legendY + LegendHeight / 2 + textSize / 3, textPaint);
            
            // Titulek
            using var titlePaint = new SKPaint 
            { 
                Color = SKColors.Black, 
                TextSize = titleSize, 
                IsAntialias = true, 
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) 
            };
            canvas.DrawText("Výška terénu", legendX, legendY - 10, titlePaint);
        }
        
        /// <summary>
        /// Uvolní prostředky.
        /// </summary>
        public void Dispose()
        {
            _terrainBitmap.Dispose();
        }
    }
}