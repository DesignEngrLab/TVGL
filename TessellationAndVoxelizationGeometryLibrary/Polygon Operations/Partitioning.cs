using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TVGL
{
    public enum MonotonicityChange { X, Y, Both, Neither }
    public struct MonotoneBox
    {
        public int LowIndex { get; }
        public int HiIndex { get; }
        public double Left { get; }
        public double Right { get; }
        public double Bottom { get; }
        public double Top { get; }
        public MonotonicityChange LowChange { get; }
        public MonotonicityChange HiChange { get; }
        public bool XInPositiveMonotonicity { get; }
        public bool YInPositiveMonotonicity { get; }

        public MonotoneBox(IList<PointLight> p, int lowIndex, int hiIndex, MonotonicityChange lowMonoChange,
            MonotonicityChange hiMonoChange, int xDirection, int yDirection) : this()
        {
            this.LowIndex = lowIndex;
            this.HiIndex = hiIndex;
            this.LowChange = lowMonoChange;
            this.HiChange = hiMonoChange;
            XInPositiveMonotonicity = xDirection > 0;
            YInPositiveMonotonicity = yDirection > 0;
            Left = Math.Min(p[lowIndex].X, p[hiIndex].X);
            Right = Math.Max(p[lowIndex].X, p[hiIndex].X);
            Bottom = Math.Min(p[lowIndex].Y, p[hiIndex].Y);
            Top = Math.Max(p[lowIndex].Y, p[hiIndex].Y);
        }

        public double Area()
        {
            return (Right - Left) * (Top - Bottom);
        }
    }
    public static partial class PolygonOperations
    {
        public static List<MonotoneBox> PartitionIntoMonotoneBoxes(IEnumerable<PointLight> polygon)
        {
            var p = (polygon is IList<PointLight>) ? (IList<PointLight>)polygon : polygon.ToList();
            if (p.Count < 3) return new List<MonotoneBox>() {new MonotoneBox(p,0,1,MonotonicityChange.Both,
                MonotonicityChange.Both,1,1)};
            // assume X and Y monotonicities are both positive, let GetMonotonicityChange tell us the 
            // correct direction
            var lowChange = GetMonotonicityChange(p, 0, 1, 1);
            var xDirection = (lowChange == MonotonicityChange.X || lowChange == MonotonicityChange.Both) ? -1 : 1;
            var yDirection = (lowChange == MonotonicityChange.Y || lowChange == MonotonicityChange.Both) ? -1 : 1;
            // getting the first box means working backwards into the end of the list, which also helps us
            // establish maxIndex
            var lowIndex = p.Count;
            do
            {
                lowIndex--;
                lowChange = GetMonotonicityChange(p, lowIndex, xDirection, yDirection);
            } while (lowChange == MonotonicityChange.Neither);
            var maxIndex = lowIndex = (lowIndex + 1) % p.Count;
            // maxIndex should simply be the end of the list. however, if the low worked its way into the end 
            // of the list, we will set it to lowIndex;
            var boxes = new List<MonotoneBox>();
            var hiIndex = 0;
            var hiChange = MonotonicityChange.Neither;
            while (hiIndex < maxIndex)
            {
                do
                {
                    hiIndex++;
                    hiChange = GetMonotonicityChange(p, hiIndex, xDirection, yDirection);
                } while (hiChange == MonotonicityChange.Neither && hiIndex < maxIndex);

                boxes.Add(new MonotoneBox(p, lowIndex, hiIndex, lowChange, hiChange, xDirection, yDirection));
                lowIndex = hiIndex;
                lowChange = hiChange;
                if (hiChange == MonotonicityChange.X || hiChange == MonotonicityChange.Both) xDirection *= -1;
                if (hiChange == MonotonicityChange.Y || hiChange == MonotonicityChange.Both) yDirection *= -1;
            }
            return boxes;
        }

        static MonotonicityChange GetMonotonicityChange(IList<PointLight> p, int index, int xDirection, int yDirection)
        {
            var numPoints = p.Count;
            var nextIndex = index + 1;
            if (nextIndex == numPoints) nextIndex = 0;
            var x = p[index].X;
            var y = p[index].Y;
            if (xDirection * p[nextIndex].X < xDirection * x
                && yDirection * p[nextIndex].Y < yDirection * y)
                // there are cases below where we may also return Both if one of the coords is the same
                return MonotonicityChange.Both;
            if (xDirection * p[nextIndex].X >= xDirection * x
                && yDirection * p[nextIndex].Y >= yDirection * y)
                // this captures all "neither" cases, so in the remainder you have to return X, Y, or both
                return MonotonicityChange.Neither;
            if (xDirection * p[nextIndex].X < xDirection * x)
            { //then either X or Both
                if (y == p[nextIndex].Y)
                {  // Y's are the same...need to look ahead to see which way Y goes
                    var aheadIndex = (nextIndex + 1) % numPoints;
                    while (p[aheadIndex].Y == y)
                        if (++aheadIndex == numPoints)
                            aheadIndex = 0;
                    // now at a point where the y's differ
                    if (yDirection * p[aheadIndex].Y < yDirection * y) return MonotonicityChange.Both;
                }
                //Y must be in the same sense as previous since code didn't exit at first condition
                return MonotonicityChange.X;
            }
            // finally Y must change monotoniticity (otherwise exited above), so the following condition is redundant
            //if (yDirection * y < yDirection * p[prevIndex].Y)
            // but like the X case it is possible that it is still Both...that's only if X is the same
            if (x == p[nextIndex].X)
            {  // X's are the same...need to look ahead to see which way X goes
                var aheadIndex = (nextIndex + 1) % numPoints;
                while (p[aheadIndex].X == x)
                    if (++aheadIndex == numPoints)
                        aheadIndex = 0;
                // now at a point where the y's differ
                if (xDirection * p[aheadIndex].X < xDirection * x) return MonotonicityChange.Both;
            }
            //Y must be in the same sense as previous since code didn't exit at first condition
            return MonotonicityChange.Y;
        }
    }
}
