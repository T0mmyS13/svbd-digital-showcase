using System;
using System.IO;

namespace DroneWar.Models
{
    /// <summary>
    /// Třída pro načítání herních scénářů ze souborů .ter.
    /// Scénáře obsahují mapu terénu a počáteční nastavení hry.
    /// </summary>
    public static class ScenarioLoader
    {
        /// <summary>
        /// Načte scénář ze souboru podle čísla.
        /// </summary>
        /// <param name="scenarioNumber">Číslo scénáře (např. 0 = soubor "data/0.ter").</param>
        /// <returns>Načtená data scénáře.</returns>
        public static ScenarioData LoadScenario(int scenarioNumber)
        {
            // Sestavíme cestu k souboru
            string fileName = $"{scenarioNumber}.ter";
            string filePath = Path.Combine("data", fileName);
            
            // Otevřeme soubor a načteme data
            using var reader = new BinaryReader(File.OpenRead(filePath));
            var data = ReadScenarioData(reader);
            
            // Vypíšeme informace do konzole
            PrintScenarioInfo(data);
            
            return data;
        }
        
        /// <summary>
        /// Načte všechna data scénáře z binárního souboru.
        /// </summary>
        private static ScenarioData ReadScenarioData(BinaryReader reader)
        {
            var data = new ScenarioData
            {
                // Metadata
                Version = ReadInt32BigEndian(reader),
                
                // Rozměry mapy
                Width = ReadInt32BigEndian(reader),
                Height = ReadInt32BigEndian(reader),
                Dx = ReadInt32BigEndian(reader),
                Dy = ReadInt32BigEndian(reader),
                
                // Dělo
                XS = ReadInt32BigEndian(reader),
                YS = ReadInt32BigEndian(reader),
                AS = ReadInt32BigEndian(reader),
                ZS = ReadInt32BigEndian(reader),
                VS = ReadInt32BigEndian(reader),
                
                // Vítr
                WVx = ReadInt32BigEndian(reader),
                WVy = ReadInt32BigEndian(reader),
                WVz = ReadInt32BigEndian(reader),
                
                // Cíl (dron)
                XT = ReadInt32BigEndian(reader),
                YT = ReadInt32BigEndian(reader)
            };
            
            // Načteme mapu výšek
            data.Heights = ReadHeightMap(reader, data.Width, data.Height);
            
            return data;
        }
        
        /// <summary>
        /// Načte mapu výšek terénu.
        /// Data jsou uložena po řádcích od severu k jihu.
        /// </summary>
        private static int[,] ReadHeightMap(BinaryReader reader, int width, int height)
        {
            var heights = new int[height, width];
            
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    heights[row, col] = ReadInt32BigEndian(reader);
                }
            }
            
            return heights;
        }
        
        /// <summary>
        /// Přečte 32-bitové celé číslo ve formátu big-endian.
        /// </summary>
        private static int ReadInt32BigEndian(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            
            return BitConverter.ToInt32(bytes, 0);
        } 
        
        /// <summary>
        /// Vypíše informace o načteném scénáři do konzole.
        /// </summary>
        private static void PrintScenarioInfo(ScenarioData data)
        {
            Console.WriteLine("=== Informace o scénáři ===");
            Console.WriteLine($"Verze: {data.Version}");
            Console.WriteLine($"Mapa: {data.Width}x{data.Height} buněk ({data.Width * data.Dx}x{data.Height * data.Dy} metrů)");
            Console.WriteLine($"Velikost buňky: {data.Dx}x{data.Dy} metrů");
            Console.WriteLine();
            Console.WriteLine($"Pozice děla: [{data.XS}, {data.YS}] = ({data.XS * data.Dx}m, {data.YS * data.Dy}m)");
            Console.WriteLine($"Výška terénu u děla: {data.Heights[data.YS, data.XS]}m");
            Console.WriteLine($"Nastavení děla: azimut={data.AS}°, zenit={data.ZS}°, rychlost={data.VS}m/s");
            Console.WriteLine();
            Console.WriteLine($"Pozice dronu: [{data.XT}, {data.YT}] = ({data.XT * data.Dx}m, {data.YT * data.Dy}m)");
            Console.WriteLine($"Výška terénu u dronu: {data.Heights[data.YT, data.XT]}m");
            Console.WriteLine();
            Console.WriteLine($"Vítr: [{data.WVx}, {data.WVy}, {data.WVz}] m/s");
            Console.WriteLine("===========================");
        }
    }
}
