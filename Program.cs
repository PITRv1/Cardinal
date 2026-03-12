using Avalonia;
using Cardinal.Backend;
using Cardinal.Views;
using System;

namespace Cardinal;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.


    [STAThread]
    public static void Main(string[] args) {
        if (args[0] == "-ui")
        {
            LoadingScreen.RoverTime = args[1];

            BuildAvaloniaApp().StartWithClassicDesktopLifetime([]);
        }
        else if (args.Length >= 1)
        {
            switch (args[0])
            {
                case "--map-editor":
                    MapEditor mapEditor = new MapEditor();
                    mapEditor.Loop();
                    break;
                case "--greedy-ga":
                    RoverSolver.Run(["mars_map_50x50.csv", args[1], args[0]]);
                    break;
                case "--ai-solver":
                    RoverSolver.Run(["mars_map_50x50.csv", args[1], args[0]]);
                    break;
                //case "--greedy-ga":
                //    RoverSolver.Run(["mars_map_50x50.csv", args[0], "greedyGA"]);
                //    break;
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
