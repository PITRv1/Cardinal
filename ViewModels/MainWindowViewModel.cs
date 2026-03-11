using Avalonia.Controls;
using Cardinal.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cardinal.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] LoadingScreen loadingScreenInstance = new();
    [ObservableProperty] DashBoard dashboardInstance = new();

    public MainWindowViewModel()
    {
        loadingScreenInstance.dashBoardInstance = dashboardInstance;
    }
}
