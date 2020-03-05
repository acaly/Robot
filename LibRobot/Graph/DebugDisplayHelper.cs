using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LibRobot.Graph
{
    public class DebugDisplayHelper<T>
    {
        public DebugDisplayHelper(IEnumerable<T> obj)
        {
            Data = obj.ToArray();
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Data;
    }
}
