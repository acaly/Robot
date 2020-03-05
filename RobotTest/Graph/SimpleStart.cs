using LibRobot.Graph;
using NUnit.Framework;
using System.Collections.Generic;

namespace RobotTest.Graph
{
    public class SimpleStart
    {
        private class TestCalculation : ISimulationHandler
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
                var storage = env.GetStorage();
                for (int i = 0; i < storage.Length; ++i)
                {
                    storage[i] = (byte)~storage[i];
                }
                env.Write("write", storage);
            }
        }

        [Test]
        public void Test()
        {
            var calcType = new CalculationComponentType("test", new TestCalculation(), new Dictionary<string, ConnectionPointType>()
            {
                { "signal", ConnectionPointType.SignalReceive },
                { "read", ConnectionPointType.MemoryReceive },
                { "write", ConnectionPointType.MemorySend },
            });

            var module = new ProgramModule();

            var entry = module.Interfaces.Add("entry");
            var initSignal = entry.ConnectionPoints.AddSignalSender("init");
            var tickSignal = entry.ConnectionPoints.AddSignalSender("tick");

            var mem1 = module.Components.AddMemory(32);
            var mem2 = module.Components.AddMemory(32);
            var calc = module.Components.AddCalculation(calcType);
            module.Connections.AddMemoryAccess(mem1.ConnectionPoints.AddSender(), calc.ConnectionPoints["read"]);
            module.Connections.AddMemoryAccess(calc.ConnectionPoints["write"], mem2.ConnectionPoints.AddReceiver());
            module.Connections.AddSignal(initSignal, calc.ConnectionPoints["signal"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();
            //Tick does noting. Skipped.
        }
    }
}
