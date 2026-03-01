using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cardinal.ViewModels;

namespace Cardinal.Views;

public partial class DayCycleVisual : UserControl
{
    public DayCycleVisual()
    {
        InitializeComponent();
        DataContext = new DayCycleVisualViewModel();
    }
}