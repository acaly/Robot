using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class ProgramModule
    {
        [DebuggerTypeProxy(typeof(DebugDisplayHelper<AbstractComponent>))]
        public sealed class ComponentCollection : ChildrenCollection<AbstractComponent>
        {
            private readonly ProgramModule _parent;

            internal ComponentCollection(ProgramModule parent)
            {
                _parent = parent;
            }

            public MemoryComponent AddMemory(int bitSize)
            {
                var ret = new MemoryComponent(_parent, bitSize);
                Add(ret);
                return ret;
            }

            public CalculationComponent AddCalculation(CalculationComponentType type)
            {
                var ret = new CalculationComponent(_parent, type);
                Add(ret);
                return ret;
            }

            public ExternalComponent AddExternal(ExternalComponentType type)
            {
                var ret = new ExternalComponent(_parent, type);
                Add(ret);
                return ret;
            }

            protected override void BeforeRemove(AbstractComponent item)
            {
                foreach (var cp in item.ConnectionPoints)
                {
                    if (cp.Connection != null)
                    {
                        _parent.Connections.Remove(cp.Connection);
                    }
                }
            }
        }

        [DebuggerTypeProxy(typeof(DebugDisplayHelper<AbstractConnection>))]
        public sealed class ConnectionCollection : ChildrenCollection<AbstractConnection>
        {
            private readonly ProgramModule _parent;

            internal ConnectionCollection(ProgramModule parent)
            {
                _parent = parent;
            }

            public MemoryLinkConnection AddMemoryLink(ConnectionPoint a, ConnectionPoint b)
            {
                if (a == null) throw new ArgumentNullException(nameof(a));
                if (b == null) throw new ArgumentNullException(nameof(b));

                if (a.Type != ConnectionPointType.MemoryLink ||
                    b.Type != ConnectionPointType.MemoryLink ||
                    a.BitLength != b.BitLength)
                {
                    throw new ArgumentException();
                }

                var ret = new MemoryLinkConnection(a, b);
                Add(ret);
                return ret;
            }

            public MemoryAccessConnection AddMemoryAccess(ConnectionPoint src, ConnectionPoint dest)
            {
                if (src == null) throw new ArgumentNullException(nameof(src));
                if (dest == null) throw new ArgumentNullException(nameof(dest));

                if (src.Type != ConnectionPointType.MemorySend ||
                    dest.Type != ConnectionPointType.MemoryReceive)
                {
                    throw new ArgumentException();
                }
                if (src.BitLength != -1 && dest.BitLength != -1 && src.BitLength != dest.BitLength)
                {
                    throw new ArgumentException();
                }
                //TODO we should consider providing CalculationComponentType a chance to check BitLength.

                var ret = new MemoryAccessConnection(src, dest);
                Add(ret);
                return ret;
            }

            public SignalConnection AddSignal(ConnectionPoint src, ConnectionPoint dest)
            {
                if (src == null) throw new ArgumentNullException(nameof(src));
                if (dest == null) throw new ArgumentNullException(nameof(dest));

                if (src.Type != ConnectionPointType.SignalSend ||
                    dest.Type != ConnectionPointType.SignalReceive)
                {
                    throw new ArgumentException();
                }

                var ret = new SignalConnection(src, dest);
                Add(ret);
                return ret;
            }

            protected override void BeforeRemove(AbstractConnection item)
            {
                if (item.A.Connection == item)
                {
                    item.A.Connection = null;
                }
                if (item.B.Connection == item)
                {
                    item.B.Connection = null;
                }
            }
        }

        [DebuggerTypeProxy(typeof(DebugDisplayHelper<ProgramInterface>))]
        public sealed class InterfaceCollection : ChildrenCollection<ProgramInterface>
        {
            private readonly ProgramModule _parent;

            internal InterfaceCollection(ProgramModule parent)
            {
                _parent = parent;
            }

            public ProgramInterface Add(string name)
            {
                var ret = new ProgramInterface(_parent, name);
                Add(ret);
                return ret;
            }

            protected override void BeforeRemove(ProgramInterface item)
            {
                foreach (var cp in item.ConnectionPoints)
                {
                    if (cp.Connection != null)
                    {
                        _parent.Connections.Remove(cp.Connection);
                    }
                }
            }
        }

        public ComponentCollection Components { get; }
        public ConnectionCollection Connections { get; }
        public InterfaceCollection Interfaces { get; }

        public ProgramModule()
        {
            Components = new ComponentCollection(this);
            Connections = new ConnectionCollection(this);
            Interfaces = new InterfaceCollection(this);
        }

        public bool Validate()
        {
            foreach (var c in Components)
            {
                foreach (var cp in c.ConnectionPoints)
                {
                    if (cp.Connection == null) return false;
                }
            }
            foreach (var i in Interfaces)
            {
                foreach (var cp in i.ConnectionPoints)
                {
                    //Allow signal connections to be unconnected
                    if (cp.Connection == null &&
                        cp.Type != ConnectionPointType.SignalSend &&
                        cp.Type != ConnectionPointType.SignalReceive)
                    {
                        return false;
                    }
                }
            }

            //The only other thing we need to check is connections to calculation components.
            //We need to add information to CalculationComponentType before we can do this.
            return true;
        }

        public ProgramModule Clone()
        {
            var ret = new ProgramModule();
            CloneInternal(ret, out _, out _);
            return ret;
        }

        internal void Clone(ProgramModule dest, out Dictionary<ProgramInterface, ProgramInterface> interfaceMap)
        {
            CloneInternal(dest, out _, out interfaceMap);
        }

        private void CloneInternal(ProgramModule dest,
            out Dictionary<AbstractComponent, AbstractComponent> componentMap,
            out Dictionary<ProgramInterface, ProgramInterface> interfaceMap)
        {
            //Clone components
            componentMap = new Dictionary<AbstractComponent, AbstractComponent>();
            foreach (var c in Components)
            {
                if (c is MemoryComponent mc)
                {
                    componentMap.Add(c, dest.Components.AddMemory(mc.BitSize));
                }
                else if (c is ExternalComponent ec)
                {
                    var nc = dest.Components.AddExternal(ec.Type);
                    nc.DisplayName = ec.DisplayName;
                    componentMap.Add(c, nc);
                }
                else
                {
                    var cc = c as CalculationComponent;
                    Debug.Assert(cc != null);
                    componentMap.Add(c, dest.Components.AddCalculation(cc.Type));
                }
            }

            //Clone interfaces
            interfaceMap = new Dictionary<ProgramInterface, ProgramInterface>();
            foreach (var i in Interfaces)
            {
                var ni = dest.Interfaces.Add(i.Name);
                interfaceMap.Add(i, ni);
                foreach (var cp in i.ConnectionPoints)
                {
                    switch (cp.Type)
                    {
                        case ConnectionPointType.MemoryLink:
                            ni.ConnectionPoints.AddMemoryLink(cp.Name, cp.BitLength);
                            break;
                        case ConnectionPointType.MemorySend:
                            ni.ConnectionPoints.AddMemorySender(cp.Name, cp.BitLength);
                            break;
                        case ConnectionPointType.MemoryReceive:
                            ni.ConnectionPoints.AddMemoryReceiver(cp.Name, cp.BitLength);
                            break;
                        case ConnectionPointType.SignalSend:
                            ni.ConnectionPoints.AddSignalSender(cp.Name);
                            break;
                        case ConnectionPointType.SignalReceive:
                            ni.ConnectionPoints.AddSignalReceiver(cp.Name);
                            break;
                        default:
                            break;
                    }
                }
            }

            var cm = componentMap;
            var im = interfaceMap;

            //Local function to convert connection point
            ConnectionPoint ConvertConnectionPoint(ConnectionPoint cp)
            {
                if (cp.Component != null)
                {
                    Debug.Assert(cp.Interface == null);
                    if (cp.Component is MemoryComponent)
                    {
                        return cp.Type switch
                        {
                            ConnectionPointType.MemoryLink => ((MemoryComponent)cm[cp.Component]).ConnectionPoints.AddLink(),
                            ConnectionPointType.MemoryReceive => ((MemoryComponent)cm[cp.Component]).ConnectionPoints.AddReceiver(),
                            ConnectionPointType.MemorySend => ((MemoryComponent)cm[cp.Component]).ConnectionPoints.AddSender(),
                            _ => throw new Exception("internal error"),
                        };
                    }
                    else if (cp.Component is CalculationComponent)
                    {
                        if (cp.Name != null)
                        {
                            return cm[cp.Component].ConnectionPoints[cp.Name];
                        }
                        else
                        {
                            return ((CalculationComponent)cm[cp.Component]).ConnectionPoints.AddAdditional();
                        }
                    }
                    else
                    {
                        return cm[cp.Component].ConnectionPoints[cp.Name];
                    }
                }
                else
                {
                    Debug.Assert(cp.Interface != null);
                    return im[cp.Interface].ConnectionPoints[cp.Name];
                }
            }

            //Clone connections
            foreach (var cn in Connections)
            {
                if (cn is MemoryLinkConnection)
                {
                    dest.Connections.AddMemoryLink(ConvertConnectionPoint(cn.A), ConvertConnectionPoint(cn.B));
                }
                else if (cn is MemoryAccessConnection)
                {
                    dest.Connections.AddMemoryAccess(ConvertConnectionPoint(cn.A), ConvertConnectionPoint(cn.B));
                }
                else
                {
                    Debug.Assert(cn is SignalConnection);
                    dest.Connections.AddSignal(ConvertConnectionPoint(cn.A), ConvertConnectionPoint(cn.B));
                }
            }
        }
    }
}
