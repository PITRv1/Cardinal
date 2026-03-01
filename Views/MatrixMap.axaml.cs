using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Metadata;
using Cardinal.ViewModels;
using Tmds.DBus.Protocol;
using VadaszTest;

namespace Cardinal.Views;

public partial class MatrixMap : UserControl
{
    private Map map = new();
    private double minSizeValue;
    public MatrixMap()
    {
        InitializeComponent();
        DataContext = new MatrixMapViewModel();
        map.SetMap("./MateMagic/maps/mars_map_50x50.csv");
        
        SetupGrid();
        LoadData();
    }

    private void SetupGrid()
    {
        minSizeValue = 50;

        map.WorldMap[0].ForEach((idk) => MatrixMapGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Parse($"{minSizeValue}")}));
        map.WorldMap.ForEach((idk) => MatrixMapGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Parse($"{minSizeValue}")}));

        MatrixMapGrid.MaxWidth = MatrixMapGrid.ColumnDefinitions.Count * minSizeValue;
        MatrixMapGrid.MaxHeight = MatrixMapGrid.RowDefinitions.Count * minSizeValue;

        // minSizeValue = MatrixMapGrid.RowDefinitions.Count < MatrixMapGrid.ColumnDefinitions.Count ? MatrixMapGrid.Bounds.Height / MatrixMapGrid.ColumnDefinitions.Count : MatrixMapGrid.Bounds.Width / MatrixMapGrid.RowDefinitions.Count;
        // minSizeValue = minSizeValue < 40.0 ? 40.0 : minSizeValue;
    }

    private void LoadData()
    {
        foreach (var currentWorldRow in map.WorldMap)
        {
            foreach (var node in currentWorldRow)
            {
                Label nodeUIElement = new Label
                {
                    Content = node.Character == '.' ? "" : node.Character,
                    Foreground = new SolidColorBrush { Color = Colors.White },
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    FontSize = minSizeValue * 0.75
                };

                switch (node.Character)
                {
                    case '#':
                        nodeUIElement.Foreground = new SolidColorBrush{Color = Colors.Red};
                        break;
                    case 'B':
                        nodeUIElement.Foreground = new SolidColorBrush{Color = Colors.MediumBlue};
                        break;
                    case 'Y':
                        nodeUIElement.Foreground = new SolidColorBrush{Color = Colors.Yellow};
                        break;
                    case 'G':
                        nodeUIElement.Foreground = new SolidColorBrush{Color = Colors.Green};
                        break;
                }

                Grid.SetColumn(nodeUIElement, (int)node.Coords.X);
                Grid.SetRow(nodeUIElement, (int)node.Coords.Y);

                MatrixMapGrid.Children.Add(nodeUIElement);
            }
            
        }
    }
}