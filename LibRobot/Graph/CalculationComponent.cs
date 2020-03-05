using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace LibRobot.Graph
{
    public sealed class CalculationComponent : AbstractComponent
    {
        public sealed class CalculationConnectionPointCollection : ConnectionPointCollection
        {
            internal CalculationComponent _parent;

            internal void Init(CalculationComponent parent)
            {
                _parent = parent;
                Init(parent, parent.Type.ConnectionPoints);
            }

            public ConnectionPoint AddAdditional()
            {
                if (!_parent.Type.AdditionalConnectionPointType.HasValue)
                {
                    throw new InvalidOperationException();
                }
                var ret = new ConnectionPoint(_parent, null, _parent.Type.AdditionalConnectionPointType.Value);
                Add(ret);
                return ret;
            }
        }

        public CalculationComponentType Type { get; }
        public new CalculationConnectionPointCollection ConnectionPoints { get; }

        internal CalculationComponent(ProgramModule module, CalculationComponentType type)
            : base(module, new CalculationConnectionPointCollection())
        {
            Type = type;
            ConnectionPoints = (CalculationConnectionPointCollection)base.ConnectionPoints;
            ConnectionPoints.Init(this);
        }
    }
}
