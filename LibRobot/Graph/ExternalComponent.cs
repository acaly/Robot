using System;
using System.Collections.Generic;
using System.Text;
using static LibRobot.Graph.ExternalComponentType;

namespace LibRobot.Graph
{
    public sealed class ExternalComponent : AbstractComponent
    {
        public ExternalComponentType Type { get; }

        public ExternalComponent(ProgramModule module, ExternalComponentType type) : base(module, GetConnectionPointTypes(type))
        {
            Type = type;
        }

        private static readonly Dictionary<ExternalComponentType, Dictionary<string, ConnectionPointType>> _connectionPoints =
            new Dictionary<ExternalComponentType, Dictionary<string, ConnectionPointType>>()
            {
                { AnalogInput, new Dictionary<string, ConnectionPointType>() { { "buffer", ConnectionPointType.MemorySend } } },
                { AnalogOutput, new Dictionary<string, ConnectionPointType>() { { "buffer", ConnectionPointType.MemoryReceive } } },
                { DigitalInput, new Dictionary<string, ConnectionPointType>() { { "buffer", ConnectionPointType.MemorySend } } },
                { DigitalOutput, new Dictionary<string, ConnectionPointType>() { { "buffer", ConnectionPointType.MemoryReceive } } },
            };

        private static Dictionary<string, ConnectionPointType> GetConnectionPointTypes(ExternalComponentType type)
        {
            if (!_connectionPoints.TryGetValue(type, out var ret))
            {
                throw new ArgumentOutOfRangeException();
            }
            return ret;
        }
    }
}
