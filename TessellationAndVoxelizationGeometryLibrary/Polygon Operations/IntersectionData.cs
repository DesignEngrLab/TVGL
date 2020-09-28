using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// Class IntersectionData.
    /// </summary>
    public class PolygonInteractionRecord
    {
        private PolygonInteractionRecord(PolygonRelationship topLevelRelationship, List<SegmentIntersection> intersections,
             PolygonRelationship[] polygonRelations, Dictionary<Polygon, int> subPolygonToInt, int numPolygonsInA, int numPolygonsInB)
        {
            this.Relationship = topLevelRelationship;
            this.IntersectionData = intersections;
            this.polygonRelations = polygonRelations;
            this.subPolygonToInt = subPolygonToInt;
            this.numPolygonsInA = numPolygonsInA;
            this.numPolygonsInB = numPolygonsInB;
        }
        internal PolygonInteractionRecord(Polygon polygonA, Polygon polygonB)
        {
            var index = 0;
            this.subPolygonToInt = new Dictionary<Polygon, int>();
            foreach (var polyA in polygonA.AllPolygons)
                subPolygonToInt.Add(polyA, index++);
            this.numPolygonsInA = index;
            if (polygonB != null)
            {
                foreach (var polyB in polygonB.AllPolygons)
                    subPolygonToInt.Add(polyB, index++);
                this.numPolygonsInB = index - numPolygonsInA;
            }
            this.polygonRelations = new PolygonRelationship[numPolygonsInA * numPolygonsInB];
            this.IntersectionData = new List<SegmentIntersection>();
        }

        public PolygonRelationship Relationship { get; internal set; }
        public List<SegmentIntersection> IntersectionData { get; }
        private readonly PolygonRelationship[] polygonRelations;
        private readonly Dictionary<Polygon, int> subPolygonToInt;
        internal readonly int numPolygonsInA;
        internal readonly int numPolygonsInB;

        public IEnumerable<Polygon> AllPolygons
        {
            get
            {
                foreach (var polygon in subPolygonToInt.Keys)
                    yield return polygon;
            }
        }
        public PolygonRelationship GetRelationshipBetween(Polygon polygonA, Polygon polygonB)
        {
            var index = findLookupIndex(polygonA, polygonB);
            return polygonRelations[index];
        }
        internal void SetRelationshipBetween(int index, PolygonRelationship polygonRelationship)
        {
            //var index = findLookupIndex(polygonA, polygonB);
            polygonRelations[index] = polygonRelationship;
        }
        internal int findLookupIndex(Polygon polygonA, Polygon polygonB)
        {
            var indexA = subPolygonToInt[polygonA];
            var indexB = subPolygonToInt[polygonB];
            return indexA < indexB
                ? numPolygonsInA * (indexB - numPolygonsInA) + indexA
                : numPolygonsInA * (indexA - numPolygonsInA) + indexB;
        }
        public IEnumerable<(PolygonRelationship, bool)> GetRelationships(Polygon polygon)
        {
            var index = subPolygonToInt[polygon];
            if (index < numPolygonsInA)
            {
                var i = 0;
                foreach (var bpolygon in AllPolygons.Skip(numPolygonsInA))
                    yield return (polygonRelations[numPolygonsInA * i++ + index], bpolygon.IsPositive);
            }
            else
            {
                var i = 0;
                foreach (var aPolygon in AllPolygons.Take(numPolygonsInA))
                    yield return (polygonRelations[numPolygonsInA * (index - numPolygonsInA) + i++], aPolygon.IsPositive);
            }
        }

        /// <summary>
        /// Makes the intersection lookup table that allows us to quickly find the intersections for a given edge.
        /// </summary>
        /// <param name="numLines">The number lines.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>System.Collections.Generic.List&lt;System.Int32&gt;[].</returns>
        internal List<int>[] MakeIntersectionLookupList(int numVertices)
        {
            // now make the lookupList. One list per vertex. If the vertex does not intersect, then it is left as null.
            // this is potentially memory intensive but speeds up the matching  when creating new polygons
            var lookupList = new List<int>[numVertices];
            for (int i = 0; i < IntersectionData.Count; i++)
            {
                var intersection = IntersectionData[i];
                intersection.VisitedA = false;
                intersection.VisitedB = false;
                numVertices = intersection.EdgeA.IndexInList;
                lookupList[numVertices] ??= new List<int>();
                lookupList[numVertices].Add(i);
                numVertices = intersection.EdgeB.IndexInList;
                lookupList[numVertices] ??= new List<int>();
                lookupList[numVertices].Add(i);
            }
            return lookupList;
        }

        internal PolygonInteractionRecord InvertPolygonInRecord(ref Polygon polygon)
        {
            var tolerance = Constants.BaseTolerance * Math.Min(polygon.MaxX - polygon.MinX, polygon.MaxY - polygon.MinY);
            bool polygonIsAInInteractions = subPolygonToInt[polygon] < numPolygonsInA;
            var visitedIntersectionPairs = new HashSet<(PolygonEdge, PolygonEdge)>();
            var delimiters = PolygonBooleanBase.NumberVerticesAndGetPolygonVertexDelimiter(polygon);
            polygon = polygon.Copy(true, true);
            var allLines = polygon.AllPolygons.SelectMany(p => p.Lines).ToList();
            var newIntersections = new List<SegmentIntersection>();
            var possibleDuplicates = new List<(int, PolygonEdge, PolygonEdge)>();
            for (int i = 0; i < IntersectionData.Count; i++)
            {
                var oldIntersection = IntersectionData[i];
                var edgeA = oldIntersection.EdgeA;
                var edgeB = oldIntersection.EdgeB;
                var indexOfEdge = polygonIsAInInteractions ? edgeA.IndexInList : edgeB.IndexInList;
                var k = 0;
                while (delimiters[k] <= indexOfEdge) k++;
                indexOfEdge = delimiters[k - 1] + (delimiters[k] - indexOfEdge) - 1;
                var newFlippedEdge = allLines[indexOfEdge];
                if (oldIntersection.WhereIntersection == WhereIsIntersection.BothStarts ||
                        (polygonIsAInInteractions && oldIntersection.WhereIntersection == WhereIsIntersection.AtStartOfA) ||
                    (!polygonIsAInInteractions && oldIntersection.WhereIntersection == WhereIsIntersection.AtStartOfB))
                    newFlippedEdge = newFlippedEdge.ToPoint.StartLine;
                if (polygonIsAInInteractions)
                {
                    if (visitedIntersectionPairs.Contains((newFlippedEdge, edgeB))) continue;
                    visitedIntersectionPairs.Add((newFlippedEdge, edgeB));
                    PolygonOperations.AddIntersectionBetweenLines(newFlippedEdge, edgeB, newIntersections, possibleDuplicates, tolerance);
                }
                else
                {
                    if (visitedIntersectionPairs.Contains((edgeA, newFlippedEdge))) continue;
                    visitedIntersectionPairs.Add((edgeA, newFlippedEdge));
                    PolygonOperations.AddIntersectionBetweenLines(edgeA, newFlippedEdge, newIntersections, possibleDuplicates, tolerance);
                }
            }
            var newSubPolygonToInt = new Dictionary<Polygon, int>();
            if (!polygonIsAInInteractions)
            {
                using var newPolyEnumerator = polygon.AllPolygons.GetEnumerator();
                foreach (var keyValuePair in subPolygonToInt)
                {
                    if (keyValuePair.Value < numPolygonsInA)
                        newSubPolygonToInt.Add(keyValuePair.Key, keyValuePair.Value);
                    else
                    {
                        newPolyEnumerator.MoveNext();
                        newSubPolygonToInt.Add(newPolyEnumerator.Current, keyValuePair.Value);
                    }
                }
                return new PolygonInteractionRecord(Relationship, newIntersections, (PolygonRelationship[])polygonRelations.Clone(), newSubPolygonToInt,
                    numPolygonsInA, numPolygonsInB);
            }
            var index = 0;
            foreach (var keyValuePair in subPolygonToInt)
            {
                if (keyValuePair.Value >= numPolygonsInA)
                    newSubPolygonToInt.Add(keyValuePair.Key, index++);
            }
            foreach (var newpoly in polygon.AllPolygons)
                newSubPolygonToInt.Add(newpoly, index++);

            var newPolygonRelations = new PolygonRelationship[numPolygonsInA * numPolygonsInB];
            for (int i = 0; i < numPolygonsInA; i++)
                for (int j = 0; j < numPolygonsInB; j++)
                    newPolygonRelations[numPolygonsInB * i + j] = Constants.SwitchAAndBPolygonRelationship(polygonRelations[numPolygonsInA * j + i]);
            return new PolygonInteractionRecord(Constants.SwitchAAndBPolygonRelationship(Relationship), newIntersections, newPolygonRelations, newSubPolygonToInt,
              numPolygonsInB, numPolygonsInA);
        }


    }
}
