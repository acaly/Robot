using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibRobot.Graph
{
    [DebuggerDisplay("Count = {Count}")]
    public abstract class ChildrenCollection<T> : IEnumerable<T>
    {
        protected readonly List<T> Data = new List<T>();
        public int Count => Data.Count;

        internal ChildrenCollection()
        {
        }

        public virtual void Clear()
        {
            for (int i = Data.Count - 1; i >= 0; --i)
            {
                BeforeRemove(Data[i]);
                Data.RemoveAt(i);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        public virtual bool Remove(T item)
        {
            if (Data.Contains(item))
            {
                BeforeRemove(item);
                Data.Remove(item);

                //item might have been removed by BeforeRemove. Always return true.
                return true;
            }
            else
            {
                return false;   
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected void Add(T item)
        {
            Data.Add(item);
        }

        protected abstract void BeforeRemove(T item);
    }
}
