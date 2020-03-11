using LibRobot.Graph;
using LibRobot.Simulation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace RobotTest.Graph
{
    public class XorCalculation
    {
        [Test]
        public void Xor4At6()
        {
            var module = new ProgramModule();

            var entry = module.Interfaces.Add("entry");
            var initSignal = entry.ConnectionPoints.AddSignalSender("init");
            var tickSignal = entry.ConnectionPoints.AddSignalSender("tick");

            var mem1 = module.Components.AddMemory(6);
            var mem2 = module.Components.AddMemory(6);
            var mem3 = module.Components.AddMemory(6);

            var calc = module.Components.AddCalculation(PredefinedCalculationComponents.Xor);
            module.Connections.AddMemoryAccess(mem1.ConnectionPoints.AddSender(0, 4), calc.ConnectionPoints["read1"]);
            module.Connections.AddMemoryAccess(mem2.ConnectionPoints.AddSender(0, 4), calc.ConnectionPoints["read2"]);
            module.Connections.AddMemoryAccess(calc.ConnectionPoints["write"], mem3.ConnectionPoints.AddReceiver(0, 4));
            module.Connections.AddSignal(tickSignal, calc.ConnectionPoints["signal"]);

            var input1 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input1.DisplayName = "input1";
            var input2 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input2.DisplayName = "input2";
            var output = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            output.DisplayName = "output";
            module.Connections.AddMemoryAccess(input1.ConnectionPoints["buffer"], mem1.ConnectionPoints.AddReceiver(0, 4));
            module.Connections.AddMemoryAccess(input2.ConnectionPoints["buffer"], mem2.ConnectionPoints.AddReceiver(0, 4));
            module.Connections.AddMemoryAccess(mem3.ConnectionPoints.AddSender(0, 4), output.ConnectionPoints["buffer"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();

            byte[] data = new byte[3];

            for (int i = 0; i < 256; ++i)
            {
                for (int j = 0; j < 256; ++j)
                {
                    data[0] = (byte)i;
                    data[1] = (byte)j;
                    data[2] = 0;

                    simulator.DigitalWriter.Write("input1", new Span<byte>(data, 0, 1));
                    simulator.DigitalWriter.Write("input2", new Span<byte>(data, 1, 1));
                    simulator.Tick();
                    simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));

                    Assert.AreEqual((i ^ j) & 0x0F, data[2]);
                }
            }
        }

        [Test]
        public void Xor4At6ShiftLeft()
        {
            var module = new ProgramModule();

            var entry = module.Interfaces.Add("entry");
            var initSignal = entry.ConnectionPoints.AddSignalSender("init");
            var tickSignal = entry.ConnectionPoints.AddSignalSender("tick");

            var mem1 = module.Components.AddMemory(6);
            var mem2 = module.Components.AddMemory(6);
            var mem3 = module.Components.AddMemory(6);

            var calc = module.Components.AddCalculation(PredefinedCalculationComponents.Xor);
            module.Connections.AddMemoryAccess(mem1.ConnectionPoints.AddSender(0, 4), calc.ConnectionPoints["read1"]);
            module.Connections.AddMemoryAccess(mem2.ConnectionPoints.AddSender(0, 4), calc.ConnectionPoints["read2"]);
            module.Connections.AddMemoryAccess(calc.ConnectionPoints["write"], mem3.ConnectionPoints.AddReceiver(0, 4));
            module.Connections.AddSignal(tickSignal, calc.ConnectionPoints["signal"]);

            var input1 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input1.DisplayName = "input1";
            var input2 = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input2.DisplayName = "input2";
            var output = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            output.DisplayName = "output";
            module.Connections.AddMemoryAccess(input1.ConnectionPoints["buffer"], mem1.ConnectionPoints.AddReceiver(0, 4));
            module.Connections.AddMemoryAccess(input2.ConnectionPoints["buffer"], mem2.ConnectionPoints.AddReceiver(2, 4));
            module.Connections.AddMemoryAccess(mem3.ConnectionPoints.AddSender(0, 4), output.ConnectionPoints["buffer"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();

            byte[] data = new byte[3];

            for (int i = 0; i < 256; ++i)
            {
                for (int j = 0; j < 256; ++j)
                {
                    data[0] = (byte)i;
                    data[1] = (byte)j;
                    data[2] = 0;

                    simulator.DigitalWriter.Write("input1", new Span<byte>(data, 0, 1));
                    simulator.DigitalWriter.Write("input2", new Span<byte>(data, 1, 1));
                    simulator.Tick();
                    simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));

                    Assert.AreEqual((i ^ (j << 2)) & 0x0F, data[2]);
                }
            }
        }
    }
}
