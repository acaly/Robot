using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class ComponentSimulationEnvironment
    {
        private readonly byte[] _storage;

        private readonly Simulator _simulator;
        private readonly Dictionary<string, Tuple<int, ConnectionPointType>> _connections;

        private readonly int[] _additionalId;
        private readonly ConnectionPointType? _additionalType;

        public ComponentSimulationEnvironment(Simulator simulator,
            Dictionary<string, Tuple<int, ConnectionPointType>> connections,
            int[] additional, CalculationComponentType type)
        {
            _simulator = simulator;

            _connections = connections;

            _additionalId = additional;
            _additionalType = type.AdditionalConnectionPointType;

            _storage = Array.Empty<byte>();
            var storageSize = type.SimulationHandler.GetStorageSize(this);
            if (storageSize / 8 * 8 != storageSize)
            {
                throw new NotImplementedException("unaligned memory not supported");
            }
            _storage = new byte[storageSize / 8];
        }

        public int GetAdditionalCount()
        {
            return _additionalId.Length;
        }

        public int GetConnectionPointSize(string connectionPoint)
        {
            if (!_connections.TryGetValue(connectionPoint, out var id) ||
                id.Item2 != ConnectionPointType.MemoryReceive && id.Item2 != ConnectionPointType.MemorySend)
            {
                throw new KeyNotFoundException();
            }
            return _simulator.GetMemorySize(id.Item1);
        }

        public int GetAdditionalConnectionPointSize(int index)
        {
            if (!_additionalType.HasValue ||
                _additionalType != ConnectionPointType.SignalReceive && _additionalType != ConnectionPointType.MemorySend ||
                index >= _additionalId.Length)
            {
                throw new KeyNotFoundException();
            }
            return _simulator.GetMemorySize(_additionalId[index]);
        }

        public Span<byte> GetStorage()
        {
            return new Span<byte>(_storage, 0, _storage.Length);
        }

        public void Read(string connectionPoint, Span<byte> buffer)
        {
            if (!_connections.TryGetValue(connectionPoint, out var id) ||
                id.Item2 != ConnectionPointType.MemoryReceive ||
                !_simulator.ReadMemory(id.Item1, buffer, false))
            {
                throw new InvalidOperationException();
            }
        }

        public void ReadAdditional(int index, Span<byte> buffer)
        {
            if (!_additionalType.HasValue ||
                index >= _additionalId.Length ||
                _additionalType != ConnectionPointType.MemoryReceive ||
                !_simulator.ReadMemory(_additionalId[index], buffer, false))
            {
                throw new InvalidOperationException();
            }
        }

        public void Write(string connectionPoint, Span<byte> data)
        {
            if (!_connections.TryGetValue(connectionPoint, out var id) ||
                id.Item2 != ConnectionPointType.MemorySend ||
                !_simulator.WriteMemory(id.Item1, data, false))
            {
                throw new InvalidOperationException();
            }
        }

        public void WriteAdditional(int index, Span<byte> data)
        {
            if (!_additionalType.HasValue ||
                index >= _additionalId.Length ||
                _additionalType != ConnectionPointType.MemorySend ||
                !_simulator.WriteMemory(_additionalId[index], data, false))
            {
                throw new InvalidOperationException();
            }
        }

        public void Trigger(string name)
        {
            if (!_connections.TryGetValue(name, out var id) ||
                id.Item2 != ConnectionPointType.SignalSend ||
                !_simulator.Trigger(id.Item1))
            {
                throw new InvalidOperationException();
            }
        }

        public void TriggerAdditional(int index)
        {
            if (!_additionalType.HasValue ||
                index >= _additionalId.Length ||
                _additionalType != ConnectionPointType.SignalSend ||
                !_simulator.Trigger(_additionalId[index]))
            {
                throw new InvalidOperationException();
            }
        }
    }
}
