using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class CalculationComponentType
    {
        public ISimulationHandler SimulationHandler { get; }
        public string Name { get; }
        public IReadOnlyDictionary<string, ConnectionPointType> ConnectionPoints { get; }
        public ConnectionPointType? AdditionalConnectionPointType { get; }

        public CalculationComponentType(string name, ISimulationHandler simulationHandler,
            IDictionary<string, ConnectionPointType> connectionsPoints)
        {
            SimulationHandler = simulationHandler;
            Name = name;
            ConnectionPoints = new ReadOnlyDictionary<string, ConnectionPointType>(connectionsPoints);
        }

        public CalculationComponentType(string name, ISimulationHandler simulationHandler,
            IDictionary<string, ConnectionPointType> connectionsPoints, ConnectionPointType additional)
        {
            SimulationHandler = simulationHandler;
            Name = name;
            ConnectionPoints = new ReadOnlyDictionary<string, ConnectionPointType>(connectionsPoints);
            AdditionalConnectionPointType = additional;
            if (connectionsPoints.Values.Contains(additional))
            {
                throw new ArgumentException("duplicated type in fixed and additional connection types");
            }
        }
    }
}
