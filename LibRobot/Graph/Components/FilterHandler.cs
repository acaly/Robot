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
            var tmp = stackalloc uint[len];
            var span = new Span<uint>(tmp, len);
            span.Fill(0);
            env.Read("read", MemoryMarshal.Cast<uint, byte>(span));

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

    internal class FilterHandlerAll : ISimulationHandler
    {
        public int GetStorageSize(ComponentSimulationEnvironment env)
        {
            return 0;
        }

        public unsafe void Read(ComponentSimulationEnvironment env)
        {
            var bitLen = env.GetConnectionPointSize("read");
            var len = (bitLen + 31) / 32;
            var tmp = stackalloc uint[len];
            var span = new Span<uint>(tmp, len);
            span.Fill(0);
            env.Read("read", MemoryMarshal.Cast<uint, byte>(span));

            for (int i = 0; i < len - 1; ++i)
            {
                if (tmp[i] != uint.MaxValue)
                {
                    return;
                }
            }
            var lastBit = bitLen - (len - 1) * 32;
            if (tmp[len - 1] != ~(uint.MaxValue << lastBit))
            {
                return;
            }
            env.Trigger("signalOut");
        }

        public void Write(ComponentSimulationEnvironment env)
        {
        }
    }
}
