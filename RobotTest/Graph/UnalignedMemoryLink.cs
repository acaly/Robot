using LibRobot.Graph;
using LibRobot.Simulation;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RobotTest.Graph
{
    public class UnalignedMemoryLink
    {
        [Test]
        public void Test()
        {
            var module = new ProgramModule();

            var entry = module.Interfaces.Add("entry");
            var initSignal = entry.ConnectionPoints.AddSignalSender("init");
            var tickSignal = entry.ConnectionPoints.AddSignalSender("tick");

            var mem1 = module.Components.AddMemory(8);
            var mem2 = module.Components.AddMemory(4);
            var mem3 = module.Components.AddMemory(8);
            module.Connections.AddMemoryLink(mem1.ConnectionPoints.AddLink(0, 4), mem2.ConnectionPoints.AddLink());
            module.Connections.AddMemoryLink(mem2.ConnectionPoints.AddLink(), mem3.ConnectionPoints.AddLink(4, 4));

            var write = module.Components.AddExternal(ExternalComponentType.DigitalInput);
            write.DisplayName = "input";
            var read = module.Components.AddExternal(ExternalComponentType.DigitalOutput);
            read.DisplayName = "output";
            module.Connections.AddMemoryAccess(write.ConnectionPoints["buffer"], mem1.ConnectionPoints.AddReceiver());
            module.Connections.AddMemoryAccess(mem3.ConnectionPoints.AddSender(), read.ConnectionPoints["buffer"]);

            var linker = new Linker();
            linker.AddModule(module);
            var program = linker.GenerateProgram();

            var simulator = new Simulator(program);
            simulator.Start();

            byte inputVal = 0x12;
            simulator.DigitalWriter.Write("input", MemoryMarshal.CreateSpan(ref inputVal, 1));
            simulator.Tick();
            byte buffer = 0;
            simulator.DigitalReader.Read("output", MemoryMarshal.CreateSpan(ref buffer, 1));

            Assert.AreEqual(0x20, buffer);
        }
    }
}
