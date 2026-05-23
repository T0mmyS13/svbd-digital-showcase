using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using Avalonia;
using Svg.Skia;

namespace DroneWar.Models
{
    /// <summary>
    /// Třída pro vykreslování spodního informačního panelu.
    /// </summary>
    public class TerrainProfile
    {
        // ============================================================
        // KONSTANTY
        // ============================================================
        
        /// <summary>Výška spodního panelu v pixelech.</summary>
        public const float PanelHeight = 250;
        
        /// <summary>SVG obrázek hlavně.</summary>
        private SKSvg? _barrelSvg;
        
        /// <summary>Zda už byly obrázek načten</summary>
        private bool _imageLoaded;

        // ============================================================
        // HLAVNÍ VYKRESLOVACÍ METODA
        // ============================================================
        
        /// <summary>
        /// Vykreslí spodní panel s profilem terénu a trajektorií.
        /// </summary>
        public void Draw(
            SKCanvas canvas, 
            Rect bounds, 
            Cannon cannon, 
            ScenarioData data, 
            Projectile? currentProjectile)
        {
            // Okraje panelu
            const float paddingLeft = 70;
            const float paddingBottom = 40; 
            const float paddingRight = 20;
            const float paddingTop = 20;

            // Oblast panelu
            var panelRect = new SKRect(
                (float)bounds.Left, 
                (float)bounds.Bottom - PanelHeight, 
                (float)bounds.Right, 
                (float)bounds.Bottom
            );

            // Oblast pro graf
            var graphRect = new SKRect(
                panelRect.Left + paddingLeft,
                panelRect.Top + paddingTop,
                panelRect.Right - paddingRight,
                panelRect.Bottom - paddingBottom
            );

            // Pozadí panelu
            using (var paint = new SKPaint
                   {
                       Color = SKColors.White, 
                       Style = SKPaintStyle.Fill
                   })
                canvas.DrawRect(panelRect, paint);

            // Simulace trajektorie (bez větru)
            var predictedPath = SimulateTrajectory(cannon); 
            
            // Výpočet měřítka grafu
            float maxDistance = Math.Max(1000, predictedPath[^1].X * 1.2f);
            float maxAltitude = GetMaxTerrainHeightInDirection(cannon, data, maxDistance);
            
            // Najde max výšku trajektorie
            float maxTrajectoryHeight = 0;
            foreach (var p in predictedPath) 
                if (p.Y > maxTrajectoryHeight) maxTrajectoryHeight = p.Y;
            
            maxAltitude = Math.Max(maxAltitude, maxTrajectoryHeight) * 1.2f; 
            if (maxAltitude < 100) maxAltitude = 100;

            // Měřítka os
            float scaleX = graphRect.Width / maxDistance;
            float scaleY = graphRect.Height / maxAltitude;

            // Kreslení
            canvas.Save();
            canvas.ClipRect(graphRect);

            DrawGrid(canvas, graphRect, maxDistance, maxAltitude, scaleX, scaleY);
            DrawTerrainPolygon(canvas, graphRect, cannon, data, maxDistance, scaleX, scaleY);
            DrawPath(canvas, graphRect, predictedPath, scaleX, scaleY, SKColors.Red, true);

            // Modrá trajektorie pouze pokud azimut odpovídá (tolerance 0.1°)
            if (currentProjectile?.TrajectoryPoints != null && 
                currentProjectile.TrajectoryPoints.Count > 1 &&
                Math.Abs(currentProjectile.FiredAzimuth - cannon.Azimuth) < 0.1)
            {
                DrawPath(canvas, graphRect, currentProjectile.TrajectoryPoints, scaleX, scaleY, SKColors.Blue, false);
            }
            
            canvas.Restore();

            // Rámeček grafu
            using (var borderPaint = new SKPaint 
            { 
                Color = SKColors.Black, 
                Style = SKPaintStyle.Stroke, 
                StrokeWidth = 2 
            })
                canvas.DrawRect(graphRect, borderPaint);

            // Hlaveň děla
            DrawBarrel(canvas, graphRect, cannon, scaleX, scaleY);

            // Popisky os
            DrawAxisLabels(canvas, graphRect, maxDistance, maxAltitude, scaleX, scaleY);
            DrawInfoLabels(canvas, panelRect, cannon);
        }

        // ============================================================
        // SIMULACE TRAJEKTORIE
        // ============================================================
        
