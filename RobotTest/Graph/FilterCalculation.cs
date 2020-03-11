using LibRobot.Graph;
using LibRobot.Simulation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace RobotTest.Graph
{
    public class FilterCalculation
    {
        [Test]
        public void FilterAny()
        {
            var module = new ProgramModule();

            var entry = module.Interfaces.Add("entry");
            var initSignal = entry.ConnectionPoints.AddSignalSender("init");
            var tickSignal = entry.ConnectionPoints.AddSignalSender("tick");

            var mem1 = module.Components.AddMemory(2);
            var mem2 = module.Components.AddMemory(8);
            var mem3 = module.Components.AddMemory(8);
            mem3.InitialData = new byte[] { 0x35 };

            var filter = module.Components.AddCalculation(PredefinedCalculationComponents.FilterAny);
            var copy = module.Components.AddCalculation(PredefinedCalculationComponents.Copy);
            module.Connections.AddMemoryAccess(mem1.ConnectionPoints.AddSender(), filter.ConnectionPoints["read"]);
            module.Connections.AddSignal(tickSignal, filter.ConnectionPoints["signalIn"]);
            module.Connections.AddSignal(filter.ConnectionPoints["signalOut"], copy.ConnectionPoints["signal"]);
            module.Connections.AddMemoryAccess(mem3.ConnectionPoints.AddSender(), copy.ConnectionPoints["read"]);
            module.Connections.AddMemoryAccess(copy.ConnectionPoints["write"], mem2.ConnectionPoints.AddReceiver());

            var input = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            input.DisplayName = "input";
            var inputZ = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            inputZ.DisplayName = "inputZ";
            var output = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            output.DisplayName = "output";
            module.Connections.AddMemoryAccess(input.ConnectionPoints["buffer"], mem1.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(inputZ.ConnectionPoints["buffer"], mem2.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(mem2.ConnectionPoints.AddSender(), output.ConnectionPoints["buffer"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();

            byte[] data = new byte[3];

            data[0] = 0;

            //0 -> 0
            data[1] = 0;
            data[2] = 0;
            simulator.DigitalWriter.Write("input", new Span<byte>(data, 1, 1));
            simulator.Tick();
            simulator.Tick();
            simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));
            Assert.AreEqual(0, data[2]);

            //1 -> 35
            data[1] = 1;
            data[2] = 0;
            simulator.DigitalWriter.Write("input", new Span<byte>(data, 1, 1));
            simulator.Tick();
            simulator.Tick();
            simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));
            Assert.AreEqual(0x35, data[2]);

            //Clear
            data[1] = 0;
            data[2] = 0;
            simulator.DigitalWriter.Write("input", new Span<byte>(data, 1, 1));
            simulator.Tick();
            simulator.DigitalWriter.Write("inputZ", new Span<byte>(data, 0, 1));
            simulator.Tick();
            simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));
            Assert.AreEqual(0, data[2]);

            //2 -> 35
            data[1] = 2;
            data[2] = 0;
            simulator.DigitalWriter.Write("input", new Span<byte>(data, 1, 1));
            simulator.Tick();
            simulator.Tick();
            simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));
            Assert.AreEqual(0x35, data[2]);

            //Clear
            data[1] = 0;
            data[2] = 0;
            simulator.DigitalWriter.Write("input", new Span<byte>(data, 1, 1));
            simulator.Tick();
            simulator.DigitalWriter.Write("inputZ", new Span<byte>(data, 0, 1));
            simulator.Tick();
            simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));
            Assert.AreEqual(0, data[2]);

            //4 -> 0
            data[1] = 4;
            data[2] = 0;
            simulator.DigitalWriter.Write("input", new Span<byte>(data, 1, 1));
            simulator.Tick();
            simulator.Tick();
            simulator.DigitalReader.Read("output", new Span<byte>(data, 2, 1));
            Assert.AreEqual(0, data[2]);
        }
    }
}
