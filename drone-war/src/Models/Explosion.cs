using SkiaSharp;

namespace DroneWar.Models
{
    /// <summary>
    /// Třída reprezentující animaci exploze.
    /// Exploze se postupně zvětšuje a mizí.
    /// </summary>
    public class Explosion
    {
        // ============================================================
        // POZICE
        // ============================================================
        
        /// <summary>X souřadnice exploze.</summary>
        private readonly double _x;
        
        /// <summary>Y souřadnice exploze.</summary>
        private readonly double _y;
        
        
        // ============================================================
        // ANIMACE
        // ============================================================
        
        /// <summary>Aktuální čas animace (v sekundách).</summary>
        private double _currentTime;
        
        /// <summary>Celková délka animace (v sekundách).</summary>
        private readonly double _duration;
        
        /// <summary>Maximální poloměr exploze (v metrech).</summary>
        private readonly double _maxRadius;
        
        
        // ============================================================
        // KONSTRUKTOR
        // ============================================================
        
        /// <summary>
        /// Vytvoří novou explozi na zadané pozici.
        /// </summary>
        /// <param name="x">X souřadnice.</param>
        /// <param name="y">Y souřadnice.</param>
        public Explosion(double x, double y)
        {
            _x = x;
            _y = y;
            
            // Nastavení animace
            _duration = 3;
            _currentTime = 0;
            _maxRadius = 150.0;
        }

        // ============================================================
        // AKTUALIZACE
        // ============================================================
        
        /// <summary>
        /// Aktualizuje stav animace.
        /// </summary>
        /// <param name="deltaTime">Čas od posledního volání (v sekundách).</param>
        public void Update(double deltaTime)
        {
            _currentTime += deltaTime;
        }

        /// <summary>
        /// Vrací true, pokud animace stále běží.
        /// </summary>
        public bool IsActive()
        {
            return _currentTime < _duration;
        }

        // ============================================================
        // VYKRESLOVÁNÍ
        // ============================================================

        /// <summary>
        /// Vykreslí animaci exploze.
        /// </summary>
        /// <param name="canvas">Skia canvas.</param>
        /// <param name="scale">Měřítko (pixelů na metr).</param>
        public void Draw(SKCanvas canvas, float scale)
        {
            if (!IsActive())
                return;
            
            float progress = (float)(_currentTime / _duration);

            // Převod souřadnic
            float screenX = (float)_x * scale;
            float screenY = (float)_y * scale;
            float baseRadius = (float)(_maxRadius * scale * progress);
            
            //  BARVA EXPLOZE
            SKColor centerColor;
            SKColor outerColor;

            if (progress < 0.2f)
            {
                // ZAČÁTEK: Bílá -> Žlutá
                centerColor = SKColors.White;
                outerColor = SKColors.Yellow;
            }
            else if (progress < 0.6f)
            {
                // STŘED: Žlutá -> Oranžovo-Červená
                centerColor = SKColors.Yellow;
                outerColor = SKColors.OrangeRed;
            }
            else
            {
                // KONEC: Tmavě červená -> Šedá (Kouř)
                centerColor = SKColors.DarkRed;
                outerColor = SKColors.DarkGray;
            }

            //Průhlednost
            byte alpha = (byte)(255 * (1.0f - progress));
            centerColor = centerColor.WithAlpha(alpha);
            outerColor = outerColor.WithAlpha((byte)(alpha * 0.5));


            using (var paint = new SKPaint())
            {
                paint.Shader = SKShader.CreateRadialGradient(
                    new SKPoint(screenX, screenY),
                    baseRadius,
                    [centerColor, outerColor, outerColor.WithAlpha(0)],
                    [0.0f, 0.7f, 1.0f],
                    SKShaderTileMode.Clamp
                );

                paint.IsAntialias = true;
                paint.Style = SKPaintStyle.Fill;

                canvas.DrawCircle(screenX, screenY, baseRadius, paint);
            }
        }
    }
}


