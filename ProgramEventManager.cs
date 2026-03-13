using Avalonia.Controls;
using PETRenderer;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cardinal.Backend;
using System.Timers;
using Cardinal.Views;
using System.Data.Common;

namespace Cardinal
{
    public enum DayPhase {
        DAY,
        NIGHT
    }
    public enum RoverState
    {
        MINING,
        NAVIGATING,
        RETURNING
    }


    public struct StepData
    {
        public int tick;
        public float time;
        public DayPhase phase;
        public Vector2 position;
        public float batteryCharge;
        public int speed;
        public RoverState state;
        public int collectedMineralAmount;
    }

    public class ProgramEventManager
    {
        public static event Action? LogFileLoaded;
        public static event Action? RouteFileLoaded;
        public event Action<StepData>? StepDataSent;
        public List<StepData> stepDataList {private set; get;} = new();
        public List<Vector2> roverRoute {private set; get;} = new();

        public int _currentTick = 1;
        public int CurrentTick
        {
            set
            {
                _currentTick = Math.Clamp(value, 1, GetTickCount());
            }
            get
            {
                return _currentTick;
            }
        }
        public Timer TickTimer {private set; get;} = new Timer {Interval=500, AutoReset=false};

        private int numberOfMinerals = 0;

        public ProgramEventManager()
        {
            TickTimer.Elapsed += TimerFinishedHandler;
        }

        private void TimerFinishedHandler(object? sender, ElapsedEventArgs e)
        {
            try
            {
                SendUpdateEvent(CurrentTick);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }

            if (_currentTick == GetTickCount()) return;
            _currentTick += 1;
            TickTimer.Start();
        }

        public void ToggleTickTimer()
        {
            if (TickTimer.Enabled) TickTimer.Stop();
            else TickTimer.Start();
        }

        public void LoadDataFromFile (string dataFile, bool skipFirstLine = true) {
            List<string> data = File.ReadAllLines(dataFile).ToList();
            if (skipFirstLine) data.RemoveAt(0);

            numberOfMinerals = int.Parse(data[0].Split(';')[6]);

            foreach (string line in data) {
                string[] elements = line.Split(';');

                StepData currentStepData = new StepData {
                    tick = int.Parse(elements[0]),
                    time = float.Parse(elements[1]),
                    phase = elements[8] switch {
                        "day" => DayPhase.DAY,
                        "night" => DayPhase.NIGHT,
                        _ => throw new Exception($"Unknown day phase: {elements[8]}")
                    },
                    position = new Vector2(float.Parse(elements[2]), float.Parse(elements[3])),
                    batteryCharge = float.Parse(elements[5]),
                    speed = int.Parse(elements[4]),
                    state = elements[7] switch {
                        "Mining" => RoverState.MINING,
                        "Navigating" => RoverState.NAVIGATING,
                        "Returning" => RoverState.RETURNING,
                        _ => throw new Exception($"Unknown rover state: {elements[7]}")
                    },
                    collectedMineralAmount = int.Parse(elements[6])
                };

                stepDataList.Add(currentStepData);
            };

            LogFileLoaded?.Invoke();
        }

        public void LoadRouteFromFile (string dataFile, bool skipFirstLine = true, char splitChar = ',') {
            List<string> data = File.ReadAllLines(dataFile).ToList();
            if (skipFirstLine) data.RemoveAt(0);

            foreach (string line in data)
            {
                string[] rawPositionData = line.Split(splitChar);
                roverRoute.Add(new Vector2(float.Parse(rawPositionData[0]), float.Parse(rawPositionData[1])));
            }

            RouteFileLoaded?.Invoke();
        }

        public List<Vector2> GetRouteCoveredAtTick(int tick)
        {
            var stepAtTick = GetStepDataAtTick(tick);
            List<Vector2> coveredRoute = new();

            foreach (Vector2 position in roverRoute)
            {
                if (position.X == stepAtTick.position.X && position.Y == stepAtTick.position.Y) break;
                coveredRoute.Add(position);
            }

            return coveredRoute;
        }

        private StepData GetStepDataAtTick(int tick) 
        {
            return stepDataList.FirstOrDefault(s => s.tick == tick);
        }

        public int GetTickCount() {
            return stepDataList.Count;
        }

        /// <summary>
        /// Fires the event loaded with the StepData that happened on the specified tick
        /// </summary>
        /// <param name="tick"></param>
        public void SendUpdateEvent(int tick) {
            StepDataSent?.Invoke(GetStepDataAtTick(tick));
        }

    }
}
