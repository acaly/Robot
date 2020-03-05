using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class ProgramInterface
    {
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(DebugDisplayHelper<ConnectionPoint>))]
        public class ConnectionPointCollection : ChildrenCollection<ConnectionPoint>
        {
            private readonly ProgramInterface _parent;

            internal ConnectionPointCollection(ProgramInterface parent)
            {
                _parent = parent;
            }

            public ConnectionPoint AddMemoryLink(string name, int bitLength)
            {
                var ret = new ConnectionPoint(_parent, name, ConnectionPointType.MemoryLink, bitLength);
                Add(ret);
                return ret;
            }

            public ConnectionPoint AddMemorySender(string name, int bitLength)
            {
                var ret = new ConnectionPoint(_parent, name, ConnectionPointType.MemorySend, bitLength);
                Add(ret);
                return ret;
            }

            public ConnectionPoint AddMemoryReceiver(string name, int bitLength)
            {
                var ret = new ConnectionPoint(_parent, name, ConnectionPointType.MemoryReceive, bitLength);
                Add(ret);
                return ret;
            }

            public ConnectionPoint AddSignalSender(string name)
            {
                var ret = new ConnectionPoint(_parent, name, ConnectionPointType.SignalSend);
                Add(ret);
                return ret;
            }

            public ConnectionPoint AddSignalReceiver(string name)
            {
                var ret = new ConnectionPoint(_parent, name, ConnectionPointType.SignalReceive);
                Add(ret);
                return ret;
            }

            protected override void BeforeRemove(ConnectionPoint item)
            {
                if (item.Connection != null)
                {
                    //Only remove the connection. Leave the other ConnectionPoint.
                    _parent.Module.Connections.Remove(item.Connection);
                }
            }

            public ConnectionPoint this[string name]
            {
                get
                {
                    foreach (var cn in Data)
                    {
                        if (cn.Name == name) return cn;
                    }
                    throw new KeyNotFoundException();
                }
            }
        }

        public ProgramModule Module { get; }
        public string Name { get; set; }
        public ConnectionPointCollection ConnectionPoints { get; }

        internal ProgramInterface(ProgramModule module, string name)
        {
            Module = module;
            Name = name;
            ConnectionPoints = new ConnectionPointCollection(this);
        }
    }
}
