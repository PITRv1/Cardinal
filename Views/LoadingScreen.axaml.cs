using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cardinal.Backend;
using Tmds.DBus.Protocol;


namespace Cardinal.Views;

public partial class LoadingScreen : UserControl
{
    public static event Action? LoadingCompleted;

    private string mapPath = "";
    private string time = "";

    public DashBoard? dashBoardInstance;
    public LoadingScreen()
    {
        InitializeComponent();
        RoverSolver.RunCompleted += IncrementProgress;
        ProgramEventManager.RouteFileLoaded  += IncrementProgress;
        ProgramEventManager.LogFileLoaded  += IncrementProgress;
        PETRendererController.MineralsLoaded  += IncrementProgress;

        MapFileButton.Click += OpenExolorerAndGetMapPath;
        StartButton.Click += InitiateLoad;
    }

    private async void OpenExolorerAndGetMapPath(object? sender, RoutedEventArgs? args)
    {
        var topLevel = TopLevel.GetTopLevel(this)!;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select and open map file",
            AllowMultiple = false
        });

        if (files.Count < 1) return;

        MapFilePathLabel.Content = files[0].Name;
        mapPath = files[0].Path.AbsolutePath.ToString();
    }

    private async void InitiateLoad(object? sender, RoutedEventArgs? args)
    {
        if (string.IsNullOrEmpty(mapPath) || !int.TryParse(TimeTextBox.Text, out _)) return;

        var time = TimeTextBox.Text;

        LoadGrid.IsVisible = true;
        InfoGrid.IsVisible = false;

        await Task.Run(() => RoverSolver.Run([mapPath, time, "--greedy-ga"]));
        Global.ProgramEventManager.LoadDataFromFile("mission_log.csv");
        Global.ProgramEventManager.LoadRouteFromFile("route.txt");
    }

    private void IncrementProgress()
    {
        Dispatcher.UIThread.Post(() =>
        {
            LoadProgress.Value += 1;

            if (LoadProgress.Value == LoadProgress.Maximum) IsVisible = false;

            if (LoadProgress.Value == LoadProgress.Maximum-1) dashBoardInstance?.Load3D(); 
            else if (LoadProgress.Value == LoadProgress.Maximum)  {
                IsVisible = false;
                LoadingCompleted?.Invoke();
                Global.ProgramEventManager.CurrentTick = 1;
            }
        }, DispatcherPriority.Render);
    }
}