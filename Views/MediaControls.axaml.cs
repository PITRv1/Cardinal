using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cardinal.Views;

public partial class MediaControls : UserControl
{
    public MediaControls()
    {
        InitializeComponent();

        // TickTime.Content = $"{TimeSlider.Value}:{Global.ProgramEventManager.GetTickCount()}";
    }
}