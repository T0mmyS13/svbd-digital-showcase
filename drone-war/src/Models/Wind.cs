using System;
using Avalonia;
using SkiaSharp;

namespace DroneWar.Models
{
    /// <summary>
    /// Třída reprezentující vítr, který ovlivňuje let střely.
    /// Vítr se plynule mění v náhodných intervalech.
    /// </summary>
    public class Wind
    {
        // ============================================================
        // AKTUÁLNÍ VEKTOR VĚTRU
        // ============================================================
        
        /// <summary>Složka větru ve směru X (m/s).</summary>
        public double Vx;
        
        /// <summary>Složka větru ve směru Y (m/s).</summary>
        public double Vy;
        
        /// <summary>Složka větru ve směru Z (m/s) - vertikální.</summary>
        public double Vz;
        
        // ============================================================
        // CÍLOVÝ VEKTOR (pro plynulý přechod)
        // ============================================================
        
        /// <summary>Cílová hodnota Vx.</summary>
        private double _targetVx;
        
        /// <summary>Cílová hodnota Vy.</summary>
        private double _targetVy;
        
        /// <summary>Cílová hodnota Vz.</summary>
        private double _targetVz;
        
        // ============================================================
        // ČASOVÁNÍ
        // ============================================================
        
        /// <summary>Čas do další náhodné změny (sekundy).</summary>
        private double _timeToNextChange;
        
        /// <summary>Generátor náhodných čísel.</summary>
        private readonly Random _random = new();
        
        // ============================================================
        // KONSTRUKTOR
        // ============================================================
        
        /// <summary>
        /// Vytvoří vítr se zadaným počátečním vektorem.
        /// </summary>
        /// <param name="vx">Počáteční rychlost X (m/s).</param>
        /// <param name="vy">Počáteční rychlost Y (m/s).</param>
        /// <param name="vz">Počáteční rychlost Z (m/s).</param>
        public Wind(double vx, double vy, double vz)
        {
            // Nastavíme aktuální i cílové hodnoty na stejné
            Vx = vx;
            Vy = vy;
            Vz = vz;
            _targetVx = vx;
            _targetVy = vy;
            _targetVz = vz;
            
            // Náhodný čas do první změny (3-8 sekund)
            _timeToNextChange = _random.NextDouble() * 5.0 + 3.0;
        }
        
        // ============================================================
        // AKTUALIZACE
        // ============================================================
        
        /// <summary>
        /// Aktualizuje stav větru - plynulé přechody k novým hodnotám.
        /// </summary>
        /// <param name="deltaTime">Čas od posledního volání (sekundy).</param>
        public void Update(double deltaTime)
        {
            // Rychlost přechodu
            double transitionSpeed = 0.5 * deltaTime;
            
            // Plynulý přechod pro každou složku
            Vx = Change(Vx, _targetVx, transitionSpeed);
            Vy = Change(Vy, _targetVy, transitionSpeed);
            Vz = Change(Vz, _targetVz, transitionSpeed);
            
            _timeToNextChange -= deltaTime;
            if (_timeToNextChange <= 0)
            {
                GenerateNewTarget();
            }
        }
        
        /// <summary>
        /// Posune hodnotu směrem k cíli o daný krok.
        /// </summary>
        private static double Change(double current, double target, double step)
        {
            if (Math.Abs(current - target) <= step)
                return target;
            return current + Math.Sign(target - current) * step;
        }
        
        /// <summary>
        /// Vygeneruje nové náhodné cílové hodnoty větru.
        /// </summary>
        private void GenerateNewTarget()
        {
            // Náhodné hodnoty od -20 do +20 m/s
            _targetVx = (_random.NextDouble() * 2 - 1) * 20.0;
            _targetVy = (_random.NextDouble() * 2 - 1) * 20.0;
            _targetVz = (_random.NextDouble() * 2 - 1) * 20.0;
            
            // Nový náhodný čas (3-5 sekund)
            _timeToNextChange = _random.NextDouble() * 2 + 3.0;
        }
        
        /// <summary>
        /// Vypočítá celkovou rychlost větru (velikost vektoru).
        /// </summary>
        private double GetMagnitude()
        {
            return Math.Sqrt(Vx * Vx + Vy * Vy + Vz * Vz);
        }
        
        // ============================================================
        // VYKRESLOVÁNÍ
        // ============================================================
        
