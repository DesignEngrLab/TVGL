using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class StepSolid : Entity
    {
        public string Name;
        public int dim;// dimension count
    }
    public class CSGStepSolid : StepSolid
    {
        public string TreeRootExpression;
        public override void Init(Dictionary<long, Entity> Data)
        {
            Name = ParseHelper.Parse<string>(Data[0]);
            TreeRootExpression = ParseHelper.Parse<string>(Data[1]);
        }
    }
    public class BREPStepSolid : StepSolid
    {
        public ClosedShell Outer;
        public override void Init(Dictionary<long, Entity> Data)
        {
            Name = ParseHelper.Parse<string>(Data[0]);
            Outer = ParseHelper.Parse<ClosedShell>(Data[1]);
        }
    }
}
