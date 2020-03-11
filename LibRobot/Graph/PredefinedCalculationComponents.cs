using LibRobot.Graph.Components;
using LibRobot.Simulation;
using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph
{
    public static class PredefinedCalculationComponents
    {
        internal abstract class SimulatorHandledHandler : ISimulationHandler
        {
            public int GetStorageSize(ComponentSimulationEnvironment env) => 0;
            public void Read(ComponentSimulationEnvironment env) => throw new InvalidOperationException();
            public void Write(ComponentSimulationEnvironment env) => throw new InvalidOperationException();
        }

        internal class SignalDelayHandler : SimulatorHandledHandler
        {
        }

        internal class SignalSplitHandler : SimulatorHandledHandler
        {
        }

        //Or
        internal class SignalMergeHandler : SimulatorHandledHandler
        {
        }

        internal class MemoryCopyHandler : ISimulationHandler
        {
            public int GetStorageSize(ComponentSimulationEnvironment env)
            {
                return env.GetConnectionPointSize("read");
            }

            public void Read(ComponentSimulationEnvironment env)
            {
                env.Read("read", env.GetStorage());
            }

            public void Write(ComponentSimulationEnvironment env)
            {
                env.Write("write", env.GetStorage());
            }
        }
        
        public static readonly CalculationComponentType Delay = new CalculationComponentType("delay", new SignalDelayHandler(),
            new Dictionary<string, ConnectionPointType>()
            {
                { "in", ConnectionPointType.SignalReceive },
                //Name of this ConnectionPoint is hard-coded in Simulator.
                { "out", ConnectionPointType.SignalSend },
            });

        public static readonly CalculationComponentType Split = new CalculationComponentType("split", new SignalSplitHandler(),
            new Dictionary<string, ConnectionPointType>()
            {
                { "in", ConnectionPointType.SignalReceive },
            }, ConnectionPointType.SignalSend);

        public static readonly CalculationComponentType Merge = new CalculationComponentType("merge", new SignalMergeHandler(),
            new Dictionary<string, ConnectionPointType>()
            {
                { "out", ConnectionPointType.SignalSend },
            }, ConnectionPointType.SignalReceive);

        public static readonly CalculationComponentType Copy = new CalculationComponentType("copy", new MemoryCopyHandler(),
            new Dictionary<string, ConnectionPointType>()
            {
                { "read", ConnectionPointType.MemoryReceive },
                { "write", ConnectionPointType.MemorySend },
                { "signal", ConnectionPointType.SignalReceive },
            });

        public static readonly CalculationComponentType Add = CalculationComponentsDesc.Create<AddHandler>("add");
        public static readonly CalculationComponentType Xor = CalculationComponentsDesc.Create<XorHandler>("xor");
    }
}
