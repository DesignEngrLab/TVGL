using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Math;

namespace TVGL.IOFunctions.Step
{
    interface IConvertable
    {
        IList<Vector3> Points { get; }
        IList<int> Indices { get; }
    }
}
