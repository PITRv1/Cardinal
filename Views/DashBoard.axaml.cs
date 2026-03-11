using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Cardinal.ViewModels;
using SixLabors.ImageSharp.Processing;
using System.Linq;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Cardinal.Views;

public partial class DashBoard : UserControl
{
    public DashBoard()
    {
        InitializeComponent();
        DataContext = new DashBoardViewModel();
        Global.DragDetector = PointerCathcer;
    }

    public void Load3D()
    {
        var petRenderer = new PETRendererController();
        PETrendererContainer.GetObservable(BoundsProperty).Subscribe(new AnonymousObserver<Rect>(bounds =>
        {
            petRenderer.Width = bounds.Width;
            petRenderer.Height = bounds.Height;
        }));

        PETrendererContainer.Children.Add(petRenderer);
    }
}