        /// <summary>
        /// Vykreslí indikátor větru (kompas se šipkou).
        /// </summary>
        /// <param name="canvas">Skia canvas.</param>
        /// <param name="bounds">Rozměry okna.</param>
        /// <param name="mapRightEdge">Pravý okraj mapy.</param>
        /// <param name="mapTopEdge">Horní okraj mapy.</param>
        public void Draw(SKCanvas canvas, Rect bounds, float mapRightEdge, float mapTopEdge)
        {
            // Pozice kompasu (pod legendou)
            float availableSpace = (float)bounds.Right - mapRightEdge;
            const float minMargin = 15;
            const float radius = 40;
            float legendX = mapRightEdge + minMargin + (availableSpace - minMargin - TerrainMap.LegendTotalWidth) / 2;
            float centerX = legendX + radius / 2 + minMargin;
            float centerY = mapTopEdge + TerrainMap.LegendPadding + TerrainMap.LegendHeight + radius + minMargin;
            
            double windSpeed = GetMagnitude();

            // 1. Pozadí kompasu (bílý kruh)
            DrawCompassBackground(canvas, centerX, centerY, radius);

            // 2. Šipka větru
            if (windSpeed > 0.1)
            {
                DrawWindArrow(canvas, centerX, centerY, radius, windSpeed);
            }

            // 3. Text s rychlostí
            DrawSpeedText(canvas, centerX, centerY, radius, windSpeed);
        }
        
        /// <summary>
        /// Vykreslí pozadí kompasu s označením světových stran.
        /// </summary>
        private void DrawCompassBackground(SKCanvas canvas, float centerX, float centerY, float radius)
        {
            // Bílý kruh
            using (var bgPaint = new SKPaint 
            { 
                Color = SKColors.White.WithAlpha(200), 
                IsAntialias = true, 
                Style = SKPaintStyle.Fill 
            })
                canvas.DrawCircle(centerX, centerY, radius, bgPaint);
            
            // Černý okraj
            using (var borderPaint = new SKPaint 
            { 
                Color = SKColors.Black, 
                IsAntialias = true, 
                Style = SKPaintStyle.Stroke, 
                StrokeWidth = 1 
            })
                canvas.DrawCircle(centerX, centerY, radius, borderPaint);

            // Světové strany
            using var textPaint = new SKPaint 
            { 
                Color = SKColors.Gray, 
                IsAntialias = true, 
                TextSize = 10, 
                TextAlign = SKTextAlign.Center 
            };
            canvas.DrawText("N", centerX, centerY - radius + 12, textPaint);
            canvas.DrawText("S", centerX, centerY + radius - 4, textPaint);
            canvas.DrawText("E", centerX + radius - 8, centerY + 4, textPaint);
            canvas.DrawText("W", centerX - radius + 8, centerY + 4, textPaint);
        }
        
        /// <summary>
        /// Vykreslí šipku větru uvnitř kompasu.
        /// </summary>
        private void DrawWindArrow(SKCanvas canvas, float centerX, float centerY, float radius, double windSpeed)
        {
            canvas.Save();
            canvas.Translate(centerX, centerY);
            
            // Otočení podle směru větru
            float angle = (float)Math.Atan2(Vy, Vx);
            canvas.RotateRadians(angle);

            // Barva podle vertikální složky
            SKColor arrowColor;
            if (Vz < -2.0)
                arrowColor = SKColors.Red;      // Dolů
            else if (Vz > 2.0)
                arrowColor = SKColors.Green;    // Nahoru
            else
                arrowColor = SKColors.Blue;     

            // Délka šipky podle síly větru
            float arrowLength = Math.Min(radius - 5, (float)windSpeed * 1.5f + 10);

            // Tělo šipky
            using (var arrowPaint = new SKPaint 
            { 
                Color = arrowColor, 
                IsAntialias = true, 
                Style = SKPaintStyle.Stroke, 
                StrokeWidth = 3 
            })
                canvas.DrawLine(-arrowLength/2, 0, arrowLength/2, 0, arrowPaint);

            // Hrot šipky
            using (var fillPaint = new SKPaint 
            { 
                Color = arrowColor, 
                IsAntialias = true, 
                Style = SKPaintStyle.Fill 
            })
            {
                using var path = new SKPath();
                path.MoveTo(arrowLength/2 + 4, 0);
                path.LineTo(arrowLength/2 - 6, -5);
                path.LineTo(arrowLength/2 - 6, 5);
                path.Close();
                canvas.DrawPath(path, fillPaint);
            }
            
            canvas.Restore();
        }
        
        /// <summary>
        /// Vykreslí text s rychlostí větru pod kompasem.
        /// </summary>
        private void DrawSpeedText(SKCanvas canvas, float centerX, float centerY, float radius, double windSpeed)
        {
            string text = $"{windSpeed:F1} m/s";
            
            // Pozadí textu
            using (var bgPaint = new SKPaint 
            { 
                Color = SKColors.White.WithAlpha(180), 
                Style = SKPaintStyle.Fill 
            })
            {
                var rect = SKRect.Create(centerX - 30, centerY + radius + 5, 60, 16);
                canvas.DrawRoundRect(rect, 4, 4, bgPaint);
            }

            // Text
            using var textPaint = new SKPaint 
            { 
                Color = SKColors.Black, 
                IsAntialias = true, 
                TextSize = 12, 
                TextAlign = SKTextAlign.Center, 
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) 
            };
            canvas.DrawText(text, centerX, centerY + radius + 17, textPaint);
        }
    }
}
