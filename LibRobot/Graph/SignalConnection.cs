using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class SignalConnection : AbstractConnection
    {
        internal SignalConnection(ConnectionPoint src, ConnectionPoint dest) : base(src, dest)
        {
        }
    }
}
