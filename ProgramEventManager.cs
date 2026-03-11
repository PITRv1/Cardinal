using Avalonia.Controls;
using PETRenderer;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static event EventHandler<EventArgs>? FileLoaded;

        public event Action<StepData>? StepDataSent;

        private int numberOfMinerals = 0;
        private List<StepData> stepDataList = [];

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

            FileLoaded?.Invoke(null, EventArgs.Empty);
        }

        private StepData GetStepDataAtTick(int tick) {
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
