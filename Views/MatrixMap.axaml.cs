using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Cardinal.ViewModels;
using Tmds.DBus.Protocol;

namespace Cardinal.Views;

public partial class MatrixMap : UserControl
{
    private Map map = new();
    const double LeastSize = 10.0;
    private double minSizeValue;
    public MatrixMap()
    {
        InitializeComponent();
        DataContext = new MatrixMapViewModel();

        
        // map = Map.Load("./MateMagic/maps/mars_map_50x50.csv");

        // Console.WriteLine(map.WorldMap[2][4].Coords);

        // LayoutUpdated += OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
        SetupGrid();
        LoadData();
    }

    private void SetupGrid()
    {
        map.WorldMap[0].ForEach((idk) => MatrixMapGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Parse($"1*")}));
        map.WorldMap.ForEach((idk) => MatrixMapGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Parse($"1*")}));

        minSizeValue = MatrixMapGrid.RowDefinitions.Count < MatrixMapGrid.ColumnDefinitions.Count ? MatrixMapGrid.Bounds.Height / MatrixMapGrid.ColumnDefinitions.Count : MatrixMapGrid.Bounds.Width / MatrixMapGrid.RowDefinitions.Count;
        minSizeValue = minSizeValue < LeastSize ? LeastSize : minSizeValue;

        // MatrixMapGrid.ColumnDefinitions.Clear();
        // MatrixMapGrid.RowDefinitions.Clear();

        // map.WorldMap[0].ForEach((idk) => MatrixMapGrid.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Parse($"{minSizeValue}")}));
        // map.WorldMap.ForEach((idk) => MatrixMapGrid.RowDefinitions.Add(new RowDefinition{Height=GridLength.Parse($"{minSizeValue}")}));
    }

    private void LoadData()
    {
        foreach (var currentWorldRow in map.WorldMap)
        {
            foreach (var node in currentWorldRow)
            {
                Border itemBorder = new Border
                {
                    BorderBrush = new SolidColorBrush{Color=Colors.Green},
                    BorderThickness = Thickness.Parse("1.5")
                };

                Label nodeUIElement = new Label
                {
                    Content = node.Character == '.' ? "#" : node.Character.ToString().ToUpper(),
                    Foreground = new SolidColorBrush { Color = node.Character == '.' ? Colors.Transparent : Colors.White },
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    FontSize = minSizeValue * .9f
                };

                switch (node.Character)
                {
                    case '#':
                        nodeUIElement.Foreground = Utility.GetResourceByName<IBrush>("VomitGreen");
                        break;
                    case 'B':
                        nodeUIElement.Foreground = Utility.GetResourceByName<IBrush>("Blue");
                        break;
                    case 'Y':
                        nodeUIElement.Foreground = Utility.GetResourceByName<IBrush>("Yellow");
                        break;
                    case 'G':
                        nodeUIElement.Foreground = Utility.GetResourceByName<IBrush>("LightGreen");
                        break;
                    case 'S':
                        nodeUIElement.Foreground = Utility.GetResourceByName<IBrush>("Red");
                        break;
                }

                itemBorder.Child = nodeUIElement;


                Grid.SetColumn(itemBorder, (int)node.Coords.X);
                Grid.SetRow(itemBorder, (int)node.Coords.Y);

                MatrixMapGrid.Children.Add(itemBorder);
            }
        }
    }
}