        /// <summary>
        /// Simuluje trajektorii střely (bez vlivu větru).
        /// Vrací seznam bodů [vzdálenost, výška].
        /// </summary>
        private static List<SKPoint> SimulateTrajectory(Cannon cannon)
        {
            var points = new List<SKPoint>();
            
            const double timeStep = 0.1;

            double distance = 0;
            double height = cannon.Elevation;
            
            // Rozložení rychlosti
            double zenithRad = cannon.Zenith * Math.PI / 180.0;
            double horizontalVelocity = cannon.InitialVelocity * Math.Cos(zenithRad);
            double verticalVelocity = cannon.InitialVelocity * Math.Sin(zenithRad);

            points.Add(new SKPoint((float)distance, (float)height));
            
            for (int i = 0; i < 2000; i++) 
            {
                horizontalVelocity += -horizontalVelocity * Physics.WindCoefficient * timeStep;
                verticalVelocity += -verticalVelocity * Physics.WindCoefficient * timeStep - Physics.Gravity * timeStep;

                distance += horizontalVelocity * timeStep;
                height += verticalVelocity * timeStep;

                points.Add(new SKPoint((float)distance, (float)height));

                if (height < -1) break;
            }
            
            return points;
        }

        // ============================================================
        // VYKRESLOVÁNÍ TRAJEKTORIE
        // ============================================================
        
        /// <summary>
        /// Vykreslí trajektorii jako čáru.
        /// </summary>
        private static void DrawPath(
            SKCanvas canvas, 
            SKRect rect, 
            List<SKPoint> points, 
            float scaleX, 
            float scaleY, 
            SKColor color, 
            bool dashed)
        {
            if (points.Count == 0) return;

            using var path = new SKPath();
            path.MoveTo(rect.Left + points[0].X * scaleX, rect.Bottom - points[0].Y * scaleY);

            foreach (var p in points)
            {
                float graphX = rect.Left + p.X * scaleX;
                if (graphX > rect.Right + 100) break; 
                path.LineTo(graphX, rect.Bottom - p.Y * scaleY);
            }

            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                IsAntialias = true,
                PathEffect = dashed ? SKPathEffect.CreateDash([5, 5], 0) : null
            };

            canvas.DrawPath(path, paint);
        }

        // ============================================================
        // VÝPOČET VÝŠKY TERÉNU
        // ============================================================
        
        /// <summary>
        /// Získá maximální výšku terénu ve směru střelby.
        /// </summary>
        private static float GetMaxTerrainHeightInDirection(
            Cannon cannon, 
            ScenarioData data, 
            float maxDistance)
        {
            float maxHeight = 0;
            double angleRad = cannon.Azimuth * Math.PI / 180.0;
            double cosA = Math.Cos(angleRad);
            double sinA = -Math.Sin(angleRad);

            for (float distance = 0; distance < maxDistance; distance += 10)
            {
                double terrainHeight = data.GetElevation(cannon.X + distance * cosA, cannon.Y + distance * sinA);
                if (terrainHeight > maxHeight) maxHeight = (float)terrainHeight;
            }
            
            return maxHeight;
        }

        // ============================================================
        // VYKRESLOVÁNÍ MŘÍŽKY
        // ============================================================
        
        /// <summary>
        /// Vykreslí mřížku grafu.
        /// </summary>
        private static void DrawGrid(
            SKCanvas canvas, 
            SKRect rect, 
            float maxDistance, 
            float maxAltitude, 
            float scaleX, 
            float scaleY)
        {
            using var gridPaint = new SKPaint 
            { 
                Color = SKColors.LightGray.WithAlpha(100), 
                StrokeWidth = 1 
            };
            
            // Vertikální čáry (osa X - vzdálenost)
            float stepX = maxDistance > 10000 ? 2000 : maxDistance > 5000 ? 1000 : maxDistance > 2000 ? 500 : 200;
            for (float d = 0; d <= maxDistance; d += stepX)
                canvas.DrawLine(rect.Left + d * scaleX, rect.Top, rect.Left + d * scaleX, rect.Bottom, gridPaint);

            // Horizontální čáry (osa Y - výška)
            float stepY = maxAltitude > 5000 ? 1000 : maxAltitude > 2000 ? 500 : 
                          maxAltitude > 1000 ? 250 : maxAltitude > 500 ? 100 : 50;
            for (float a = 0; a <= maxAltitude; a += stepY)
                canvas.DrawLine(rect.Left, rect.Bottom - a * scaleY, rect.Right, rect.Bottom - a * scaleY, gridPaint);
        }

