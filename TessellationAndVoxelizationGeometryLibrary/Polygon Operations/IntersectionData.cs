using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    public readonly struct IntersectionData
    {
        public readonly PolygonSegment segmentA;
        public readonly PolygonSegment segmentB;
        public readonly Vector2 intersectCoordinates;
        public readonly PolygonSegmentRelationship relationship;

        public IntersectionData(PolygonSegment segmentA, PolygonSegment segmentB, Vector2 intersectionPoint, PolygonSegmentRelationship relationship)
        {
            this.segmentA = segmentA;
            this.segmentB = segmentB;
            this.intersectCoordinates = intersectionPoint;
            this.relationship = relationship;
        }
    }
}
