using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL.IOFunctions.Step
{
    public class AdvancedFace : Entity
    {
        public string Info;
        public IList<long> BoundIds;
        public long SurfaceId;
        public bool Boo;

        public override void Init(Dictionary<long, Entity> Data)
        {
            Info = ParseHelper.Parse<string>(Data[0]);
            BoundIds = ParseHelper.ParseList<string>(Data[1]).Select(ParseHelper.ParseId).ToList();
            SurfaceId = ParseHelper.ParseId(Data[2]);
            Boo = ParseHelper.Parse<bool>(Data[3]);
        }

        public override string ToString()
        {
            return String.Format("<AdvancedFace({0}, {1}, {2}, {3})", Info, BoundIds, SurfaceId, Boo);
        }
    }

}
