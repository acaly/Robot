using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibRobot.Graph
{
    public abstract class AbstractConnection
    {
        public ConnectionPoint A { get; internal set; }
        public ConnectionPoint B { get; internal set; }
        public string DisplayName { get; set; }

        internal AbstractConnection(ConnectionPoint a, ConnectionPoint b)
        {
            A = a;
            B = b;
            A.Connection = B.Connection = this;
        }
    }
}
