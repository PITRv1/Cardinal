using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Cardinal.ViewModels;
using SixLabors.ImageSharp.Processing;

namespace Cardinal.Views;

public partial class DashBoard : UserControl
{
    public DashBoard()
    {
        InitializeComponent();
        DataContext = new DashBoardViewModel();
        Global.DragDetector = PointerCathcer;
    }
}