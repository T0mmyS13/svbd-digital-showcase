using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace DroneWar.Models
{
    /// <summary>
    /// Třída reprezentující střelu vystřelenou z děla.
    /// Simuluje balistický let s vlivem gravitace a větru.
    /// </summary>
    public class Projectile
    {
        // ============================================================
        // KONSTANTY
        // ============================================================
        
        /// <summary>Poloměr exploze při dopadu (v metrech).</summary>
        private const double ExplosionRadius = 50.0;
        
        /// <summary>Hloubka kráteru v terénu (v metrech).</summary>
        private const double CraterDepth = 15.0;

        // ============================================================
        // POZICE A RYCHLOST
        // ============================================================
        
        private double _x, _y, _z;                    // Aktuální pozice (metry)
        private double _velocityX, _velocityY, _velocityZ;  // Rychlost (m/s)
        private double _startX, _startY;              // Počáteční pozice (pro trajektorii)

        // ============================================================
        // STAV STŘELY
        // ============================================================
        
        private bool _isActive;                       // Je střela v letu?
        private double _flightTime;                   // Čas letu (sekundy)
        private double _impactX, _impactY, _impactZ;  // Místo dopadu
        
        /// <summary>Azimut při výstřelu (pro profil terénu).</summary>
        public double FiredAzimuth;
        
        /// <summary>Animace exploze po dopadu.</summary>
        public Explosion? CurrentExplosion;

        /// <summary>Body trajektorie pro graf v profilu terénu.</summary>
        public List<SKPoint> TrajectoryPoints = new();

        // ============================================================
        // GRAFIKA
        // ============================================================
        
        private static SKSvg? _projectileSvg;
        private static bool _imageLoaded;

        // ============================================================
        // VYTVOŘENÍ STŘELY
        // ============================================================
        
        /// <summary>
        /// Vytvoří novou střelu vystřelenou z děla.
        /// </summary>
        public static Projectile Create(Cannon cannon)
        {
            LoadResource();
            
            var projectile = new Projectile
            {
                _x = cannon.X,
                _y = cannon.Y,
                _z = cannon.Elevation,
                _startX = cannon.X,
                _startY = cannon.Y,
                _isActive = true,
                _flightTime = 0.0,
                FiredAzimuth = cannon.Azimuth,
                TrajectoryPoints =
                [
                    new SKPoint(0, (float)cannon.Elevation) // První bod trajektorie
                ]
            };

            projectile.CalculateInitialVelocity(cannon);
            
            return projectile;
        }
        
        /// <summary>
        /// Načte SVG obrázek střely.
        /// </summary>
        private static void LoadResource()
        {
            if (_imageLoaded) return;
            _imageLoaded = true;

            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Img", "Projectile.svg");
                
                if (File.Exists(path))
                {
                    _projectileSvg = new SKSvg();
                    _projectileSvg.Load(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba načítání Projectilu: {ex.Message}");
            }
        }
        
        
        /// <summary>Zjistí, zda střela stále letí.</summary>
        public bool IsActive()
        {
            return _isActive;
        }

        public double ProjectileZ()
        {
            return _z;
        }
        
        
        /// <summary>
        /// Vypočítá počáteční rychlost z nastavení děla.
        /// </summary>
        private void CalculateInitialVelocity(Cannon cannon)
        {
            double azimuthRad = cannon.Azimuth * Math.PI / 180.0;
            double zenithRad = cannon.Zenith * Math.PI / 180.0;

            double horizontalSpeed = cannon.InitialVelocity * Math.Cos(zenithRad);
            _velocityX = horizontalSpeed * Math.Cos(azimuthRad);
            _velocityY = -horizontalSpeed * Math.Sin(azimuthRad);
            _velocityZ = cannon.InitialVelocity * Math.Sin(zenithRad);
        }

        // ============================================================
        // AKTUALIZACE
        // ============================================================
        
        /// <summary>
        /// Aktualizuje stav střely - pohyb, kolize, exploze.
        /// </summary>
        public void Update(double deltaTime, Wind wind, ScenarioData scenarioData, Drone drone)
        {
            // Po dopadu pouze aktualizovat explozi
            if (!_isActive)
            {
                CurrentExplosion?.Update(deltaTime);
                return;
            }
            
            // Pohyb střely
            UpdatePosition(deltaTime);
            UpdateVelocity(deltaTime, wind);
            
            // Záznam trajektorie
            double dx = _x - _startX;
            double dy = _y - _startY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            TrajectoryPoints.Add(new SKPoint(distance, (float)_z));
            
            _flightTime += deltaTime;
            
            // Rozměry mapy
            double mapWidth = scenarioData.Width * scenarioData.Dx;
            double mapHeight = scenarioData.Height * scenarioData.Dy;

            // Kontrola zásahu dronu
            if (CheckDroneHit(drone))
            {
                RegisterImpact(_x, _y, _z,scenarioData);
                drone.IsDestroyed = true;
                Console.WriteLine($"ZÁSAH! Dron zasažen po {_flightTime:F2}s");
                return;
            }
            
            // Kontrola opuštění mapy
            if (_x < 0 || _x > mapWidth || _y < 0 || _y > mapHeight)
            {
                RegisterImpact(_x, _y, _z,scenarioData);
                Console.WriteLine($"Střela opustila mapu po {_flightTime:F2}s");
                return;
            }
            
            // Kontrola nárazu do terénu
            if (CheckTerrainHit(scenarioData))
            {
                if (CheckDroneHitByExplosion(drone))
                {
                    drone.IsDestroyed = true;
                    Console.WriteLine($"ZÁSAH EXPLOZÍ! Dron zničen po {_flightTime:F2}s");
                }
                else
                {
                    Console.WriteLine($"Střela minula po {_flightTime:F2}s");
                }
            }
        }

        /// <summary>Aktualizuje pozici podle rychlosti.</summary>
        private void UpdatePosition(double deltaTime)
        {
            _x += _velocityX * deltaTime;
            _y += _velocityY * deltaTime;
            _z += _velocityZ * deltaTime;
        }

        /// <summary>Aktualizuje rychlost (gravitace + vítr).</summary>
        private void UpdateVelocity(double deltaTime, Wind wind)
        {
            double gravityEffect = -Physics.Gravity * deltaTime;
            
            double windEffectX = (wind.Vx - _velocityX) * Physics.WindCoefficient * deltaTime;
            double windEffectY = (wind.Vy - _velocityY) * Physics.WindCoefficient * deltaTime;
            double windEffectZ = (wind.Vz - _velocityZ) * Physics.WindCoefficient * deltaTime;
            
            _velocityX += windEffectX;
            _velocityY += windEffectY;
            _velocityZ += gravityEffect + windEffectZ;
        }

        // ============================================================
        // DETEKCE KOLIZÍ
        // ============================================================
        
        /// <summary>Kontroluje přímý zásah dronu.</summary>
        private bool CheckDroneHit(Drone drone)
        {
            if (drone.IsDestroyed) return false;
            
            double dx = _x - drone.X;
            double dy = _y - drone.Y;
            double dz = _z - drone.Z;
            double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            
            return distance <= ExplosionRadius;
        }
        
        /// <summary>Kontroluje zásah dronu explozí.</summary>
        private bool CheckDroneHitByExplosion(Drone drone)
        {
            if (drone.IsDestroyed) return false;
            
            double dx = _impactX - drone.X;
            double dy = _impactY - drone.Y;
            double dz = _impactZ - drone.Z;
            double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            
            return distance <= ExplosionRadius;
        }

        /// <summary>Kontroluje náraz do terénu.</summary>
        private bool CheckTerrainHit(ScenarioData scenarioData)
        {
            double terrainHeight = scenarioData.GetElevation(_x, _y);
            
            if (terrainHeight >= 0 && _z <= terrainHeight)
            {
                RegisterImpact(_x, _y, terrainHeight,scenarioData);
                return true;
            }
            
            return false;
        }
        
        /// <summary>Zaregistruje dopad a vytvoří explozi + kráter v terénu.</summary>
        private void RegisterImpact(double x, double y, double z, ScenarioData scenarioData)
        {
            _isActive = false;
            _impactX = x;
            _impactY = y;
            _impactZ = z;
            CurrentExplosion = new Explosion(x, y);
            
            // Vytvořit kráter v terénu
            scenarioData.CreateCrater(x, y, ExplosionRadius, CraterDepth);
        }

        // ============================================================
        // VYKRESLOVÁNÍ
        // ============================================================
        
        /// <summary>
        /// Vykreslí střelu, explozi nebo kráter.
        /// </summary>
        public void Draw(SKCanvas canvas, float scale)
        {
            // Exploze
            if (CurrentExplosion != null && CurrentExplosion.IsActive())
            {
                CurrentExplosion.Draw(canvas, scale);
            }
            else if (_isActive)
            {
                DrawFlyingProjectile(canvas, scale);
            }
            
            // Trajektorie
            DrawTrajectory(canvas, scale);
        }

        /// <summary>Vykreslí letící střelu.</summary>
        private void DrawFlyingProjectile(SKCanvas canvas, float scale)
        {
            float sx = (float)_x * scale;
            float sy = (float)_y * scale;

            canvas.Save();
            canvas.Translate(sx, sy);
            
            // Rotace ve směru letu
            float angleDegrees = (float)(Math.Atan2(_velocityY, _velocityX) * 180.0 / Math.PI);
            canvas.RotateDegrees(angleDegrees);
            
            if (_projectileSvg?.Picture != null)
            {
                var svgBounds = _projectileSvg.Picture.CullRect;
                float targetSize = Math.Max(20f, 3f * scale);
                float svgScale = targetSize / Math.Max(svgBounds.Width, svgBounds.Height);

                canvas.Scale(svgScale, svgScale);
                canvas.Translate(-svgBounds.MidX, -svgBounds.MidY);
                canvas.DrawPicture(_projectileSvg.Picture);
            }
            else
            {
                // Náhradní kruh
                float radius = Math.Max(8f, 1.5f * scale);
                using var fill = new SKPaint
                {
                    Color = SKColors.DarkRed, 
                    Style = SKPaintStyle.Fill, 
                    IsAntialias = true
                };
                using var stroke = new SKPaint
                {
                    Color = SKColors.Black, 
                    Style = SKPaintStyle.Stroke, 
                    StrokeWidth = 2, 
                    IsAntialias = true
                };
                canvas.DrawCircle(0, 0, radius, fill);
                canvas.DrawCircle(0, 0, radius, stroke);
            }

            canvas.Restore();
        }
        
        /// <summary>Vykreslí trajektorii.</summary>
        private void DrawTrajectory(SKCanvas canvas, float scale)
        {
            using var path = new SKPath();
            path.MoveTo((float)_startX * scale, (float)_startY * scale);
            path.LineTo((float)_x * scale, (float)_y * scale);

            using var paint = new SKPaint
            {
                Color = SKColors.Blue.WithAlpha(200),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            
            canvas.DrawPath(path, paint);
        }
    }
}
