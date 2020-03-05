using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class MemoryAccessConnection : AbstractConnection
    {
        internal MemoryAccessConnection(ConnectionPoint src, ConnectionPoint dest) : base(src, dest)
        {
        }
    }
}
