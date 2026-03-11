using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Cardinal.Views;

public partial class LoadingScreen : UserControl
{
    public static string RoverTime = "";
    public DashBoard? dashBoardInstance;
    public LoadingScreen()
    {
        InitializeComponent();
        RoverSolver.RunCompleted += IncrementProgress;
        ProgramEventManager.FileLoaded  += IncrementProgress;
        PETRendererController.MineralsLoaded  += IncrementProgress;

        Loaded += BeginLoad;
    }

    private void IncrementProgress(object? sender, EventArgs e)
    {
        LoadProgress.Value += 1;
        
        if (LoadProgress.Value == LoadProgress.Maximum) IsVisible = false;
        Dispatcher.UIThread.Post(() =>
        {
            if (LoadProgress.Value == LoadProgress.Maximum-1) dashBoardInstance?.Load3D(); 
            else if (LoadProgress.Value == LoadProgress.Maximum) IsVisible = false;
        }, DispatcherPriority.Render);
    }

    public void BeginLoad(object? sender, EventArgs e)
    {
        Loaded -= BeginLoad;
        RoverSolver.Run(["mars_map_50x50.csv", RoverTime]);
        Global.ProgramEventManager.LoadDataFromFile("mission_log.csv");
    }
}