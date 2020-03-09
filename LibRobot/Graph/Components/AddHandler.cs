using LibRobot.Simulation;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph.Components
{
    class AddHandler : ISimulationHandler
    {
        public int GetStorageSize(ComponentSimulationEnvironment env)
        {
            var read1 = env.GetConnectionPointSize("read1");
            var read2 = env.GetConnectionPointSize("read2");
            if (read1 != read2)
            {
                throw new Exception();
            }
            return read1 + 1;
        }

        public unsafe void Read(ComponentSimulationEnvironment env)
        {
            var s = env.GetStorage();
            var tmp = stackalloc byte[s.Length];
            env.Read("read1", s);
            env.Read("read2", new Span<byte>(tmp, s.Length));

            var bitLen = env.GetConnectionPointSize("read1");
            var byteLen = (bitLen + 7) / 8;

            //Add byte by byte
            //TODO fast path for 8,16,32 bit?
            int carry = 0;
            for (int i = 0; i < byteLen; ++i)
            {
                AddByte(ref s[i], tmp[i], ref carry);
            }

            //s is 1 bit longer than input
            //We should continue wrtie if input is 8n bits
            if ((bitLen & 7) == 0)
            {
                s[s.Length - 1] = (byte)carry;
            }
        }

        private void AddByte(ref byte a, byte b, ref int c)
        {
            var r = (byte)(a + b + c);
            c = (byte)(b > 0 && r <= a ? 1 : 0);
            a = r;
        }

        public void Write(ComponentSimulationEnvironment env)
        {
            env.Write("write", env.GetStorage());
        }
    }
}
