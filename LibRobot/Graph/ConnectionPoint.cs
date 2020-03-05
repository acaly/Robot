using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class ConnectionPoint
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public AbstractComponent Component { get; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ProgramInterface Interface { get; }

        public object ParentObject => (object)Component ?? Interface;

        public string Name { get; }

        public ConnectionPointType Type { get; }

        public int BitOffset { get; }
        public int BitLength { get; }

        public AbstractConnection Connection { get; internal set; }

        public ConnectionPoint(AbstractComponent component, string name, ConnectionPointType type)
            : this(component, name, type, -1, -1)
        {
        }

        public ConnectionPoint(AbstractComponent component, string name, ConnectionPointType type, int bitOffset, int bitLength)
        {
            Component = component;
            Name = name;
            Type = type;
            BitOffset = bitOffset;
            BitLength = bitLength;
        }

        public ConnectionPoint(ProgramInterface programInterface, string name, ConnectionPointType type)
            : this(programInterface, name, type, -1)
        {
        }

        public ConnectionPoint(ProgramInterface programInterface, string name, ConnectionPointType type, int bitLength)
        {
            Interface = programInterface;
            Name = name;
            Type = type;
            BitOffset = -1;
            BitLength = bitLength;
        }
    }
}
