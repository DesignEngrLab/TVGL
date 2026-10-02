using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.IOFunctions.Step
{
    public class Bound : Entity
    {
        public string Info;
        public long EdgeLoopId;
        public bool Boo;
        public override void Init(Dictionary<long, Entity> Data)
        {
            Info = ParseHelper.Parse<String>(Data[0]);
            EdgeLoopId = ParseHelper.ParseId(Data[1]);
            Boo = ParseHelper.Parse<bool>(Data[2]);
        }

    }

}
