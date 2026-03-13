using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Cardinal.Views;

public partial class MediaControls : UserControl
{
    public MediaControls()
    {
        InitializeComponent();
        ProgramEventManager.LogFileLoaded += InitializeUI;
        Global.ProgramEventManager.StepDataSent += UpdateSliderValue;
        TimeSlider.ValueChanged += UpdateUI;

        BackwardButton.AddHandler(PointerPressedEvent, SkipInTime, handledEventsToo: true);
        ForwardButton.AddHandler(PointerPressedEvent, SkipInTime, handledEventsToo: true);
        PlayButton.AddHandler(PointerPressedEvent, PlayOrStopRollBack, handledEventsToo: true);
    }

    private void UpdateSliderValue(StepData data)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            TimeSlider.Value = data.tick;
        });
    }

    private void PlayOrStopRollBack(object? sender, PointerPressedEventArgs e)
    {
        Global.ProgramEventManager.ToggleTickTimer();
    }

    private void SkipInTime(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Button button)
        {
            if (button.Name == BackwardButton.Name) TimeSlider.Value -= 1;
            else if (button.Name == ForwardButton.Name) TimeSlider.Value += 1;
        }
    }

    private void InitializeUI()
    {
        UpdateUI();

        TimeSlider.Maximum = Global.ProgramEventManager.GetTickCount();
        TimeSlider.Minimum = 0;
        TimeSlider.Value = TimeSlider.Minimum;
        TimeSlider.SmallChange = 1;
        TimeSlider.LargeChange = 1;
    }

    private void UpdateUI(object? sender = null, EventArgs? e = null)
    {
        TickTime.Content = $"{Math.Floor(TimeSlider.Value)}:{Global.ProgramEventManager.GetTickCount()}";

        if (Global.ProgramEventManager.CurrentTick != (int)TimeSlider.Value) Global.ProgramEventManager.CurrentTick = (int)TimeSlider.Value;
    }
}