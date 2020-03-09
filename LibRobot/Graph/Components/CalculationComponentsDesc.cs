using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph.Components
{
    internal static class CalculationComponentsDesc
    {
        public static CalculationComponentType Create<T>(string name) where T : ISimulationHandler, new()
        {
            return new CalculationComponentType(name, new T(),
                new Dictionary<string, ConnectionPointType>()
                {
                    { "read1", ConnectionPointType.MemoryReceive },
                    { "read2", ConnectionPointType.MemoryReceive },
                    { "write", ConnectionPointType.MemorySend },
                    { "signal", ConnectionPointType.SignalReceive },
                });
        }
    }
}
