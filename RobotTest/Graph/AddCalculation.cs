using LibRobot.Graph;
using LibRobot.Simulation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RobotTest.Graph
{
    public class AddCalculation
    {
        [Test]
        public void Add8Combined()
        {
            var module = new ProgramModule();

            var entry = module.Interfaces.Add("entry");
            var initSignal = entry.ConnectionPoints.AddSignalSender("init");
            var tickSignal = entry.ConnectionPoints.AddSignalSender("tick");

            var mem1 = module.Components.AddMemory(8);
            var mem2 = module.Components.AddMemory(8);
            var mem3 = module.Components.AddMemory(9);

            var calc = module.Components.AddCalculation(PredefinedCalculationComponents.Add);
            module.Connections.AddMemoryAccess(mem1.ConnectionPoints.AddSender(), calc.ConnectionPoints["read1"]);
            module.Connections.AddMemoryAccess(mem2.ConnectionPoints.AddSender(), calc.ConnectionPoints["read2"]);
            module.Connections.AddMemoryAccess(calc.ConnectionPoints["write"], mem3.ConnectionPoints.AddReceiver());
            module.Connections.AddSignal(tickSignal, calc.ConnectionPoints["signal"]);

            var input1 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input1.DisplayName = "input1";
            var input2 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input2.DisplayName = "input2";
            var output = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            output.DisplayName = "output";
            module.Connections.AddMemoryAccess(input1.ConnectionPoints["buffer"], mem1.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(input2.ConnectionPoints["buffer"], mem2.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(mem3.ConnectionPoints.AddSender(), output.ConnectionPoints["buffer"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();

            byte[] data = new byte[4];

            for (int i = 0; i < 256; ++i)
            {
                for (int j = 0; j < 256; ++j)
                {
                    data[0] = (byte)i;
                    data[1] = (byte)j;

                    simulator.DigitalWriter.Write("input1", new Span<byte>(data, 0, 1));
                    simulator.DigitalWriter.Write("input2", new Span<byte>(data, 1, 1));
                    simulator.Tick();
                    simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 2));

                    Assert.AreEqual(i + j, (int)BitConverter.ToUInt16(data, 2));
                }
            }
        }

        [Test]
        public void Add8Separate()
        {
            var module = new ProgramModule();

            var entry = module.Interfaces.Add("entry");
            var initSignal = entry.ConnectionPoints.AddSignalSender("init");
            var tickSignal = entry.ConnectionPoints.AddSignalSender("tick");

            var mem1 = module.Components.AddMemory(8);
            var mem2 = module.Components.AddMemory(8);
            var mem34 = module.Components.AddMemory(9);
            var mem3 = module.Components.AddMemory(8);
            var mem4 = module.Components.AddMemory(1);

            module.Connections.AddMemoryLink(mem34.ConnectionPoints.AddLink(0, 8), mem3.ConnectionPoints.AddLink());
            module.Connections.AddMemoryLink(mem34.ConnectionPoints.AddLink(8, 1), mem4.ConnectionPoints.AddLink());

            var calc = module.Components.AddCalculation(PredefinedCalculationComponents.Add);
            module.Connections.AddMemoryAccess(mem1.ConnectionPoints.AddSender(), calc.ConnectionPoints["read1"]);
            module.Connections.AddMemoryAccess(mem2.ConnectionPoints.AddSender(), calc.ConnectionPoints["read2"]);
            module.Connections.AddMemoryAccess(calc.ConnectionPoints["write"], mem34.ConnectionPoints.AddReceiver());
            module.Connections.AddSignal(tickSignal, calc.ConnectionPoints["signal"]);

            var input1 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input1.DisplayName = "input1";
            var input2 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input2.DisplayName = "input2";
            var output1 = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            output1.DisplayName = "output1";
            var output2 = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            output2.DisplayName = "output2";
            module.Connections.AddMemoryAccess(input1.ConnectionPoints["buffer"], mem1.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(input2.ConnectionPoints["buffer"], mem2.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(mem3.ConnectionPoints.AddSender(), output1.ConnectionPoints["buffer"]);
            module.Connections.AddMemoryAccess(mem4.ConnectionPoints.AddSender(), output2.ConnectionPoints["buffer"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();

            byte[] data = new byte[4];

            for (int i = 0; i < 256; ++i)
            {
                for (int j = 0; j < 256; ++j)
                {
                    data[0] = (byte)i;
                    data[1] = (byte)j;

                    simulator.DigitalWriter.Write("input1", new Span<byte>(data, 0, 1));
                    simulator.DigitalWriter.Write("input2", new Span<byte>(data, 1, 1));
                    simulator.Tick();
                    simulator.DigitalReader.Read("output1", new Span<byte>(data, 2, 1));
                    simulator.DigitalReader.Read("output2", new Span<byte>(data, 3, 1));

                    Assert.AreEqual(i + j, (int)BitConverter.ToUInt16(data, 2));
                }
            }
        }
    }
}
