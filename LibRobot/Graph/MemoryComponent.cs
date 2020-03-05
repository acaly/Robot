using System;
using System.Collections.Generic;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class MemoryComponent : AbstractComponent
    {
        public class MemoryConnectionPointCollection : ConnectionPointCollection
        {
            private MemoryComponent _parent;

            internal void Init(MemoryComponent parent)
            {
                _parent = parent;
                Init(parent, null);
            }

            public ConnectionPoint AddLink()
            {
                return AddLink(0, _parent.BitSize);
            }

            public ConnectionPoint AddLink(int bitOffset, int bitLength)
            {
                if (bitOffset < 0 || bitLength <= 0 || bitOffset + bitLength > _parent.BitSize)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var ret = new ConnectionPoint(_parent, null, ConnectionPointType.MemoryLink, bitOffset, bitLength);
                Add(ret);
                return ret;
            }

            public ConnectionPoint AddSender()
            {
                return AddSender(0, _parent.BitSize);
            }

            public ConnectionPoint AddSender(int bitOffset, int bitLength)
            {
                if (bitOffset < 0 || bitLength <= 0 || bitOffset + bitLength > _parent.BitSize)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var ret = new ConnectionPoint(_parent, null, ConnectionPointType.MemorySend, bitOffset, bitLength);
                Add(ret);
                return ret;
            }

            public ConnectionPoint AddReceiver()
            {
                return AddReceiver(0, _parent.BitSize);
            }

            public ConnectionPoint AddReceiver(int bitOffset, int bitLength)
            {
                if (bitOffset < 0 || bitLength <= 0 || bitOffset + bitLength > _parent.BitSize)
                {
                    throw new ArgumentOutOfRangeException();
                }

                var ret = new ConnectionPoint(_parent, null, ConnectionPointType.MemoryReceive, bitOffset, bitLength);
                Add(ret);
                return ret;
            }
        }

        public int BitSize { get; }
        public new MemoryConnectionPointCollection ConnectionPoints { get; }

        private byte[] _initialData;
        public byte[] InitialData
        {
            get => _initialData;
            set
            {
                if (value != null && value.Length != (BitSize + 7) / 8)
                {
                    throw new ArgumentException();
                }
                _initialData = value;
            }
        }

        internal MemoryComponent(ProgramModule module, int bitSize) : base(module, new MemoryConnectionPointCollection())
        {
            BitSize = bitSize;
            ConnectionPoints = (MemoryConnectionPointCollection)base.ConnectionPoints;
            ConnectionPoints.Init(this);
        }
    }
}
