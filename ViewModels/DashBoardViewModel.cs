using System;
using Avalonia.Controls;
using Cardinal.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cardinal.ViewModels;

public partial class DashBoardViewModel : ViewModelBase
{
    [ObservableProperty] UserControl matrixMapInstance = new MatrixMap();
    [ObservableProperty] UserControl dayCycleVisualInstace = new DayCycleVisual();
}
