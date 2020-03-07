using LibRobot.Graph;
using LibRobot.Simulation;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProfilingTest
{
    class Program
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

        static void Main(string[] args)
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
            module.Connections.AddSignal(tickSignal, calc.ConnectionPoints["signal"]);

            var write = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            write.DisplayName = "input";
            var read = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            read.DisplayName = "output";
            module.Connections.AddMemoryAccess(write.ConnectionPoints["buffer"], mem1.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(mem2.ConnectionPoints.AddSender(), read.ConnectionPoints["buffer"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();

            int inputVal = 0x12345678;
            byte[] buffer = new byte[4];

            var ret = 0;
            for (int i = 0; i < 10000000; ++i)
            {
                simulator.DigitalWriter.Write("input", MemoryMarshal.Cast<int, byte>(MemoryMarshal.CreateSpan(ref inputVal, 1)));
                simulator.Tick();
                simulator.DigitalReader.Read("output", buffer);
                ret += buffer[0];
            }

            Console.WriteLine(ret);
        }
    }
}
