using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Cardinal.ViewModels;

namespace Cardinal.Views;

public partial class DashBoard : UserControl
{
    public DashBoard()
    {
        InitializeComponent();
        DataContext = new DashBoardViewModel();
        Global.PETrendererMovementHandler = PETrendererMovementHandler;
    }
}