        // ============================================================
        // POPISKY OS
        // ============================================================
        
        /// <summary>
        /// Vykreslí popisky os X a Y.
        /// </summary>
        private static void DrawAxisLabels(
            SKCanvas canvas, 
            SKRect rect, 
            float maxDistance, 
            float maxAltitude, 
            float scaleX, 
            float scaleY)
        {
            using var textPaint = new SKPaint
            {
                Color = SKColors.Gray, 
                TextSize = 10, 
                IsAntialias = true
            };
            using var titlePaint = new SKPaint 
            { 
                Color = SKColors.DarkSlateGray, 
                TextSize = 11, 
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            // Popisky osy X
            float labelStepX = maxDistance > 10000 ? 2000 : maxDistance > 5000 ? 1000 : maxDistance > 2000 ? 500 : 200;
            for (float d = 0; d <= maxDistance; d += labelStepX)
            {
                string label = $"{d/1000:0.#}km";
                canvas.DrawText(label, rect.Left + d * scaleX - textPaint.MeasureText(label)/2, rect.Bottom + 20, textPaint);
            }
            
            string xTitle = "Vzdálenost [m]";
            canvas.DrawText(xTitle, rect.MidX - titlePaint.MeasureText(xTitle) / 2, rect.Bottom + 35, titlePaint);

            // Popisky osy Y
            float labelStepY = maxAltitude > 2000 ? 1000 : maxAltitude > 1000 ? 500 : 
                               maxAltitude > 500 ? 200 : 100;
            for (float a = 0; a <= maxAltitude; a += labelStepY)
            {
                float y = rect.Bottom - a * scaleY;
                if (y < rect.Top) continue;
                string label = $"{a:0}";
                canvas.DrawText(label, rect.Left - textPaint.MeasureText(label) - 5, y + 4, textPaint);
            }
            
            // Titulek osy Y (otočený)
            canvas.Save();
            string yTitle = "Výška [m]";
            canvas.RotateDegrees(-90, rect.Left - 40, rect.MidY);
            canvas.DrawText(yTitle, rect.Left - 40 - titlePaint.MeasureText(yTitle) / 2, rect.MidY + 4, titlePaint);
            canvas.Restore();
        }

        // ============================================================
        // VYKRESLOVÁNÍ TERÉNU
        // ============================================================
        
        /// <summary>
        /// Vykreslí profil terénu jako vyplněný polygon.
        /// </summary>
        private static void DrawTerrainPolygon(
            SKCanvas canvas, 
            SKRect rect, 
            Cannon cannon, 
            ScenarioData data, 
            float maxDistance, 
            float scaleX, 
            float scaleY)
        {
            double angleRad = cannon.Azimuth * Math.PI / 180.0;
            double cosA = Math.Cos(angleRad);
            double sinA = -Math.Sin(angleRad);

            using var path = new SKPath();
            path.MoveTo(rect.Left, rect.Bottom);

            float step = Math.Max(10f, maxDistance / 200f); 
            for (float dist = 0; dist < maxDistance; dist += step)
            {
                double h = data.GetElevation(cannon.X + dist * cosA, cannon.Y + dist * sinA);
                if (h < 0) h = 0;
                path.LineTo(rect.Left + dist * scaleX, rect.Bottom - (float)h * scaleY);
            }
            path.LineTo(rect.Right, rect.Bottom);
            path.Close();

            // Gradient od hnědé k zelené
            var colors = new SKColor[] { new SKColor(34, 139, 34),new SKColor(101, 67, 33)  };
            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(rect.Left, rect.Top), 
                new SKPoint(rect.Left, rect.Bottom),
                colors, 
                SKShaderTileMode.Clamp);

            using var fillPaint = new SKPaint 
            { 
                Shader = shader, 
                Style = SKPaintStyle.Fill, 
                IsAntialias = true 
            };
            canvas.DrawPath(path, fillPaint);
            
            // Okraj terénu
            using var strokePaint = new SKPaint 
            { 
                Color = SKColors.DarkOliveGreen, 
                Style = SKPaintStyle.Stroke, 
                StrokeWidth = 1,
                IsAntialias = true 
            };
            canvas.DrawPath(path, strokePaint);
        }

        // ============================================================
        // INFORMAČNÍ POPISKY
        // ============================================================
        
