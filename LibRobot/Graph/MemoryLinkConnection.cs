using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class MemoryLinkConnection : AbstractConnection
    {
        internal MemoryLinkConnection(ConnectionPoint a, ConnectionPoint b) : base(a, b)
        {
        }
    }
}
