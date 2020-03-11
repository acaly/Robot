using LibRobot.Simulation;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph.Components
{
    internal class XorHandler : ISimulationHandler
    {
        public int GetStorageSize(ComponentSimulationEnvironment env)
        {
            var read1 = env.GetConnectionPointSize("read1");
            var read2 = env.GetConnectionPointSize("read2");
            if (read1 != read2)
            {
                throw new Exception();
            }
            return read1;
        }

        public unsafe void Read(ComponentSimulationEnvironment env)
        {
            var s = env.GetStorage();
            var tmp = stackalloc byte[s.Length];
            s[s.Length - 1] = 0;
            env.Read("read1", s);
            env.Read("read2", new Span<byte>(tmp, s.Length));

            var bitLen = env.GetConnectionPointSize("read1");
            var byteLen = (bitLen + 7) / 8;

            for (int i = 0; i < byteLen; ++i)
            {
                s[i] ^= tmp[i];
            }
        }

        public void Write(ComponentSimulationEnvironment env)
        {
            env.Write("write", env.GetStorage());
        }
    }
}
