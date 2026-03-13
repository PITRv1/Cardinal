using System;
using System.Collections.Generic;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Cardinal.Views;

public partial class LoggingTab : UserControl
{
    static StackPanel? loggingStackPanel;
    static List<Label> savedLogs = new();

    public LoggingTab()
    {
        InitializeComponent();
        loggingStackPanel = LoggingStackPanel;
        if (savedLogs.Count > 0) LoadSavedLogs();
    }

    private void LoadSavedLogs()
    {
        foreach (var log in savedLogs)
        {
            LoggingStackPanel.Children.Add(log);
        }
        savedLogs.Clear();
        WriteLine("\nLOADED SAVED LOGS\n", false);
    }

    public static void WriteLine(string message, bool printToConsole = true)
    {
        CreateNewRegistry(message, "White", printToConsole);
    }

    public static void WriteSuccess(string message, bool printToConsole = true)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        CreateNewRegistry(message, "LightGreen", printToConsole);
        Console.ResetColor();
    }

    public static void WriteError(string message, bool printToConsole = true)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        CreateNewRegistry(message, "Red", printToConsole);
        Console.ResetColor();
    }

    public static void WriteWarning(string message, bool printToConsole = true)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        CreateNewRegistry(message, "Yellow", printToConsole);
        Console.ResetColor();
    }

    private static void CreateNewRegistry(string message, string colorResourceName, bool printToConsole)
    {
        if (printToConsole) Console.WriteLine(message);

        var Registry = new Label
        {
            Foreground = Utility.GetResourceByName<IBrush>(colorResourceName),
            Content = message
        };

        if (loggingStackPanel == null)
        {
            savedLogs.Add(Registry);
            return;
        }

        loggingStackPanel.Children.Add(Registry);
    }
}