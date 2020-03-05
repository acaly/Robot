using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class Program
    {
        internal ProgramModule Module { get; }

        private Program(ProgramModule module)
        {
            Module = module;
        }

        internal static Program Create(ProgramModule module)
        {
            return new Program(module);
        }

        internal static bool CheckModule(ProgramModule module)
        {
            if (module.Interfaces.Count != 1)
            {
                return false;
            }
            var ii = module.Interfaces.First();
            if (ii.Name != "entry")
            {
                return false;
            }
            if (ii.ConnectionPoints.Count != 2)
            {
                return false;
            }
            var cp = ii.ConnectionPoints.ToArray();
            if (cp[0].Type != ConnectionPointType.SignalSend ||
                cp[1].Type != ConnectionPointType.SignalSend)
            {
                return false;
            }
            if (cp[0].Name != "init" && cp[1].Name != "init" ||
                cp[0].Name != "tick" && cp[1].Name != "tick")
            {
                return false;
            }
            return true;
        }
    }
}
