using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace Cardinal
{
    public static class Global {
        public static ProgramEventManager ProgramEventManager = new();
        public static Grid DragDetector = new();
    }
}
