using LibRobot.Simulation;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibRobot.Graph.Components
{
    internal class FilterHandlerAny : ISimulationHandler
    {
        public int GetStorageSize(ComponentSimulationEnvironment env)
        {
            return 0;
        }

        public unsafe void Read(ComponentSimulationEnvironment env)
        {
            var len = (env.GetConnectionPointSize("read") + 31) / 32;
            var tmp = stackalloc int[len];
            var span = new Span<int>(tmp, len);
            span.Fill(0);
            env.Read("read", MemoryMarshal.Cast<int, byte>(span));

            for (int i = 0; i < len; ++i)
            {
                if (tmp[i] != 0)
                {
                    env.Trigger("signalOut");
                    break;
                }
            }
        }

        public void Write(ComponentSimulationEnvironment env)
        {
        }
    }
}
