using System;

namespace DroneWar.Models
{
    /// <summary>
    /// Třída obsahující kompletní data načteného scénáře.
    /// Obsahuje informace o mapě, pozicích objektů a větru.
    /// </summary>
    public class ScenarioData
    {
        // ============================================================
        // METADATA
        // ============================================================
        
        /// <summary>Verze formátu souboru.</summary>
        public int Version;
        
        // ============================================================
        // ROZMĚRY MAPY
        // ============================================================
        
        /// <summary>Počet sloupců mapy (šířka v buňkách).</summary>
        public int Width;
        
        /// <summary>Počet řádků mapy (výška v buňkách).</summary>
        public int Height;
        
        /// <summary>Šířka jedné buňky v metrech.</summary>
        public int Dx;
        
        /// <summary>Výška jedné buňky v metrech.</summary>
        public int Dy;

        // ============================================================
        // DĚLO (STARTOVNÍ POZICE)
        // ============================================================
        
        /// <summary>X pozice děla (sloupec v mřížce).</summary>
        public int XS;
        
        /// <summary>Y pozice děla (řádek v mřížce).</summary>
        public int YS;
        
        /// <summary>Počáteční azimut děla (stupně).</summary>
        public int AS;
        
        /// <summary>Počáteční zenit děla (stupně).</summary>
        public int ZS;
        
        /// <summary>Počáteční rychlost střely (m/s).</summary>
        public int VS;

        // ============================================================
        // VÍTR
        // ============================================================
        
        /// <summary>Složka větru ve směru X (m/s).</summary>
        public int WVx;
        
        /// <summary>Složka větru ve směru Y (m/s).</summary>
        public int WVy;
        
        /// <summary>Složka větru ve směru Z (m/s).</summary>
        public int WVz;

        // ============================================================
        // CÍL (DRON)
        // ============================================================
        
        /// <summary>X pozice dronu (sloupec v mřížce).</summary>
        public int XT;
        
        /// <summary>Y pozice dronu (řádek v mřížce).</summary>
        public int YT;

        // ============================================================
        // TERÉN
        // ============================================================
        
        /// <summary>
        /// 2D pole výšek terénu [řádek, sloupec].
        /// </summary>
        public int[,] Heights = new int[0, 0];

        // ============================================================
        // VYTVÁŘENÍ HERNÍCH OBJEKTŮ
        // ============================================================
        
        /// <summary>
        /// Vytvoří objekt děla podle dat ze scénáře.
        /// </summary>
        /// <returns>Nová instance děla.</returns>
        public Cannon CreateCannon()
        {
            // Převod souřadnic z mřížky na metry
            double worldX = XS * Dx;
            double worldY = YS * Dy;
            double elevation = GetElevation(worldX, worldY);
            
            return new Cannon(worldX, worldY, elevation, AS, ZS, VS);
        }

        /// <summary>
        /// Vytvoří objekt dronu podle dat ze scénáře.
        /// Dron startuje 35m nad terénem.
        /// </summary>
        /// <returns>Nová instance dronu.</returns>
        public Drone CreateDrone()
        {
            double worldX = XT * Dx;
            double worldY = YT * Dy;
            double elevation = GetElevation(worldX, worldY);
            
            // Dron startuje 35 metrů nad zemí
            return new Drone(worldX, worldY, elevation + 35);
        }

        /// <summary>
        /// Vytvoří objekt větru podle dat ze scénáře.
        /// </summary>
        /// <returns>Nová instance větru.</returns>
        public Wind CreateWind()
        {
            return new Wind(WVx, WVy, WVz);
        }

        // ============================================================
        // VÝPOČET VÝŠKY TERÉNU
        // ============================================================
        
        /// <summary>
        /// Získá výšku terénu v zadaných souřadnicích pomocí bilineární interpolace.
        /// </summary>
        /// <param name="x">X souřadnice v metrech.</param>
        /// <param name="y">Y souřadnice v metrech.</param>
        /// <returns>Interpolovaná výška terénu, nebo -1 pokud jsou souřadnice mimo mapu.</returns>
        public double GetElevation(double x, double y)
        {
            // Převod z metrů na pozici v mřížce
            double cellXDouble = x / Dx;
            double cellYDouble = y / Dy;

            int cellX = (int)cellXDouble;
            int cellY = (int)cellYDouble;

            // Kontrola hranic mapy
            if (cellX < 0 || cellX >= Width - 1 || cellY < 0 || cellY >= Height - 1)
            {
                return -1;
            }

            // Pozice v rámci buňky (0 až 1)
            double tx = cellXDouble - cellX;
            double ty = cellYDouble - cellY;

            // Výšky čtyř rohů buňky:
            double h00 = Heights[cellY, cellX];
            double h10 = Heights[cellY, cellX + 1];
            double h01 = Heights[cellY + 1, cellX];
            double h11 = Heights[cellY + 1, cellX + 1];
            
            // Interpolujeme v ose X
            double h0 = h00 * (1 - tx) + h10 * tx;
            double h1 = h01 * (1 - tx) + h11 * tx;
            
            // 2. Interpolujeme v ose Y
            double result = h0 * (1 - ty) + h1 * ty;

            return result;
        }
        
        
        /// <summary>
        /// Vytvoří kráter v terénu - sníží výšku v okolí dopadu.
        /// </summary>
        /// <param name="centerX">X souřadnice středu kráteru (metry).</param>
        /// <param name="centerY">Y souřadnice středu kráteru (metry).</param>
        /// <param name="radius">Poloměr kráteru (metry).</param>
        /// <param name="depth">Maximální hloubka kráteru (metry).</param>
        public void CreateCrater(double centerX, double centerY, double radius, double depth)
        {
            int centerCellX = (int)(centerX / Dx);
            int centerCellY = (int)(centerY / Dy);
            
            // Poloměr v buňkách
            int radiusCells = (int)Math.Ceiling(radius / Math.Min(Dx, Dy));
            
            // Projít všechny buňky v okolí
            for (int dy = -radiusCells; dy <= radiusCells; dy++)
            {
                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                {
                    int cellX = centerCellX + dx;
                    int cellY = centerCellY + dy;
                    
                    // Kontrola hranic
                    if (cellX < 0 || cellX >= Width || cellY < 0 || cellY >= Height)
                        continue;
                    
                    // Vzdálenost od středu (v metrech)
                    double distX = dx * Dx;
                    double distY = dy * Dy;
                    double distance = Math.Sqrt(distX * distX + distY * distY);
                    
                    // Mimo poloměr - přeskočit
                    if (distance > radius)
                        continue;
                    
                    // Hloubka klesá s vzdáleností (parabolický profil)
                    // Ve středu = depth, na okraji = 0
                    double factor = 1.0 - (distance / radius);
                    factor *= factor; // Parabolický profil
                    int depthHere = (int)(depth * factor);
                    
                    // Snížit výšku terénu
                    Heights[cellY, cellX] = Math.Max(0, Heights[cellY, cellX] - depthHere);
                }
            }
        }
    }
    
    /// <summary>
    /// Globální fyzikální konstanty pro celou simulaci.
    /// </summary>
    public static class Physics
    {
        /// <summary>Gravitační zrychlení (m/s²).</summary>
        public const double Gravity = 10.0;
        
        /// <summary>Koeficient odporu vzduchu.</summary>
        public const double WindCoefficient = 0.05;
    }
}

