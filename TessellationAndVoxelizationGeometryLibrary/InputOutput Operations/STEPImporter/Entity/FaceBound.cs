using System;
using System.Linq;
using STPLoader.Implementation.Parser;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class FaceBound : Bound
    {
        public override string ToString()
        {
            return String.Format("<FaceBound({0}, {1}, {2})", Info, EdgeLoopId, Boo);
        }
    }

}