        /// <summary>
        /// Vykreslí informace o nastavení děla a legendu.
        /// </summary>
        private static void DrawInfoLabels(SKCanvas canvas, SKRect panelRect, Cannon cannon)
        {
            using var paint = new SKPaint 
            { 
                Color = SKColors.Black, 
                TextSize = 13, 
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            float x = panelRect.Left + 80;
            float y = panelRect.Top + 5;

            // Informace o děle
            canvas.DrawText($"Azimut: {cannon.Azimuth:F1}°", x, y, paint);
            canvas.DrawText($"Zenit: {cannon.Zenith:F1}°", x + 150, y, paint);
            canvas.DrawText($"Počáteční rychlost: {cannon.InitialVelocity:F0} m/s", x + 300, y, paint);
            
            // Legenda trajektorií
            float legendX = panelRect.Right - 220;
            using var textPaint = new SKPaint 
            { 
                Color = SKColors.Black, 
                TextSize = 10, 
                IsAntialias = true 
            };
            
            // Červená přerušovaná - předpověď
            using var redPaint = new SKPaint 
            { 
                Color = SKColors.Red, 
                StrokeWidth = 3, 
                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0) 
            };
            canvas.DrawLine(legendX, y - 5, legendX + 25, y - 5, redPaint);
            canvas.DrawText("Předpověď (bez větru)", legendX + 30, y - 2, textPaint);
            
            // Modrá plná - poslední výstřel
            using var bluePaint = new SKPaint 
            { 
                Color = SKColors.Blue, 
                StrokeWidth = 3 
            };
            canvas.DrawLine(legendX, y + 9, legendX + 25, y + 9, bluePaint);
            canvas.DrawText("Poslední výstřel", legendX + 30, y + 12, textPaint);
        }
        
        // ============================================================
        // VYKRESLENÍ HLAVNĚ
        // ============================================================
        
        /// <summary>
        /// Vykreslí hlaveň děla na začátku trajektorie.
        /// </summary>
        private void DrawBarrel(
            SKCanvas canvas, 
            SKRect graphRect, 
            Cannon cannon, 
            float scaleX, 
            float scaleY)
        {
            if (!_imageLoaded)
                _imageLoaded = true;
            try
            {
                string imgFolder = Path.Combine(AppContext.BaseDirectory, "Img");
                
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
                Console.WriteLine($"Chyba načítání hlavně u profilu terénu: {ex.Message}");
            }
            
            
            // Pozice děla
            float cannonX = graphRect.Left;
            float cannonY = graphRect.Bottom - (float)cannon.Elevation * scaleY;
            cannonY = Math.Clamp(cannonY, graphRect.Top + 5, graphRect.Bottom - 5);
            
            // Výpočet vizuálního úhlu
            var predictedPath = SimulateTrajectory(cannon);
            float visualAngle = 0;
            if (predictedPath.Count > 1)
            {
                int index = Math.Min(10, predictedPath.Count - 1);
                
                float x1 = predictedPath[0].X * scaleX;
                float y1 = predictedPath[0].Y * scaleY;
                float x2 = predictedPath[index].X * scaleX;
                float y2 = predictedPath[index].Y * scaleY;
                
                visualAngle = (float)(Math.Atan2(y2 - y1, x2 - x1) * 180.0 / Math.PI);
            }
            
            canvas.Save();
            canvas.Translate(cannonX, cannonY);
            canvas.RotateDegrees(-visualAngle);
            
            if (_barrelSvg?.Picture != null)
            {
                const float scale = 0.4f;
                const float svgBarrelStartX = 50;
                const float svgBarrelCenterY = 50;
                
                canvas.Scale(scale, scale);
                canvas.Translate(-svgBarrelStartX, -svgBarrelCenterY);
                canvas.DrawPicture(_barrelSvg.Picture);
            }
            else
            {
                // Fallback - jednoduchý obdélník
                const float barrelLength = 30;
                const float barrelWidth = 8;
                
                var barrelRect = SKRect.Create(0, -barrelWidth / 2, barrelLength, barrelWidth);
                using (var barrelPaint = new SKPaint 
                { 
                    Color = SKColors.DimGray, 
                    Style = SKPaintStyle.Fill, 
                    IsAntialias = true 
                })
                    canvas.DrawRoundRect(barrelRect, 2, 2, barrelPaint);
                
                using (var outlinePaint = new SKPaint 
                { 
                    Color = SKColors.Black, 
                    Style = SKPaintStyle.Stroke, 
                    StrokeWidth = 1, 
                    IsAntialias = true 
                })
                    canvas.DrawRoundRect(barrelRect, 2, 2, outlinePaint);
            }
            
            canvas.Restore();
        }
    }
}