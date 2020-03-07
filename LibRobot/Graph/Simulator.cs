using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class Simulator
    {
        private class MemoryInfo
        {
            public int[] Offsets;
            public int[] Lengths;
            public int TotalLength;
            public bool IsUnaliged;
        }

        private class ComponentInfo
        {
            public ComponentSimulationEnvironment Environment;
            public ISimulationHandler Handler;
        }

        private class DelayedSignalInfo
        {
            public int NextSignalChannel;
        }

        private class ChannelInfo
        {
            public int[] Components;
            public string[] ComponentTriggerPoints;
            public int[] Delays;
        }

        private class InitialMemoryValues
        {
            public int MemoryIndex;
            public byte[] Data;
        }

        private readonly byte[] _memory;

        private readonly MemoryInfo[] _memorySegments;
        private readonly ComponentInfo[] _components;
        private readonly DelayedSignalInfo[] _delays;
        private readonly ChannelInfo[] _channels;
        private readonly InitialMemoryValues[] _initialValues;

        private readonly Dictionary<string, int> _analogInput;
        private readonly Dictionary<string, int> _analogOutput;
        private readonly Dictionary<string, int> _digitalInput;
        private readonly Dictionary<string, int> _digitalOutput;

        private readonly int _initChannel;
        private readonly int _tickChannel;

        private bool _allowRead, _allowWrite;
        private readonly List<int> _nextTriggerList = new List<int>();
        private readonly List<int> _processingTriggerList = new List<int>();

        public ExternalAnalogWriter AnalogWriter;
        public ExternalAnalogReader AnalogReader;
        public ExternalDigitalWriter DigitalWriter;
        public ExternalDigitalReader DigitalReader;
        
        public Simulator(Program program)
        {
            CompileMemory(program, out _memorySegments, out var memMapping, out var memLen, out var memConnectionMapping);
            CreateComponents(program, out _components, out var componentMapping);
            CompileChannels(program, componentMapping, out _channels, out var channelMapping, out _delays);
            CreateComponentEnvironments(this, _components, componentMapping, memConnectionMapping, channelMapping);

            _memory = new byte[memLen / 8];

            var entry = program.Module.Interfaces.First();
            Debug.Assert(entry.Name == "entry");
            var init = entry.ConnectionPoints.Single(cp => cp.Name == "init");
            var tick = entry.ConnectionPoints.Single(cp => cp.Name == "tick");
            _initChannel = init.Connection != null ? channelMapping[init.Connection] : -1;
            _tickChannel = tick.Connection != null ? channelMapping[tick.Connection] : -1;

            List<InitialMemoryValues> initialValues = new List<InitialMemoryValues>();
            foreach (var c in memMapping)
            {
                if (c.Key.InitialData != null)
                {
                    initialValues.Add(new InitialMemoryValues
                    {
                        MemoryIndex = c.Value,
                        Data = c.Key.InitialData,
                    });
                }
            }
            _initialValues = initialValues.ToArray();

            _analogInput = GetExternalDictionary(program, ExternalComponentType.AnalogInput, memConnectionMapping);
            _analogOutput = GetExternalDictionary(program, ExternalComponentType.AnalogOutput, memConnectionMapping);
            _digitalInput = GetExternalDictionary(program, ExternalComponentType.DigitalInput, memConnectionMapping);
            _digitalOutput = GetExternalDictionary(program, ExternalComponentType.DigitalOutput, memConnectionMapping);

            AnalogReader = new ExternalAnalogReader(this);
            AnalogWriter = new ExternalAnalogWriter(this);
            DigitalReader = new ExternalDigitalReader(this);
            DigitalWriter = new ExternalDigitalWriter(this);
        }

        private static void CompileMemory(Program program, out MemoryInfo[] info,
            out Dictionary<MemoryComponent, int> mapping, out int len,
            out Dictionary<AbstractConnection, int> memConnectionMapping)
        {
            mapping = new Dictionary<MemoryComponent, int>();

            List<int> memSize = new List<int>();
            List<List<Tuple<int, int>>> segmentLinkInfo = new List<List<Tuple<int, int>>>();

            //Create components
            foreach (var c in program.Module.Components)
            {
                if (c is MemoryComponent m)
                {
                    mapping.Add(m, mapping.Count);
                    memSize.Add(m.BitSize);
                    segmentLinkInfo.Add(new List<Tuple<int, int>>());
                }
            }
            var memBlocks = new List<MemoryInfo>();

            List<int> linkSize = new List<int>();
            Dictionary<int, List<int>> splitLink = new Dictionary<int, List<int>>();

            //Create link info
            foreach (var c in program.Module.Connections)
            {
                if (c is MemoryLinkConnection l)
                {
                    var ma = mapping[(MemoryComponent)l.A.Component];
                    var mb = mapping[(MemoryComponent)l.B.Component];
                    var lsize = l.A.BitLength;
                    var lid = linkSize.Count;

                    linkSize.Add(lsize);
                    segmentLinkInfo[ma].Add(new Tuple<int, int>(lid, l.A.BitOffset));
                    segmentLinkInfo[mb].Add(new Tuple<int, int>(lid, l.B.BitOffset));
                }
            }

            //Process overlapping links
            var unusedLink = new HashSet<int>();
            while (true)
            {
                int minOverlapSize = int.MaxValue;
                int minOverlapSegment = -1;
                int minOverlapLink = -1;
                for (int i = 0; i < segmentLinkInfo.Count; ++i)
                {
                    var currentLinkInfo = segmentLinkInfo[i];
                    currentLinkInfo.Sort((a, b) => a.Item2 - b.Item2);
                    for (int j = 0; j < currentLinkInfo.Count - 1; ++j)
                    {
                        if (currentLinkInfo[j].Equals(currentLinkInfo[j + 1]))
                        {
                            currentLinkInfo.RemoveAt(j);
                            j -= 1;
                        }
                        else
                        {
                            var leftEnds = currentLinkInfo[j].Item2 + linkSize[currentLinkInfo[j].Item1];
                            var overlapSize = leftEnds - currentLinkInfo[j + 1].Item2;
                            if (overlapSize > 0)
                            {
                                if (overlapSize < minOverlapSize)
                                {
                                    minOverlapSize = overlapSize;
                                    minOverlapSegment = i;
                                    minOverlapLink = j;
                                }
                            }
                        }
                    }
                }
                if (minOverlapSize == int.MaxValue)
                {
                    break;
                }

                //Find one overlapped link
                {
                    var currentLinkInfo = segmentLinkInfo[minOverlapSegment];
                    var leftLink = currentLinkInfo[minOverlapLink].Item1;
                    var rightLink = currentLinkInfo[minOverlapLink + 1].Item1;
                    if (leftLink == rightLink)
                    {
                        //Self overlapping
                        throw new NotImplementedException("self overlapping is not supported");
                    }

                    //Create new links
                    var newCenterLink = linkSize.Count;
                    linkSize.Add(minOverlapSize);
                    var newLeftLink = -1;
                    var newLeftSize = linkSize[leftLink] - minOverlapSize;
                    if (newLeftSize > 0)
                    {
                        newLeftLink = linkSize.Count;
                        linkSize.Add(newLeftSize);
                    }
                    var newRightLink = -1;
                    var newRightSize = linkSize[rightLink] - minOverlapSize;
                    if (newRightSize > 0)
                    {
                        newRightLink = linkSize.Count;
                        linkSize.Add(newRightSize);
                    }

                    //Update each segments (leftLink -> newLeftLink+newCenterLink, rightLink -> newCenterLink+newRightLink)
                    foreach (var links in segmentLinkInfo)
                    {
                        for (int i = 0; i < links.Count; ++i)
                        {
                            if (links[i].Item1 == leftLink)
                            {
                                var start = links[i].Item2;
                                links[i] = new Tuple<int, int>(newCenterLink, start + newLeftSize);
                                if (newLeftLink != -1)
                                {
                                    links.Add(new Tuple<int, int>(newLeftLink, start));
                                }
                            }
                            else if (links[i].Item1 == rightLink)
                            {
                                var start = links[i].Item2;
                                links[i] = new Tuple<int, int>(newCenterLink, start);
                                if (newRightLink != -1)
                                {
                                    links.Add(new Tuple<int, int>(newRightLink, start + newRightSize));
                                }
                            }
                        }
                    }

                    unusedLink.Add(leftLink);
                    unusedLink.Add(rightLink);
                }
            }

            int mainMemoryPos = 0;
            var tmpOffset = new List<int>();
            var tmpLen = new List<int>();
            void AlignMemory()
            {
                mainMemoryPos = (mainMemoryPos + 7) / 8 * 8;
            }
            void AddMemory(int size)
            {
                tmpOffset.Add(mainMemoryPos);
                tmpLen.Add(size);
                mainMemoryPos += size;
                AlignMemory();
            }
            
            //Allocate for linked memory
            var linkInMainMemory = new int[linkSize.Count];
            for (int i = 0; i < linkSize.Count; ++i)
            {
                if (unusedLink.Contains(i)) continue;
                linkInMainMemory[i] = mainMemoryPos;
                mainMemoryPos += linkSize[i];
                AlignMemory();
            }

            //Allocate for each segment
            for (int i = 0; i < segmentLinkInfo.Count; ++i)
            {
                tmpOffset.Clear();
                tmpLen.Clear();
                int scanPos = 0;

                //The linkinfo list should be sorted and have no overlapped link
                for (int j = 0; j < segmentLinkInfo[i].Count; ++j)
                {
                    var linkStart = segmentLinkInfo[i][j].Item2;
                    if (linkStart > scanPos)
                    {
                        AddMemory(linkStart - scanPos);
                    }
                    var linkId = segmentLinkInfo[i][j].Item1;
                    if (linkSize[linkId] > 0)
                    {
                        tmpOffset.Add(linkInMainMemory[linkId]);
                        tmpLen.Add(linkSize[linkId]);
                    }
                    scanPos = linkStart + linkSize[linkId];
                }

                if (scanPos < memSize[i])
                {
                    AddMemory(memSize[i] - scanPos);
                }

                memBlocks.Add(new MemoryInfo
                {
                    Offsets = tmpOffset.ToArray(),
                    Lengths = tmpLen.ToArray(),
                    TotalLength = memSize[i],
                });
            }

            MemoryInfo CreateSubInfo(MemoryInfo info, int offset, int length)
            {
                tmpOffset.Clear();
                tmpLen.Clear();
                var parentPos = 0;
                for (int i = 0; i < info.Lengths.Length; ++i)
                {
                    if (parentPos >= offset + length) break;
                    var parentEnd = parentPos + info.Lengths[i];
                    if (parentEnd > offset)
                    {
                        var shinkLeft = 0;
                        var shinkRight = 0;
                        if (parentPos < offset)
                        {
                            shinkLeft = offset - parentPos;
                        }
                        if (parentEnd > offset + length)
                        {
                            shinkRight = parentEnd - (offset + length);
                        }
                        tmpOffset.Add(info.Offsets[i] + shinkLeft);
                        tmpLen.Add(info.Lengths[i] - shinkLeft - shinkRight);
                    }
                    parentPos = parentEnd;
                }
                return new MemoryInfo
                {
                    Offsets = tmpOffset.ToArray(),
                    Lengths = tmpLen.ToArray(),
                    TotalLength = length,
                };
            }

            memConnectionMapping = new Dictionary<AbstractConnection, int>();
            foreach (var conn in program.Module.Connections)
            {
                if (conn is MemoryAccessConnection)
                {
                    if (conn.A.Component is MemoryComponent mem)
                    {
                        Debug.Assert(!(conn.B.Component is MemoryComponent));
                        memConnectionMapping.Add(conn, memBlocks.Count);
                        memBlocks.Add(CreateSubInfo(memBlocks[mapping[mem]], conn.A.BitOffset, conn.A.BitLength));
                    }
                    else
                    {
                        Debug.Assert(conn.B.Component is MemoryComponent);
                        mem = (MemoryComponent)conn.B.Component;
                        memConnectionMapping.Add(conn, memBlocks.Count);
                        memBlocks.Add(CreateSubInfo(memBlocks[mapping[mem]], conn.B.BitOffset, conn.B.BitLength));
                    }
                }
            }

            info = memBlocks.ToArray();
            len = mainMemoryPos;

            //Mark unaliged memory
            foreach (var mem in info)
            {
                mem.IsUnaliged = mem.Lengths.Any(len => (len & 7) != 0);
            }
        }

        private static void CreateComponents(Program program, out ComponentInfo[] info,
            out Dictionary<AbstractComponent, int> mapping)
        {
            List<ComponentInfo> ret = new List<ComponentInfo>();
            mapping = new Dictionary<AbstractComponent, int>();

            foreach (var c in program.Module.Components)
            {
                if (c is CalculationComponent cc &&
                    !(cc.Type.SimulationHandler is PredefinedCalculationComponents.SimulatorHandledHandler))
                {
                    mapping.Add(c, ret.Count);
                    ret.Add(new ComponentInfo
                    {
                        Handler = cc.Type.SimulationHandler,
                    });
                }
            }

            info = ret.ToArray();
        }

        private class ConstructingChannel
        {
            public HashSet<ConnectionPoint> Components = new HashSet<ConnectionPoint>();
            public HashSet<int> Delays = new HashSet<int>();
            public HashSet<SignalConnection> SignalLinks = new HashSet<SignalConnection>();
            public bool ActiveSender = false;
            public int ActiveChannelId = -1;

            public void Expand(Dictionary<SignalConnection, ConstructingChannel> channels)
            {
                HashSet<SignalConnection> loopCheck = new HashSet<SignalConnection>();
                while (SignalLinks.Count > 0)
                {
                    var link = SignalLinks.First();
                    SignalLinks.Remove(link);
                    if (loopCheck.Add(link))
                    {
                        if (!channels.TryGetValue(link, out var linkInfo))
                        {
                            throw new Exception("internal error");
                        }
                        Components.UnionWith(linkInfo.Components);
                        Delays.UnionWith(linkInfo.Delays);
                        SignalLinks.UnionWith(linkInfo.SignalLinks);
                    }
                }
            }
        }

        private static void CompileChannels(Program program, Dictionary<AbstractComponent, int> componentMapping,
            out ChannelInfo[] info, out Dictionary<AbstractConnection, int> channelMapping,
            out DelayedSignalInfo[] delayInfo)
        {
            //Create delay list
            List<SignalConnection> delayConnectionMapping = new List<SignalConnection>();
            Dictionary<AbstractComponent, int> delayMapping = new Dictionary<AbstractComponent, int>();
            foreach (var c in program.Module.Components)
            {
                if (c is CalculationComponent cal &&
                    cal.Type.SimulationHandler is PredefinedCalculationComponents.SignalDelayHandler)
                {
                    delayMapping.Add(c, delayMapping.Count);
                    delayConnectionMapping.Add((SignalConnection)cal.ConnectionPoints["out"].Connection);
                }
            }

            //Create initial channel list
            Dictionary<SignalConnection, ConstructingChannel> channels = new Dictionary<SignalConnection, ConstructingChannel>();
            int activeChannelCount = 0;
            foreach (var c in program.Module.Connections)
            {
                if (c is SignalConnection s)
                {
                    var channel = new ConstructingChannel();
                    var receiver = s.B.Component;

                    //Check sender
                    var sender = s.A.Component;
                    if (sender == null)
                    {
                        //Interface. This is active.
                        channel.ActiveSender = true;
                    }
                    else if (sender is CalculationComponent cal &&
                        !(cal.Type.SimulationHandler is PredefinedCalculationComponents.SignalSplitHandler ||
                            cal.Type.SimulationHandler is PredefinedCalculationComponents.SignalMergeHandler))
                    {
                        //Note that delay needs an active channel
                        channel.ActiveSender = true;
                    }
                    if (channel.ActiveSender)
                    {
                        channel.ActiveChannelId = activeChannelCount++;
                    }

                    //External and memory can't have signal receving connection.
                    Debug.Assert(receiver is CalculationComponent);

                    var handler = ((CalculationComponent)receiver).Type.SimulationHandler;
                    if (handler is PredefinedCalculationComponents.SignalDelayHandler)
                    {
                        channel.Delays.Add(delayMapping[receiver]);
                    }
                    else if (handler is PredefinedCalculationComponents.SignalSplitHandler ||
                        handler is PredefinedCalculationComponents.SignalMergeHandler)
                    {
                        //Record all SignalSend connections
                        foreach (var cp in receiver.ConnectionPoints)
                        {
                            if (cp.Type == ConnectionPointType.SignalSend)
                            {
                                Debug.Assert(cp.Connection is SignalConnection);
                                channel.SignalLinks.Add((SignalConnection)cp.Connection);
                            }
                        }
                    }
                    else
                    {
                        channel.Components.Add(s.B);
                    }
                    channels.Add(s, channel);
                }
            }

            List<ChannelInfo> ret = new List<ChannelInfo>();
            channelMapping = new Dictionary<AbstractConnection, int>();

            //Generate delay info
            delayInfo = delayConnectionMapping
                .Select(dd => new DelayedSignalInfo { NextSignalChannel = channels[dd].ActiveChannelId })
                .ToArray();

            //Generate active channels
            foreach (var ch in channels)
            {
                if (!ch.Value.ActiveSender) continue;

                channelMapping.Add(ch.Key, ret.Count);
                ch.Value.Expand(channels);
                ret.Add(new ChannelInfo
                {
                    Components = ch.Value.Components.Select(c => componentMapping[c.Component]).ToArray(),
                    ComponentTriggerPoints = ch.Value.Components.Select(c => c.Name).ToArray(),
                    Delays = ch.Value.Delays.ToArray(),
                });
            }

            info = ret.ToArray();
        }

        private static void CreateComponentEnvironments(Simulator sim, ComponentInfo[] components,
            Dictionary<AbstractComponent, int> componentMapping, Dictionary<AbstractConnection, int> memConnectionMapping,
            Dictionary<AbstractConnection, int> signalMapping)
        {
            List<int> additional = new List<int>();
            foreach (var component in componentMapping)
            {
                var connections = new Dictionary<string, Tuple<int, ConnectionPointType>>();
                foreach (var cp in component.Key.ConnectionPoints)
                {
                    int id;
                    if (cp.Connection is MemoryAccessConnection)
                    {
                        id = memConnectionMapping[cp.Connection];
                    }
                    else if (cp.Connection is SignalConnection)
                    {
                        id = signalMapping[cp.Connection];
                    }
                    else
                    {
                        throw new Exception("internal error");
                    }
                    if (cp.Name == null)
                    {
                        additional.Add(id);
                    }
                    else
                    {
                        connections.Add(cp.Name, new Tuple<int, ConnectionPointType>(id, cp.Type));
                    }
                }
                var type = ((CalculationComponent)component.Key).Type;
                components[component.Value].Environment = new ComponentSimulationEnvironment(sim, connections, additional.ToArray(), type);
            }
        }

        private static Dictionary<string, int> GetExternalDictionary(Program program, ExternalComponentType type,
            Dictionary<AbstractConnection, int> mapping)
        {
            return program.Module.Components
                .Where(cc => cc is ExternalComponent ex && ex.Type == type && cc.DisplayName != null)
                .ToDictionary(cc => cc.DisplayName, cc => mapping[cc.ConnectionPoints["buffer"].Connection]);
        }

        public void Start()
        {
            foreach (var i in _initialValues)
            {
                WriteMemory(i.MemoryIndex, i.Data, true);
            }

            Trigger(_initChannel);
            Step();
        }

        public void Tick()
        {
            Trigger(_tickChannel);
            Step();
        }

        public void Step()
        {
            _processingTriggerList.Clear();
            _processingTriggerList.AddRange(_nextTriggerList);
            _nextTriggerList.Clear();

            _allowRead = true;
            foreach (var tt in _processingTriggerList)
            {
                var ch = _channels[tt];
                _nextTriggerList.AddRange(ch.Delays.Select(dd => _delays[dd].NextSignalChannel));

                for (int i = 0; i < ch.Components.Length; ++i)
                {
                    var comp = _components[ch.Components[i]];
                    comp.Handler.Triggered(comp.Environment, ch.ComponentTriggerPoints[i]);
                    comp.Handler.Read(comp.Environment);
                }
            }
            _allowRead = false;

            _allowWrite = true;
            foreach (var tt in _processingTriggerList)
            {
                var ch = _channels[tt];
                for (int i = 0; i < ch.Components.Length; ++i)
                {
                    var comp = _components[ch.Components[i]];
                    comp.Handler.Write(comp.Environment);
                }
            }
            _allowWrite = false;
        }

        public sealed class ExternalAnalogReader
        {
            private readonly Simulator _simulator;

            internal ExternalAnalogReader(Simulator simulator)
            {
                _simulator = simulator;
            }

            public double this[string name]
            {
                get
                {
                    if (!_simulator._analogOutput.TryGetValue(name, out var index))
                    {
                        throw new KeyNotFoundException();
                    }
                    var mem = _simulator._memorySegments[index];
                    if (mem.TotalLength == 8)
                    {
                        byte data = 0;
                        _simulator.ReadMemory(index, MemoryMarshal.CreateSpan(ref data, 1), true);
                        return data / 255.0;
                    }
                    else if (mem.TotalLength == 16)
                    {
                        ushort data = 0;
                        _simulator.ReadMemory(index, MemoryMarshal.Cast<ushort, byte>(MemoryMarshal.CreateSpan(ref data, 1)), true);
                        return data / (double)ushort.MaxValue;
                    }
                    else if (mem.TotalLength == 32)
                    {
                        uint data = 0;
                        _simulator.ReadMemory(index, MemoryMarshal.Cast<uint, byte>(MemoryMarshal.CreateSpan(ref data, 1)), true);
                        return data / (double)uint.MaxValue;
                    }
                    else if (mem.TotalLength == 64)
                    {
                        ulong data = 0;
                        _simulator.ReadMemory(index, MemoryMarshal.Cast<ulong, byte>(MemoryMarshal.CreateSpan(ref data, 1)), true);
                        return data / (double)ulong.MaxValue;
                    }
                    else
                    {
                        throw new NotImplementedException("unaligned memory is not supported");
                    }
                }
            }
        }

        public sealed class ExternalAnalogWriter
        {
            private readonly Simulator _simulator;

            internal ExternalAnalogWriter(Simulator simulator)
            {
                _simulator = simulator;
            }

            public double this[string name]
            {
                set
                {
                    if (!_simulator._analogInput.TryGetValue(name, out var index))
                    {
                        throw new KeyNotFoundException();
                    }
                    var mem = _simulator._memorySegments[index];
                    if (mem.TotalLength == 8)
                    {
                        byte data = (byte)(value * 255);
                        _simulator.WriteMemory(index, MemoryMarshal.CreateSpan(ref data, 1), true);
                    }
                    else if (mem.TotalLength == 16)
                    {
                        ushort data = (ushort)(value * ushort.MaxValue);
                        _simulator.WriteMemory(index, MemoryMarshal.Cast<ushort, byte>(MemoryMarshal.CreateSpan(ref data, 1)), true);
                    }
                    else if (mem.TotalLength == 32)
                    {
                        uint data = (uint)(value * uint.MaxValue);
                        _simulator.WriteMemory(index, MemoryMarshal.Cast<uint, byte>(MemoryMarshal.CreateSpan(ref data, 1)), true);
                    }
                    else if (mem.TotalLength == 64)
                    {
                        ulong data = (ulong)(value * ulong.MaxValue);
                        _simulator.WriteMemory(index, MemoryMarshal.Cast<ulong, byte>(MemoryMarshal.CreateSpan(ref data, 1)), true);
                    }
                    else
                    {
                        throw new NotImplementedException("unaligned memory is not supported");
                    }
                }
            }
        }

        public sealed class ExternalDigitalReader
        {
            private readonly Simulator _simulator;

            internal ExternalDigitalReader(Simulator simulator)
            {
                _simulator = simulator;
            }

            public int GetSize(string name)
            {
                if (!_simulator._digitalOutput.TryGetValue(name, out var index))
                {
                    throw new KeyNotFoundException();
                }
                var mem = _simulator._memorySegments[index];
                return mem.TotalLength;
            }

            public void Read(string name, Span<byte> buffer)
            {
                if (!_simulator._digitalOutput.TryGetValue(name, out var index))
                {
                    throw new KeyNotFoundException();
                }
                var mem = _simulator._memorySegments[index];
                if ((mem.TotalLength + 7) / 8 > buffer.Length)
                {
                    throw new ArgumentException();
                }
                _simulator.ReadMemory(index, buffer, true);
            }
        }

        public sealed class ExternalDigitalWriter
        {
            private readonly Simulator _simulator;

            internal ExternalDigitalWriter(Simulator simulator)
            {
                _simulator = simulator;
            }

            public int GetSize(string name)
            {
                if (!_simulator._digitalInput.TryGetValue(name, out var index))
                {
                    throw new KeyNotFoundException();
                }
                var mem = _simulator._memorySegments[index];
                return mem.TotalLength;
            }

            public void Write(string name, Span<byte> buffer)
            {
                if (!_simulator._digitalInput.TryGetValue(name, out var index))
                {
                    throw new KeyNotFoundException();
                }
                var mem = _simulator._memorySegments[index];
                if ((mem.TotalLength + 7) / 8 > buffer.Length)
                {
                    throw new ArgumentException();
                }
                _simulator.WriteMemory(index, buffer, true);
            }
        }

        internal int GetMemorySize(int id)
        {
            return _memorySegments[id].TotalLength;
        }

        private static byte ReadUnalignedByte(Span<byte> buffer, int bitOffset, int bitLength)
        {
            var alignedByteStart = bitOffset / 8;
            if (alignedByteStart * 8 == bitOffset)
            {
                //Fast path
                return buffer[alignedByteStart];
            }
            //LE
            var move = bitOffset & 7;
            var len1 = 8 - move;
            var len2 = bitLength - len1;
            var first = buffer[alignedByteStart];
            var second = len2 > 0 ? buffer[alignedByteStart + 1] : 0; //Avoid accessing outside range
            return (byte)(first >> move | second << len1);
        }

        private static void WriteUnalignedByte(Span<byte> buffer, int bitOffset, int bitLength, byte data)
        {
            var alignedByteStart = bitOffset / 8;
            if (alignedByteStart * 8 == bitOffset)
            {
                //Fast path
                buffer[alignedByteStart] = data;
                return;
            }
            var move = bitOffset & 7;
            var len1 = 8 - move;
            var len2 = bitLength - len1;
            //Be careful when bitLength < len1
            var mask1 = (byte)(((byte)~(255u << bitLength)) << move);
            buffer[alignedByteStart] &= (byte)~mask1;
            buffer[alignedByteStart] |= (byte)((data << move) & mask1);
            if (len2 > 0)
            {
                buffer[alignedByteStart + 1] &= (byte)~(255u >> len1);
                buffer[alignedByteStart + 1] |= (byte)(data >> len1);
            }
        }

        private static void CopyUnalignedMemory(Span<byte> src, int bitOffsetSrc, Span<byte> dest, int bitOffsetDest, int bitLength)
        {
            if ((bitOffsetSrc & 7) == 0 && (bitOffsetDest & 7) == 0)
            {
                //Fast path
                var byteLength = (bitLength + 7) / 8;
                src.Slice(bitOffsetSrc / 8, byteLength).CopyTo(dest.Slice(bitOffsetDest / 8, byteLength));
            }
            else
            {
                //Slow path
                //We copy byte by byte
                for (int i = 0; i < bitLength; i += 8)
                {
                    var copyLen = Math.Min(8, bitLength - i);
                    var b = ReadUnalignedByte(src, bitOffsetSrc + i, copyLen);
                    WriteUnalignedByte(dest, bitOffsetDest + i, copyLen, b);
                }
            }
        }

        internal bool ReadMemory(int id, Span<byte> buffer, bool skipCheck)
        {
            if (!skipCheck && !_allowRead) return false;
            var mem = _memorySegments[id];
            int bufferBitPos = 0;
            var memorySpan = new Span<byte>(_memory);
            for (int i = 0; i < mem.Offsets.Length; ++i)
            {
                CopyUnalignedMemory(memorySpan, mem.Offsets[i], buffer, bufferBitPos, mem.Lengths[i]);
                bufferBitPos += mem.Lengths[i];
            }
            return true;
        }

        internal bool WriteMemory(int id, Span<byte> data, bool skipCheck)
        {
            if (!skipCheck && !_allowWrite) return false;
            var mem = _memorySegments[id];
            int bufferBitPos = 0;
            var memorySpan = new Span<byte>(_memory);
            for (int i = 0; i < mem.Offsets.Length; ++i)
            {
                CopyUnalignedMemory(data, bufferBitPos, memorySpan, mem.Offsets[i], mem.Lengths[i]);
                bufferBitPos += mem.Lengths[i];
            }
            return true;
        }

        internal bool Trigger(int id)
        {
            if (id == -1) return true;
            _nextTriggerList.Add(id);
            return true;
        }
    }
}
