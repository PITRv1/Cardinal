using System;
using Avalonia.Controls;
using Cardinal.Views;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cardinal.ViewModels;

public partial class DashBoardViewModel : ViewModelBase
{
    [ObservableProperty] MatrixMap matrixMapInstance = new();
    [ObservableProperty] RoverTab roverTabInstance = new();
    [ObservableProperty] LogicTab logicTabInstance = new();
    [ObservableProperty] LoggingTab loggingTabInstance = new();
    [ObservableProperty] MediaControls mediaControlsInstance = new();
}
