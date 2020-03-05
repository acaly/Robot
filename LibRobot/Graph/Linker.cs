using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class Linker
    {
        private ProgramModule _mergedModule = new ProgramModule();
        private readonly Dictionary<ProgramInterface, ProgramInterface> _interfaceMap = new Dictionary<ProgramInterface, ProgramInterface>();

        public void AddModule(ProgramModule module)
        {
            if (_mergedModule == null)
            {
                throw new InvalidOperationException();
            }

            if (!module.Validate())
            {
                throw new ArgumentException("module data not valid");
            }
            module.Clone(_mergedModule, out var newInterfaces);
            foreach (var ii in newInterfaces)
            {
                _interfaceMap.Add(ii.Key, ii.Value);
            }
        }

        public void LinkInterface(ProgramInterface a, ProgramInterface b)
        {
            if (_mergedModule == null)
            {
                throw new InvalidOperationException();
            }

            if (!_interfaceMap.TryGetValue(a, out var aa) ||
                !_interfaceMap.TryGetValue(b, out var bb))
            {
                throw new ArgumentException();
            }
            foreach (var cna in aa.ConnectionPoints)
            {
                var cnb = bb.ConnectionPoints[cna.Name];
                if (cna.Connection == null || cnb.Connection == null)
                {
                    //An unconnected connection (signal?)
                    //Remove both connections
                    if (cna.Connection != null)
                    {
                        _mergedModule.Connections.Remove(cna.Connection);
                    }
                    if (cnb.Connection != null)
                    {
                        _mergedModule.Connections.Remove(cnb.Connection);
                    }
                    continue;
                }

                var link = cnb.Connection.A == cnb ? cnb.Connection.B : cnb.Connection.A;

                if (cna.Connection is MemoryLinkConnection)
                {
                    if (!(cnb.Connection is MemoryLinkConnection))
                    {
                        throw new Exception("connection type not matching");
                    }
                    if (cna.BitLength != cnb.BitLength)
                    {
                        throw new Exception("memory link size not matching");
                    }
                }
                else if (cna.Connection is MemoryAccessConnection)
                {
                    if (!(cnb.Connection is MemoryAccessConnection))
                    {
                        throw new Exception("connection type not matching");
                    }
                    if (cna.BitLength != cnb.BitLength)
                    {
                        throw new Exception("memory link size not matching");
                    }
                    if (cna.Type == cnb.Type)
                    {
                        //Can't be both receiver or both sender.
                        throw new Exception("connection type not matching");
                    }
                }
                else
                {
                    Debug.Assert(cna.Connection is SignalConnection);
                    if (!(cnb.Connection is SignalConnection))
                    {
                        throw new Exception("connection type not matching");
                    }
                }

                //This remove does not remove connection point.
                _mergedModule.Connections.Remove(cnb.Connection);

                if (cna.Connection.A == cna)
                {
                    cna.Connection.A = link;
                }
                else
                {
                    cna.Connection.B = link;
                }
                link.Connection = cna.Connection;

                cnb.Connection = null;
                cna.Connection = null;
            }

            _mergedModule.Interfaces.Remove(aa);
            _mergedModule.Interfaces.Remove(bb);
        }

        public ProgramModule GenerateModule()
        {
            var ret = _mergedModule;
            _mergedModule = null;
            return ret;
        }

        public Program GenerateProgram()
        {
            if (!Program.CheckModule(_mergedModule))
            {
                throw new Exception("module is not a program");
            }
            var ret = Program.Create(_mergedModule);
            _mergedModule = null;
            return ret;
        }
    }
}
