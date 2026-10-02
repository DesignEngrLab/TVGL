using System;
using System.Linq;
using STPLoader.Implementation.Parser;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class Circle : Entity
    {
        /// <summary>
        /// 
        /// </summary>
        public string Info;
        /// <summary>
        /// 
        /// </summary>
        public long PointId;

        public double Radius;

        public override void Init()
        {
            Info = ParseHelper.Parse<string>(Data[0]);
            PointId = ParseHelper.ParseId(Data[1]);
            Radius = ParseHelper.Parse<double>(Data[2]);
        }

        public override string ToString()
        {
            return String.Format("<Circle({0}, {1}, {2})", Info, PointId, Radius);
        }
    }

}
