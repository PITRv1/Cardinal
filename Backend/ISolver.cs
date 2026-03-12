using System.Collections.Generic;

namespace Cardinal.Backend
{
    public interface ISolver
    {
        public List<LogEntry> MissionLog { get; set; }
        public Result Solve();
    }
}
