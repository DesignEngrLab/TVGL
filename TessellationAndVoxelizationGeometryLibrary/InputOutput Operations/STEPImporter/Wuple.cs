using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL.IOFunctions.Step
{
    public class Wuple<T1, T2>
    {
        public T1 First { get; private set; }
        public T2 Second { get; private set; }
        internal Wuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }

    public static class Wuple
    {
        public static Wuple<T1, T2> New<T1, T2>(T1 first, T2 second)
        {
            var tuple = new Wuple<T1, T2>(first, second);
            return tuple;
        }

        public static Wuple<int, List<int>> AggregateIndices<T>(Wuple<int, List<int>> last, Wuple<IList<T>, IList<int>> next)
        {
            var offset = last.First;
            var indices = last.Second;
            indices.AddRange(next.Second.Select(i => i + offset));
            return New(offset + next.First.Count, indices);
        }
    }
}
