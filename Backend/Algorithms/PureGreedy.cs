using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cardinal.Backend
{
    public class PureGreedy : ISolver
    {
        public List<LogEntry> MissionLog { get; set; } = new();
        public Result Solve()
        {
            return new Result();
        }
    }
}
