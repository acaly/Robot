using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace LibRobot.Graph
{
    public abstract class AbstractComponent
    {
        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(DebugDisplayHelper<ConnectionPoint>))]
        public class ConnectionPointCollection : ChildrenCollection<ConnectionPoint>, IDictionary<string, ConnectionPoint>
        {
            private AbstractComponent _parent;

            internal void Init(AbstractComponent parent, IReadOnlyDictionary<string, ConnectionPointType> fixedValues)
            {
                if (fixedValues == null)
                {
                    fixedValues = new Dictionary<string, ConnectionPointType>();
                }

                Dictionary = new Dictionary<string, ConnectionPoint>();
                foreach (var cpt in fixedValues)
                {
                    var cp = new ConnectionPoint(parent, cpt.Key, cpt.Value);
                    Dictionary.Add(cpt.Key, cp);
                    Add(cp);
                }

                _parent = parent;
            }

            public override bool Remove(ConnectionPoint item)
            {
                if (item.Name != null && Dictionary.ContainsKey(item.Name))
                {
                    throw new InvalidOperationException();
                }
                return base.Remove(item);
            }

            public override void Clear()
            {
                for (int i = Data.Count - 1; i >= 0; --i)
                {
                    if (Data[i].Name != null && Dictionary.ContainsKey(Data[i].Name))
                    {
                        continue;
                    }
                    BeforeRemove(Data[i]);
                    Data.RemoveAt(i);
                }
            }

            protected override void BeforeRemove(ConnectionPoint item)
            {
                if (item.Connection != null)
                {
                    //Only remove the connection. Leave the other ConnectionPoint.
                    _parent.Module.Connections.Remove(item.Connection);
                }
            }

            private Dictionary<string, ConnectionPoint> Dictionary;

            public ConnectionPoint this[string key]
            {
                get => Dictionary[key];
                set => Dictionary[key] = value;
            }
            public ICollection<string> Keys => Dictionary.Keys;
            public ICollection<ConnectionPoint> Values => Dictionary.Values;

            bool ICollection<KeyValuePair<string, ConnectionPoint>>.IsReadOnly => true;

            bool IDictionary<string, ConnectionPoint>.ContainsKey(string key)
            {
                return Dictionary.ContainsKey(key);
            }
            bool IDictionary<string, ConnectionPoint>.TryGetValue(string key, out ConnectionPoint value)
            {
                return Dictionary.TryGetValue(key, out value);
            }
            bool ICollection<KeyValuePair<string, ConnectionPoint>>.Contains(KeyValuePair<string, ConnectionPoint> item)
            {
                return ((IDictionary<string, ConnectionPoint>)Dictionary).Contains(item);
            }
            void ICollection<KeyValuePair<string, ConnectionPoint>>.CopyTo(KeyValuePair<string, ConnectionPoint>[] array, int arrayIndex)
            {
                ((IDictionary<string, ConnectionPoint>)Dictionary).CopyTo(array, arrayIndex);
            }
            IEnumerator<KeyValuePair<string, ConnectionPoint>> IEnumerable<KeyValuePair<string, ConnectionPoint>>.GetEnumerator()
            {
                return Dictionary.GetEnumerator();
            }

            void IDictionary<string, ConnectionPoint>.Add(string key, ConnectionPoint value)
            {
                throw new InvalidOperationException();
            }
            void ICollection<KeyValuePair<string, ConnectionPoint>>.Add(KeyValuePair<string, ConnectionPoint> item)
            {
                throw new InvalidOperationException();
            }
            bool IDictionary<string, ConnectionPoint>.Remove(string key)
            {
                throw new InvalidOperationException();
            }
            bool ICollection<KeyValuePair<string, ConnectionPoint>>.Remove(KeyValuePair<string, ConnectionPoint> item)
            {
                throw new InvalidOperationException();
            }
            void ICollection<KeyValuePair<string, ConnectionPoint>>.Clear()
            {
                throw new InvalidOperationException();
            }
        }

        public ProgramModule Module { get; }
        public ConnectionPointCollection ConnectionPoints { get; }
        public string DisplayName { get; set; }

        protected AbstractComponent(ProgramModule module, Dictionary<string, ConnectionPointType> fixedConnectionPoints)
        {
            Module = module;
            ConnectionPoints = new ConnectionPointCollection();
            ConnectionPoints.Init(this, fixedConnectionPoints);
        }

        protected AbstractComponent(ProgramModule module, ConnectionPointCollection customCollection)
        {
            Module = module;
            ConnectionPoints = customCollection;
        }
    }
}
