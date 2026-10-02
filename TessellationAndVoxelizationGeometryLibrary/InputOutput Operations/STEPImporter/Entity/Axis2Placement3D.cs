using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public class Axis2Placement3D : Entity
    {
        public string Name;
        public CartesianPoint Location;
        public DirectionPoint Axis;
        public DirectionPoint RefDirection;

        public override void Init(Dictionary<long, Entity> Data)
        {
            Name = ParseHelper.ParseString(Data[0]);
            Location = ParseHelper.Parse<CartesianPoint>(Data[1]);
            Axis = ParseHelper.Parse<DirectionPoint>(Data[2]);
            RefDirection = ParseHelper.Parse<DirectionPoint>(Data[3]);
        }

        public override string ToString()
        {
            return String.Format("<Axis2Placement3D({0}, {1})", Name, Location, Axis, RefDirection);
        }
    }

}
