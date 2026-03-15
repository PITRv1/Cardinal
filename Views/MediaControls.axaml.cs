using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Cardinal.Views;

public partial class MediaControls : UserControl
{
    private bool timeSliderAutoUpdate = false;
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
            timeSliderAutoUpdate = true;
            TimeSlider.Value = data.tick;
            timeSliderAutoUpdate = false;
        });
    }

    private void PlayOrStopRollBack(object? sender, PointerPressedEventArgs e)
    {
        Global.ProgramEventManager.ToggleTickTimer();
    }

    private void SkipInTime(object? sender, PointerPressedEventArgs e)
    {
        timeSliderAutoUpdate = true;
        if (sender is Button button)
        {
            if (button.Name == BackwardButton.Name) Global.ProgramEventManager.CurrentTick -= 1;
            else if (button.Name == ForwardButton.Name) Global.ProgramEventManager.CurrentTick += 1;
        }
        timeSliderAutoUpdate = false;
    }

    private void InitializeUI()
    {
        timeSliderAutoUpdate = true;

        UpdateUI();

        TimeSlider.Maximum = Global.ProgramEventManager.GetTickCount();
        TimeSlider.Minimum = 1;
        TimeSlider.Value = TimeSlider.Minimum;
        TimeSlider.SmallChange = 1;
        TimeSlider.LargeChange = 1;

        timeSliderAutoUpdate = false;
    }

    private void UpdateUI(object? sender = null, EventArgs? e = null)
    {
        TickTime.Content = $"{Math.Floor(TimeSlider.Value)}:{Global.ProgramEventManager.GetTickCount()}";

        if (!timeSliderAutoUpdate) Global.ProgramEventManager.CurrentTick = (int)TimeSlider.Value;
    }
}