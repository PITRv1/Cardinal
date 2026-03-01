using Avalonia.Controls;
using Cardinal.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cardinal.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] UserControl dashBoardInstance = new DashBoard();
}
