using Avalonia;
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
        if  (args[0] == "-ui") {
            LoadingScreen.RoverTime = args[1];

            BuildAvaloniaApp().StartWithClassicDesktopLifetime([]);
        }
        else if (args.Length >= 1) RoverSolver.Run(new string[] { "mars_map_50x50.csv", args[0] });
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
