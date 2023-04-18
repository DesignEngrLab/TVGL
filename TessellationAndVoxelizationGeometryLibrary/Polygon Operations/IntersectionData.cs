// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="IntersectionData.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class IntersectionData.
    /// </summary>
    public class PolygonInteractionRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonInteractionRecord"/> class.
        /// </summary>
        /// <param name="topLevelRelationship">The top level relationship.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="polygonRelations">The polygon relations.</param>
        /// <param name="subPolygonToInt">The sub polygon to int.</param>
        /// <param name="numPolygonsInA">The number polygons in a.</param>
        /// <param name="numPolygonsInB">The number polygons in b.</param>
        /// <param name="isAPositive">if set to <c>true</c> [is a positive].</param>
        /// <param name="isBPositive">if set to <c>true</c> [is b positive].</param>
        private PolygonInteractionRecord(PolygonRelationship topLevelRelationship, List<SegmentIntersection> intersections,
             PolyRelInternal[] polygonRelations, Dictionary<Polygon, int> subPolygonToInt, int numPolygonsInA, int numPolygonsInB,
            bool isAPositive, bool isBPositive)
        {
            this.Relationship = topLevelRelationship;
            this.IntersectionData = intersections;
            this.polygonRelations = polygonRelations;
            this.subPolygonToInt = subPolygonToInt;
            this.numPolygonsInA = numPolygonsInA;
            this.numPolygonsInB = numPolygonsInB;
            this.AIsPositive = isAPositive;
            this.BIsPositive = isBPositive;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonInteractionRecord"/> class.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        internal PolygonInteractionRecord(Polygon polygonA, Polygon polygonB)
        {
            var index = 0;
            this.subPolygonToInt = new Dictionary<Polygon, int>();
            foreach (var polyA in polygonA.AllPolygons)
                subPolygonToInt.Add(polyA, index++);
            this.numPolygonsInA = index;
            if (polygonA == polygonB)
            {
                this.Relationship = PolygonRelationship.Equal;
                return;
            }
            if (polygonB != null)
            {
                foreach (var polyB in polygonB.AllPolygons)
                    subPolygonToInt.Add(polyB, index++);
                this.numPolygonsInB = index - numPolygonsInA;
            }
            this.polygonRelations = new PolyRelInternal[numPolygonsInA * numPolygonsInB];
            this.IntersectionData = new List<SegmentIntersection>();
            this.Relationship = PolygonRelationship.Separated;
            this.AIsPositive = polygonA.IsPositive;
            if (polygonB != null) this.BIsPositive = polygonB.IsPositive;
        }

        /// <summary>
        /// Gets the relationship.
        /// </summary>
        /// <value>The relationship.</value>
        public PolygonRelationship Relationship { get; internal set; }
        /// <summary>
        /// Gets the intersection data.
        /// </summary>
        /// <value>The intersection data.</value>
        public List<SegmentIntersection> IntersectionData { get; }
        /// <summary>
        /// The polygon relations
        /// </summary>
        private readonly PolyRelInternal[] polygonRelations;
        /// <summary>
        /// The sub polygon to int
        /// </summary>
        private readonly Dictionary<Polygon, int> subPolygonToInt;
        /// <summary>
        /// The number polygons in a
        /// </summary>
        internal readonly int numPolygonsInA;
        /// <summary>
        /// The number polygons in b
        /// </summary>
        internal readonly int numPolygonsInB;
        /// <summary>
        /// a is positive
        /// </summary>
        internal readonly bool AIsPositive;
        /// <summary>
        /// The b is positive
        /// </summary>
        internal readonly bool BIsPositive;
        /// <summary>
        /// Gets a value indicating whether [coincident edges].
        /// </summary>
        /// <value><c>true</c> if [coincident edges]; otherwise, <c>false</c>.</value>
        public bool CoincidentEdges { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [edges cross].
        /// </summary>
        /// <value><c>true</c> if [edges cross]; otherwise, <c>false</c>.</value>
        public bool EdgesCross { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [coincident vertices].
        /// </summary>
        /// <value><c>true</c> if [coincident vertices]; otherwise, <c>false</c>.</value>
        public bool CoincidentVertices { get; internal set; }

        /// <summary>
        /// Intersections the will be empty.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool IntersectionWillBeEmpty()
        {
            if (Relationship == PolygonRelationship.Intersection ||
                Relationship == PolygonRelationship.Equal)
                return false;
            if (Relationship == PolygonRelationship.EqualButOpposite) return true;
            if (Relationship == PolygonRelationship.Separated) return AIsPositive && BIsPositive;
            //if either or both are negative, then the separation actually means an intersection
            return !((AIsPositive && Relationship == PolygonRelationship.BInsideA) ||
                (BIsPositive && Relationship == PolygonRelationship.AInsideB) ||
                (!AIsPositive && Relationship == PolygonRelationship.BIsInsideHoleOfA) ||
                (!BIsPositive && Relationship == PolygonRelationship.AIsInsideHoleOfB));
        }


        /// <summary>
        /// Gets all polygons.
        /// </summary>
        /// <value>All polygons.</value>
        public IEnumerable<Polygon> AllPolygons
        {
            get
            {
                foreach (var polygon in subPolygonToInt.Keys)
                    yield return polygon;
            }
        }
        /// <summary>
        /// Gets the relationship between.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <returns>PolyRelInternal.</returns>
        internal PolyRelInternal GetRelationshipBetween(Polygon polygonA, Polygon polygonB)
        {
            if (polygonRelations == null) return PolyRelInternal.Equal;
            var index = findLookupIndex(polygonA, polygonB);
            return polygonRelations[index];
        }
        /// <summary>
        /// Sets the relationship between.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="newRel">The new relative.</param>
        internal void SetRelationshipBetween(int index, PolyRelInternal newRel)
        {
            polygonRelations[index] = newRel;
            //Separated
            //AInsideB
            //AIsInsideHoleOfB
            //BInsideA
            //BIsInsideHoleOfA
            //Intersection
            //Equal
            //EqualButOpposite
            // okay need to compare all possibilities of the PolygonRelationship enum to itself
            // there are 8 values so that 8 x 8 = 64 possibilities.
            // let's see how this breaks down
            if (this.Relationship == PolygonRelationship.Intersection) return;
            // if already Intersection, then nothing to do (that's 8)
            if (newRel == PolyRelInternal.Separated) return;
            // if the newRel is Separated then no update as well (7 more)
            var newRelationship = (PolygonRelationship)(((int)newRel) & 248);
            if (newRelationship == Relationship) return;
            // if they're the same then nothing to do (that's 6 more since previous conditions would have caught 2 of these
            // down to 43
            if (newRelationship == PolygonRelationship.Intersection ||
                ((newRelationship == PolygonRelationship.AInsideB || newRelationship == PolygonRelationship.AIsInsideHoleOfB) &&
                (Relationship == PolygonRelationship.BInsideA || Relationship == PolygonRelationship.BIsInsideHoleOfA)) ||
                ((newRelationship == PolygonRelationship.BInsideA || newRelationship == PolygonRelationship.BIsInsideHoleOfA) &&
                (Relationship == PolygonRelationship.AInsideB || Relationship == PolygonRelationship.AIsInsideHoleOfB)))
                this.Relationship = PolygonRelationship.Intersection;
            // how many more pairs are these: 7 + 8....down to 28
            else if (Relationship == PolygonRelationship.Separated)
                Relationship = newRelationship; //6 more here (i think...not included newRel is Separated or Intersection
            else if (newRelationship == PolygonRelationship.Equal) return; // current Relationship would be more descriptive
            // so finding out that a subpolygon in Equal doesn't change anything (that 5 more cases)
            else if (newRelationship == PolygonRelationship.EqualButOpposite)
            {
                if (Relationship == PolygonRelationship.BInsideA)
                    Relationship = PolygonRelationship.BIsInsideHoleOfA;
                if (Relationship == PolygonRelationship.AInsideB)
                    Relationship = PolygonRelationship.AIsInsideHoleOfB;
                // really need to check the new EqualButOpposite with AInsideB, AIsInsideHoleOfB, BInsideA,
                // BIsInsideHoleOfA, & Equal (so that's 5 additional cases) but the above two subcases are the
                // only ways this can ever happen, right?
            }
            //R = BInsideA , Nrel = BIsInsideHoleOfA
            else if (newRelationship == PolygonRelationship.BIsInsideHoleOfA && Relationship == PolygonRelationship.BInsideA)
                Relationship = PolygonRelationship.BIsInsideHoleOfA;
            //R = AInsideB , Nrel = AIsInsideHoleOfB
            else if (newRelationship == PolygonRelationship.AIsInsideHoleOfB && Relationship == PolygonRelationship.AInsideB)
                Relationship = PolygonRelationship.AIsInsideHoleOfB;
            // there are 10 left, all of which are either impossible or have no effect (I think). These are listed below. 
            // The first 4 are possible and we need to be careful if the outer positive polygons match, then when we compare
            // the inner hole of A to the outer of B, we will get the first condition below, but we don't want to change the 
            // full Relationship unless we are sure it's not identical. This requires us to have one more function at the end
            // which is DefineOverallInteractionFromFinalListOfSubInteractions
            //R = Equal , Nrel = AInsideB
            //R = Equal , Nrel = AIsInsideHoleOfB
            //R = Equal , Nrel = BInsideA
            //R = Equal , Nrel = BIsInsideHoleOfA
            //R = AIsInsideHoleOfB , Nrel = AInsideB
            //R = BIsInsideHoleOfA , Nrel = BInsideA
            //R = EqualButOpposite , Nrel = AInsideB
            //R = EqualButOpposite , Nrel = AIsInsideHoleOfB
            //R = EqualButOpposite , Nrel = BInsideA
            //R = EqualButOpposite , Nrel = BIsInsideHoleOfA
            return;
        }
        /// <summary>
        /// Defines the overall interaction from final list of sub interactions.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        internal void DefineOverallInteractionFromFinalListOfSubInteractions()
        {
            CoincidentEdges = polygonRelations.Any(pr => (pr & PolyRelInternal.CoincidentEdges) != 0b0);
            EdgesCross = polygonRelations.Any(pr => (pr & PolyRelInternal.EdgesCross) != 0b0);
            CoincidentVertices = polygonRelations.Any(pr => (pr & PolyRelInternal.CoincidentVertices) != 0b0);


            if (polygonRelations[0] == PolyRelInternal.Equal)
            {
                var matchedPolygonBIndices = new HashSet<int>();
                var subPolysA = this.AllPolygons.Skip(1).Take(numPolygonsInA - 1).ToList();
                var subPolysB = this.AllPolygons.Skip(numPolygonsInA + 1).ToList();
                foreach (var subPolyA in subPolysA)
                {
                    for (int i = 0; i < numPolygonsInB - 1; i++)
                    {
                        if (matchedPolygonBIndices.Contains(i)) continue;
                        if (polygonRelations[findLookupIndex(subPolyA, subPolysB[i])] == PolyRelInternal.Equal)
                        {
                            matchedPolygonBIndices.Add(i);
                            break;
                        }
                    }
                }
                if (matchedPolygonBIndices.Count() == numPolygonsInB - 1) // then we found a unique match for each hole in
                    // the polygon
                    Relationship = PolygonRelationship.Equal;
                else
                {
                    throw new NotImplementedException();
                    // now we hvae a tricky situation that will need a bunch more lines to code. I fear I'm missing an
                    // easier fix to the problem. For example, the outer polygons can be equal, and then a b-hole can be
                    // inside an a-hole, which actually means the overall relationship would be AIsInsideB
                }
            }
        }

        /// <summary>
        /// Finds the index of the lookup.
        /// </summary>
        /// <param name="polygon1">The polygon1.</param>
        /// <param name="polygon2">The polygon2.</param>
        /// <returns>System.Int32.</returns>
        internal int findLookupIndex(Polygon polygon1, Polygon polygon2)
        {
            var index1 = subPolygonToInt[polygon1];
            var index2 = subPolygonToInt[polygon2];
            return index1 < index2
                ? numPolygonsInA * (index2 - numPolygonsInA) + index1
                : numPolygonsInA * (index1 - numPolygonsInA) + index2;
        }
        /// <summary>
        /// Gets the relationships.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>IEnumerable&lt;System.ValueTuple&lt;PolyRelInternal, System.Boolean&gt;&gt;.</returns>
        internal IEnumerable<(PolyRelInternal, bool)> GetRelationships(Polygon polygon)
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
        /// <param name="numVertices">The number vertices.</param>
        /// <returns>System.Collections.Generic.List&lt;System.Int32&gt;[].</returns>
        internal List<int>[] MakeIntersectionLookupList(int numVertices)
        {
            // now make the lookupList. One list per vertex. If the vertex does not intersect, then it is left as null.
            // this is potentially memory intensive but speeds up the matching  when creating new polygons
            var lookupList = new List<int>[numVertices];
            if (IntersectionData == null) return lookupList;
            for (int i = 0; i < IntersectionData.Count; i++)
            {
                var intersection = IntersectionData[i];
                intersection.VisitedA = false;
                intersection.VisitedB = false;
                var index = intersection.EdgeA.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
                index = intersection.EdgeB.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
            }
            return lookupList;
        }

        /// <summary>
        /// Inverts the polygon in record.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="invertedPolygon">The inverted polygon.</param>
        /// <returns>PolygonInteractionRecord.</returns>
        internal PolygonInteractionRecord InvertPolygonInRecord(Polygon polygon, out Polygon invertedPolygon)
        {
            bool polygonAIsInverted = subPolygonToInt[polygon] < numPolygonsInA;
            var visitedIntersectionPairs = new HashSet<(PolygonEdge, PolygonEdge)>();
            var delimiters = PolygonBooleanBase.NumberVerticesAndGetPolygonVertexDelimiter(polygon);
            invertedPolygon = polygon.Copy(true, true);
            var allLines = invertedPolygon.AllPolygons.SelectMany(p => p.Edges).ToList();
            var newIntersections = new List<SegmentIntersection>();
            var possibleDuplicates = new List<(int, PolygonEdge, PolygonEdge)>();
            for (int i = 0; i < IntersectionData.Count; i++)
            {
                var oldIntersection = IntersectionData[i];
                var edgeA = oldIntersection.EdgeA;
                var edgeB = oldIntersection.EdgeB;
                var indexOfEdge = polygonAIsInverted ? edgeA.IndexInList : edgeB.IndexInList;
                var k = 0;
                while (delimiters[k] <= indexOfEdge) k++;
                indexOfEdge = delimiters[k - 1] + (delimiters[k] - indexOfEdge) - 1;
                var newFlippedEdge = allLines[indexOfEdge];
                if (oldIntersection.WhereIntersection == WhereIsIntersection.BothStarts ||
                        (polygonAIsInverted && oldIntersection.WhereIntersection == WhereIsIntersection.AtStartOfA) ||
                    (!polygonAIsInverted && oldIntersection.WhereIntersection == WhereIsIntersection.AtStartOfB))
                    newFlippedEdge = newFlippedEdge.ToPoint.StartLine;
                if (polygonAIsInverted)
                {
                    if (visitedIntersectionPairs.Contains((newFlippedEdge, edgeB))) continue;
                    visitedIntersectionPairs.Add((newFlippedEdge, edgeB));
                    PolygonOperations.AddIntersectionBetweenLines(newFlippedEdge, edgeB, newIntersections, possibleDuplicates, polygon.NumSigDigits, false, false);
                }
                else
                {
                    if (visitedIntersectionPairs.Contains((edgeA, newFlippedEdge))) continue;
                    visitedIntersectionPairs.Add((edgeA, newFlippedEdge));
                    PolygonOperations.AddIntersectionBetweenLines(edgeA, newFlippedEdge, newIntersections, possibleDuplicates, polygon.NumSigDigits, false, false);
                }
            }
            var newSubPolygonToInt = new Dictionary<Polygon, int>();
            var isAPositive = invertedPolygon.IsPositive;
            var isBPositive = invertedPolygon.IsPositive;
            if (!polygonAIsInverted)
            {
                using var newPolyEnumerator = invertedPolygon.AllPolygons.GetEnumerator();
                foreach (var keyValuePair in subPolygonToInt)
                {
                    if (keyValuePair.Value == 0) isAPositive = keyValuePair.Key.IsPositive;
                    if (keyValuePair.Value < numPolygonsInA)
                        newSubPolygonToInt.Add(keyValuePair.Key, keyValuePair.Value);
                    else
                    {
                        newPolyEnumerator.MoveNext();
                        newSubPolygonToInt.Add(newPolyEnumerator.Current, keyValuePair.Value);
                    }
                }
                return new PolygonInteractionRecord(Relationship, newIntersections, (PolyRelInternal[])polygonRelations.Clone(), newSubPolygonToInt,
                    numPolygonsInA, numPolygonsInB, isAPositive, isBPositive)
                {
                    CoincidentEdges = this.CoincidentEdges,
                    CoincidentVertices = this.CoincidentVertices,
                    EdgesCross = this.EdgesCross
                };
            }
            var index = 0;
            foreach (var keyValuePair in subPolygonToInt)
            {
                if (keyValuePair.Value == numPolygonsInA) isBPositive = keyValuePair.Key.IsPositive;
                if (keyValuePair.Value >= numPolygonsInA)
                    newSubPolygonToInt.Add(keyValuePair.Key, index++);
            }
            foreach (var newpoly in invertedPolygon.AllPolygons)
                newSubPolygonToInt.Add(newpoly, index++);

            var newPolygonRelations = new PolyRelInternal[numPolygonsInA * numPolygonsInB];
            for (int i = 0; i < numPolygonsInA; i++)
                for (int j = 0; j < numPolygonsInB; j++)
                    newPolygonRelations[numPolygonsInB * i + j] = Constants.SwitchAAndBPolygonRelationship(polygonRelations[numPolygonsInA * j + i]);
            return new PolygonInteractionRecord((PolygonRelationship)Constants.SwitchAAndBPolygonRelationship((PolyRelInternal)Relationship),
                newIntersections, newPolygonRelations, newSubPolygonToInt,
              numPolygonsInB, numPolygonsInA, isBPositive, isAPositive)
            {
                CoincidentEdges = this.CoincidentEdges,
                CoincidentVertices = this.CoincidentVertices,
                EdgesCross = this.EdgesCross
            };
        }

    }
}