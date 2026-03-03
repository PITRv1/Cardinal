using System;
using Avalonia;
using Avalonia.Media;
using VadaszTest;

static class Utility
{
    public static T GetResourceByName<T>(string name)
    {
        return (T)Application.Current!.Resources[name]!;
    }
}