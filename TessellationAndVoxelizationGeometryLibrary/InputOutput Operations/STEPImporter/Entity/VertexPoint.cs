using System;
using STPLoader.Implementation.Parser;

namespace TVGL.IOFunctions.Step
{
    public class VertexPoint : Entity
    {
        public string Info;
        public long PointId;

        public override void Init()
        {
            Info = ParseHelper.ParseString(Data[0]);
            PointId = ParseHelper.ParseId(Data[1]);
        }

        public override string ToString()
        {
            return String.Format("<VertexPoint({0}, {1})", Info, PointId);
        }
    }

}
