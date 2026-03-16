using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Cardinal.Views;

public partial class RoverTab : UserControl
{
    public RoverTab()
    {
        InitializeComponent();

        Global.ProgramEventManager.StepDataSent += UpdateUI;
    }

    private void UpdateUI(StepData stepData)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            BatteryPercentage.Content = $"{stepData.batteryCharge}%";
            BatteryUsage.Content = stepData.speed;
            BatteryCharging.Content = stepData.phase == DayPhase.DAY ? "Yes" : "No";
            BatteryTotalUsed.Content = GetTotalUsageAtTick(stepData.tick).ToString();
        });
    }

    private int GetTotalUsageAtTick(int tick)
    {
        var prevSteps = Global.ProgramEventManager.stepDataList.Where(s => s.tick < tick);
        int total = 0;

        if (prevSteps == null) return total;

        foreach (var step in prevSteps) total += 100 - (int)step.batteryCharge;

        return total;
    }
}