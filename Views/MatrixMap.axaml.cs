using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cardinal.ViewModels;
using Cardinal.Backend;
using System.Linq;
using Avalonia.Dialogs.Internal;
using System.Collections.Generic;

namespace Cardinal.Views;

public partial class MatrixMap : UserControl
{
    const double LeastSize = 10.0;
    Map map = new();
    Border? previousPositionMarker;
    double minSizeValue;
    Vector2 borderSize;

    public MatrixMap()
    {
        InitializeComponent();
        DataContext = new MatrixMapViewModel();

        LoadingScreen.LoadingCompleted += () => {
            SetupGrid();
            InitializeMapData();
        };

        Global.ProgramEventManager.StepDataSent += UpdateMapData;
    }

    private void UpdateMapData(StepData stepData)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            ShowCoveredRoute(Global.ProgramEventManager.GetRouteCoveredAtTick(stepData.tick));
            ShowCurrentPosition(stepData.position);
        });
    }

    private void ShowCoveredRoute(List<Vector2> positions)
    {
        OverlayMatrixMapGrid.Children.Clear();

        foreach (var position in positions)
        {
            var borderElement = new Border
            {
                Background = MatrixMapGrid.Background,
                BorderBrush = new SolidColorBrush { Color = Colors.Green},
                BorderThickness = Thickness.Parse("1")
            };

            Grid.SetRow(borderElement, (int)position.Y);
            Grid.SetColumn(borderElement, (int)position.X);

            OverlayMatrixMapGrid.Children.Add(borderElement);
        }
    }

    private void ShowCurrentPosition(Vector2 position)
    {
        if (previousPositionMarker != null)
        {
            MatrixMapGrid.Children.Remove(previousPositionMarker);
        }

        Label currentPosLabel = CreatePositionMarkerNode("!", position);
        currentPosLabel.LayoutUpdated += OnCurrentPosLabelLayoutUpdated;
        
        previousPositionMarker = (Border?)currentPosLabel.Parent;
    }

    private void SetupGrid()
    {
        map = Map.Load(RoverSolver.MapFileName);

        MatrixMapGrid.ColumnDefinitions.Clear();
        MatrixMapGrid.RowDefinitions.Clear();
        MatrixMapGrid.Children.Clear();

        map.WorldMap[0].ForEach((idk) => 
        {
            MatrixMapGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Parse("1*")});
            // OverlayMatrixMapGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Parse($"1*")});
        });

        map.WorldMap.ForEach((idk) =>
        {
            MatrixMapGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Parse("1*")});
            // OverlayMatrixMapGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Parse($"1*")});
        });

        minSizeValue = MatrixMapGrid.RowDefinitions.Count < MatrixMapGrid.ColumnDefinitions.Count ? MatrixMapGrid.Bounds.Height / MatrixMapGrid.ColumnDefinitions.Count : MatrixMapGrid.Bounds.Width / MatrixMapGrid.RowDefinitions.Count;
        minSizeValue = minSizeValue < LeastSize ? LeastSize : minSizeValue;
    }

    private void InitializeMapData()
    {
        MatrixMapGrid.Children.Clear();

        foreach (var currentWorldRow in map.WorldMap)
        {
            foreach (var node in currentWorldRow)
            {
                Label labelElement = CreateMatrixNode(node);

                switch (node.Character)
                {
                    case '#':
                        labelElement.Foreground = Utility.GetResourceByName<IBrush>("VomitGreen");
                        break;
                    case 'B':
                        labelElement.Foreground = Utility.GetResourceByName<IBrush>("Blue");
                        break;
                    case 'Y':
                        labelElement.Foreground = Utility.GetResourceByName<IBrush>("Yellow");
                        break;
                    case 'G':
                        labelElement.Foreground = Utility.GetResourceByName<IBrush>("LightGreen");
                        break;
                    case 'S':
                        labelElement.Foreground = Utility.GetResourceByName<IBrush>("Red");
                        ShowCurrentPosition(node.Coords);
                        break;
                }
            }
        }

        MatrixMapGrid.LayoutUpdated += SaveBorderSize;
    }

    private void SaveBorderSize(object? s, EventArgs a)
    {
        MatrixMapGrid.LayoutUpdated -= SaveBorderSize;

        Border borderChildSample = (Border)MatrixMapGrid.Children.First();
        borderSize = new Vector2((float)borderChildSample.Bounds.Width, (float)borderChildSample.Bounds.Height);

        OverlayMatrixMapGrid.ColumnDefinitions.Clear();
        OverlayMatrixMapGrid.RowDefinitions.Clear();

        map.WorldMap[0].ForEach(_ => 
            OverlayMatrixMapGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(borderSize.X) }));
        map.WorldMap.ForEach(_ => 
            OverlayMatrixMapGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(borderSize.Y) }));

        // Pin the overlay to exactly the base grid's size
        OverlayMatrixMapGrid.Width = MatrixMapGrid.Bounds.Width;
        OverlayMatrixMapGrid.Height = MatrixMapGrid.Bounds.Height;
        OverlayMatrixMapGrid.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        OverlayMatrixMapGrid.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
    }

    private Label CreateMatrixNode(NodeBase node)
    {
        var borderElement = new Border
        {
            BorderBrush = new SolidColorBrush { Color = Colors.Green},
            BorderThickness = Thickness.Parse("1")
        };

        var labelElement = new Label
        {
            Content = node.Character == '.' ? "#" : node.Character.ToString().ToUpper(),
            Foreground = new SolidColorBrush { Color = node.Character == '.' ? Colors.Transparent : Colors.White },
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            FontSize = minSizeValue * 1.1f
        };


        Grid.SetColumn(borderElement, (int)node.Coords.X);
        Grid.SetRow(borderElement, (int)node.Coords.Y);

        borderElement.Child = labelElement;
        MatrixMapGrid.Children.Add(borderElement);

        return labelElement;
    }

    private Label CreatePositionMarkerNode(string text, Vector2 position)
    {
        var borderElement = new Border
        {
            BorderBrush = new SolidColorBrush { Color = Colors.Green},
            BorderThickness = Thickness.Parse("1"),
            Background = Utility.GetResourceByName<IBrush>("Red")
        };

        var labelElement = new Label
        {
            Content = text.ToUpper(),
            Foreground = new SolidColorBrush { Color = Colors.Black},
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            FontSize = minSizeValue * 1.1f
        };

        Grid.SetColumn(borderElement, (int)position.X);
        Grid.SetRow(borderElement, (int)position.Y);

        borderElement.Child = labelElement;
        OverlayMatrixMapGrid.Children.Add(borderElement);

        return labelElement;
    }

    private void OnCurrentPosLabelLayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is not Label label) return;
        label.LayoutUpdated -= OnCurrentPosLabelLayoutUpdated;

        var pos = label.TranslatePoint(new Avalonia.Point(0, 0), MatrixMapGrid);
        if (!pos.HasValue) return;

        var offsetX = pos.Value.X - MatrixScrollViewer.Viewport.Width / 2;
        var offsetY = pos.Value.Y - MatrixScrollViewer.Viewport.Height / 2;

        MatrixScrollViewer.Offset = new Vector(
            Math.Max(0, offsetX),
            Math.Max(0, offsetY)
        );
    }
}