using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Cardinal.Backend;

namespace Cardinal.Views;

public partial class RoverTab : UserControl
{
    readonly string[] speedModeLabels = ["Slow", "Normal", "Fast"];
    Map map = new();

    public RoverTab()
    {
        InitializeComponent();

        Global.ProgramEventManager.StepDataSent += UpdateUI;

        LoadingScreen.LoadingCompleted += () => {
            map = Map.Load(RoverSolver.MapFileName);
            PlannedDistance.Content = GetTotalDistance();
            
            var mineralValues = UpdateMineralCount(Global.ProgramEventManager.stepDataList);
            
            PlannedYellowMineralsCount.Content =  mineralValues[0];
            PlannedGreenMineralsCount.Content = mineralValues[1];
            PlannedBlueMineralsCount.Content = mineralValues[2];
            PlannedTotalMineralsCount.Content = Global.ProgramEventManager.numberOfMinerals;
        };
    }

    private void UpdateUI(StepData stepData)
    {
        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentMaxDistance.Content = (int)stepData.batteryCharge / 2;

            BatteryPercentage.Content = stepData.batteryCharge + "%";
            BatteryUsage.Content = 2 * Math.Pow(stepData.speed, 2);
            BatteryCharging.Content = stepData.phase == DayPhase.DAY ? "Yes" : "No";
            BatteryTotalUsed.Content = GetTotalUsageAtTick(stepData.tick);

            XPosition.Content = stepData.position.X;
            YPosition.Content = stepData.position.Y;

            SpeedMode.Content = speedModeLabels[stepData.speed-1];
            BlockSpeed.Content = stepData.speed;

            CoveredDistance.Content = GetTotalDistanceAtTick(stepData.tick);

            var mineralValues = UpdateMineralCount(Global.ProgramEventManager.GetPreviousAndCurrentMiningSteps(stepData.tick));
            
            CurrentYellowMineralsCount.Content = mineralValues[0];
            CurrentGreenMineralsCount.Content = mineralValues[1];
            CurrentBlueMineralsCount.Content =mineralValues[2];
            CurrentTotalMineralsCount.Content = stepData.collectedMineralAmount;
        });
    }

    private int[] UpdateMineralCount(List<StepData> stepList)
    {
        int blueMineral = 0, yellowMineral = 0, greenMineral = 0;

        foreach (var step in stepList)
        {
            if (step.state != RoverState.MINING) continue;
            
            NodeBase node = map.WorldMap[(int)step.position.Y][(int)step.position.X];

            switch (node.Character)
            {
                case 'B':
                    blueMineral += 1;
                    break;
                case 'Y':
                    yellowMineral += 1;
                    break;
                case 'G':
                    greenMineral += 1;
                    break;
                default:
                    break;
            }
        }

        return [yellowMineral, greenMineral, blueMineral];
    }

    private int GetTotalUsageAtTick(int tick)
    {
        var prevSteps = Global.ProgramEventManager.stepDataList.Where(s => s.tick < tick);
        int total = 0;

        if (prevSteps == null) return total;

        foreach (var step in prevSteps) total += 100 - (int)step.batteryCharge;

        return total;
    }

    private int GetTotalDistanceAtTick(int tick)
    {
        var prevSteps = Global.ProgramEventManager.stepDataList.Where(s => s.tick < tick);
        int total = 0;

        if (prevSteps == null) return total;

        foreach (var step in prevSteps) total += step.speed;

        return total;
    }

    private int GetTotalDistance()
    {
        int total = 0;

        foreach (var step in Global.ProgramEventManager.stepDataList) total += step.speed;

        return total;
    }
}