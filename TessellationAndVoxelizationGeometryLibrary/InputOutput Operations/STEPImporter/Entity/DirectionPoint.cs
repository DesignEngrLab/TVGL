using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL.IOFunctions.Step
{
    public class DirectionPoint : CartesianPoint
    {

        public override string ToString()
        {
            return String.Format("<DirectionPoint({0}, {1})", Info, Vector);
        }
    }
}
