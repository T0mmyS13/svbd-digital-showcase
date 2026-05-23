using Avalonia;
using System;

namespace DroneWar
{

    internal class Program
    {
        /// <summary>
        /// Číslo vybraného scénáře.
        /// </summary>
        public static int SelectedScenario;
        
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int scenario))
            {
                SelectedScenario = scenario;
            }
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }


        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()    
                .WithInterFont()       
                .LogToTrace();          
        }
    }
}
