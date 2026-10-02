using System;
using System.Linq;
using STPLoader.Implementation.Parser;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class FaceOuterBound : Bound
    {
        public override string ToString()
        {
            return String.Format("<FaceOuterBound({0}, {1}, {2})", Info, EdgeLoopId, Boo);
        }
    }

}
