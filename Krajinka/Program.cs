using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace Krajinka
{
    /// <summary>
    /// Vstupní bod aplikace, který vytvoří a spustí herní okno.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Inicializuje nastavení okna a spustí hlavní smyčku aplikace.
        /// </summary>
        /// <param name="args">Argumenty příkazové řádky.</param>
        static void Main(string[] args)
        {
            string selectedMapPath = ResolveSelectedMapPath();

            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(800, 600),
                Title = "Semestrální práce - Krajinka",
                APIVersion = new Version(3, 3),
            };

            using (Window window = new Window(GameWindowSettings.Default, nativeWindowSettings, selectedMapPath))
            {
                window.Run();
            }
        }

        /// <summary>
        /// Vybere cestu k mapě z konzolového menu.
        /// </summary>
        /// <returns>Relativní cesta k mapě.</returns>
        private static string ResolveSelectedMapPath()
        {

            List<string> availableMaps = GetAvailableMaps();
            if (availableMaps.Count == 0)
            {
                throw new Exception("Error loading maps");
            }

            if (availableMaps.Count == 1)
            {
                return availableMaps[0];
            }

            Console.WriteLine("Vyber mapu:");
            for (int i = 0; i < availableMaps.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(availableMaps[i])}");
            }

            Console.Write("Zadej číslo mapy (Enter = 1): ");
            string? input = Console.ReadLine();

            if (int.TryParse(input, out int selectedIndex))
            {
                if (selectedIndex >= 1 && selectedIndex <= availableMaps.Count)
                {
                    return availableMaps[selectedIndex - 1];
                }
                else throw new Exception("Invalid index");
            }

            return availableMaps[0];
        }

        /// <summary>
        /// Vrátí dostupné PNG mapy z adresáře Data/maps.
        /// </summary>
        /// <returns>Seznam relativních cest k mapám.</returns>
        private static List<string> GetAvailableMaps()
        {
            List<string> result = new List<string>();

            string mapsDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "maps");
            if (!Directory.Exists(mapsDirectory))
            {
                throw new Exception("Error loading maps");
            }

            string[] files = Directory.GetFiles(mapsDirectory, "*.png", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < files.Length; i++)
            {
                result.Add(Path.Combine("Data", "maps", Path.GetFileName(files[i])));
            }

            return result;
        }
    }
}