using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph
{
    public interface ISimulationHandler
    {
        //First step is after triggered, the calculation component read memeory values.
        //Second step is after all components read their data, each of those requiring a write, writes memory values.

        int GetStorageSize(ComponentSimulationEnvironment env);

        void Triggered(ComponentSimulationEnvironment env, string name) { }
        void Read(ComponentSimulationEnvironment env);
        void Write(ComponentSimulationEnvironment env);
    }
}
