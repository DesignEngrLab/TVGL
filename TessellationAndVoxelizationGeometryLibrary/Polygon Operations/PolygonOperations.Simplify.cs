// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        #region Simplify
        #region RemoveCollinearEdges
        /// <summary>
        /// Simplifies the specified polygons by removing vertices that have collinear edges.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="allowableChangeInAreaFraction">The allowable change in area fraction.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static IEnumerable<Polygon> RemoveCollinearEdges(this IEnumerable<Polygon> polygons)
        {
            return polygons.Select(poly => poly.RemoveCollinearEdges());
        }
        /// <summary>
        /// Simplifies the specified polygons by removing vertices that have collinear edges.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="allowableChangeInAreaFraction">The allowable change in area fraction.</param>
        /// <returns>Polygon.</returns>
        public static Polygon RemoveCollinearEdges(this Polygon polygon)
        {
            var simplifiedPositivePolygon = new Polygon(polygon.Path.RemoveCollinearEdges());
            foreach (var polygonHole in polygon.InnerPolygons)
                simplifiedPositivePolygon.AddInnerPolygon(new Polygon(polygonHole.Path.RemoveCollinearEdges()));
            return simplifiedPositivePolygon;
        }

        /// <summary>        
        /// Simplifies the specified polygons by removing vertices that have collinear edges.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        public static IEnumerable<List<Vector2>> RemoveCollinearEdges(this IEnumerable<IEnumerable<Vector2>> paths)
        {
            return paths.Select(p => RemoveCollinearEdges(p));
        }

        /// <summary>
        /// Simplifies the specified polygons by removing vertices that have collinear edges.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Vector2> RemoveCollinearEdges(this IEnumerable<Vector2> path)
        {
            var polygon = path.ToList();
            var forwardPoint = polygon[0];
            var currentPoint = polygon[^1];
            for (int i = polygon.Count - 1; i >= 0; i--)
            {
                var backwardPoint = i == 0 ? polygon[^1] : polygon[i - 1];
                var cross = (currentPoint - backwardPoint).Cross(forwardPoint - currentPoint);
                if (cross.IsNegligible()) polygon.RemoveAt(i);
                else forwardPoint = currentPoint;
                currentPoint = backwardPoint;
            }
            return polygon;
        }

        /// <summary>
        /// Simplifies the specified polygons by removing vertices that have collinear edges.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Vector2> RemoveCollinearEdgesDestructiveList(this List<Vector2> path)
        {
            var forwardPoint = path[0];
            var currentPoint = path[^1];
            for (int i = path.Count - 1; i >= 0; i--)
            {
                var backwardPoint = i == 0 ? path[^1] : path[i - 1];
                var cross = (currentPoint - backwardPoint).Cross(forwardPoint - currentPoint);
                if (cross.IsNegligible()) path.RemoveAt(i);
                else forwardPoint = currentPoint;
                currentPoint = backwardPoint;
            }
            return path;
        }

        #endregion

        #region SimplifyMinLength
        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="allowableChangeInAreaFraction">The allowable change in area fraction.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static IEnumerable<Polygon> SimplifyMinLength(this IEnumerable<Polygon> polygons, double minAllowableLength)
        {
            return polygons.Select(poly => poly.SimplifyMinLength(minAllowableLength));
        }

        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minAllowableLength">The allowable change in area fraction.</param>
        /// <returns>Polygon.</returns>
        public static Polygon SimplifyMinLength(this Polygon polygon, double minAllowableLength)
        {
            var simplifiedPositivePolygon = new Polygon(polygon.Path.SimplifyMinLength(minAllowableLength));
            foreach (var polygonHole in polygon.InnerPolygons)
                simplifiedPositivePolygon.AddInnerPolygon(new Polygon(polygonHole.Path.SimplifyMinLength(minAllowableLength)));
            return simplifiedPositivePolygon;
        }

        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="minAllowableLength">The allowable change in area fraction.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        public static IEnumerable<IEnumerable<Vector2>> SimplifyMinLength(this IEnumerable<IEnumerable<Vector2>> paths, double minAllowableLength)
        {
            return paths.Select(p => SimplifyMinLength(p, minAllowableLength));
        }

        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2> SimplifyMinLength(this IEnumerable<Vector2> path, double minAllowableLength)
        {
            // first remove collinear points
            var polygon = path.RemoveCollinearEdges();
            var numPoints = polygon.Count;

            #region build initial list of edge lengths
            var edgeLengthQueue = new SimplePriorityQueue<int, double>(new ForwardSort());
            var lengthsArray = new double[numPoints];
            // note that the lengthsArray and the queue work together. This is also done in the SimplifyByAreaChange as well.
            // the queue points to the vertex index and the length is found from the array. Had the priority queue allowed us 
            // to access the "key" - the array could be removed

            // buiding queue and array corresponds to the To Vertex of the edge
            for (int i = 0, j = numPoints - 1; i < numPoints; j = i++)
            {
                var edgeVector = (polygon[i] - polygon[j]);
                var length = edgeVector.LengthSquared();
                lengthsArray[i] = length;
                edgeLengthQueue.Enqueue(i, length);
            }
            #endregion

            while (edgeLengthQueue.Any())
            {
                var index = edgeLengthQueue.Dequeue();  // take off the lowest edge
                var length = lengthsArray[index]; //retrive the length and...
                if (length > minAllowableLength) break;  //check that it is below the minAllowableLength
                int nextIndex = FindValidNeighborIndex(index, true, polygon, numPoints); //given that neighbros may have been removed and the polygon wraps around, call special function
                int prevIndex = FindValidNeighborIndex(index, false, polygon, numPoints); // special function, can go forward (true) or backward (false) in list.
                int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygon, numPoints); //because the previous point will move, we need to update the one behind that
                if (nextIndex == prevprevIndex) // then reduced to three points. probably should stop, eh?
                    break;
                var newPoint = (polygon[index] + polygon[prevIndex]) / 2; //find midpoint on this short line and use as new location for previous point
                polygon[prevIndex] = newPoint;
                polygon[index] = Vector2.Null; // remove this point
                edgeLengthQueue.UpdatePriority(prevIndex, (newPoint - polygon[prevprevIndex]).LengthSquared());  // update priorities of previous-previous line
                edgeLengthQueue.UpdatePriority(nextIndex, (polygon[nextIndex] - newPoint).LengthSquared()); // and the next line
            }
            return polygon.Where(v => !v.IsNull()); //return only the vertices that have not been set to null.
            // note that this one time reduction of the polygon at the end is much more efficient than removing and updating the polygon between each vertex removal
        }


        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>Polygon.</returns>
        public static Polygon SimplifyMinLength(this Polygon polygon, int targetNumberOfPoints)
        {
            var simplifiedPaths = polygon.AllPolygons.Select(poly => poly.Path).SimplifyMinLength(targetNumberOfPoints);
            return CreateShallowPolygonTreesOrderedListsAndVertices(simplifiedPaths).First();
        }

        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static IEnumerable<Polygon> SimplifyMinLength(this IEnumerable<Polygon> polygons, int targetNumberOfPoints)
        {
            var simplifiedPaths = polygons.SelectMany(poly => poly.AllPolygons.Select(p => p.Path)).SimplifyMinLength(targetNumberOfPoints);
            return CreateShallowPolygonTreesOrderedListsAndVertices(simplifiedPaths);
        }

        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        public static IEnumerable<Vector2> SimplifyMinLength(this IEnumerable<Vector2> path, int targetNumberOfPoints)
        { return SimplifyMinLength(new[] { path }, targetNumberOfPoints).First(); }

        /// <summary>
        /// Simplifies the specified polygons so that no edge is less than the minimum allowable length.
        /// In this method vertices are not simply deleted but a new one is created at midPoint of deleted edge.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        /// <exception cref="ArgumentOutOfRangeException">targetNumberOfPoints - The number of points to remove in PolygonOperations.Simplify"
        ///                   + " is more than the total number of points in the polygon(s).</exception>
        public static IEnumerable<List<Vector2>> SimplifyMinLength(this IEnumerable<IEnumerable<Vector2>> paths, int targetNumberOfPoints)
        {
            // first remove collinear points and set up lists
            var polygons = paths.Select(p => p.RemoveCollinearEdges()).ToList();
            var numPoints = polygons.Select(p => p.Count).ToList();
            var numToRemove = numPoints.Sum() - targetNumberOfPoints;
            if (numToRemove <= 0)
                foreach (var item in polygons)
                    yield return item;

            #region build initial list of edge lengths
            var edgeLengthQueue = new SimplePriorityQueue<int, double>(new ForwardSort());
            var lengthsArray = new double[numPoints.Sum()];
            // note that the lengthsArray and the queue work together. This is also done in the SimplifyByAreaChange as well.
            // the queue points to the vertex index and the length is found from the array. Had the priority queue allowed us 
            // to access the "key" - the array could be removed

            // buiding queue and array corresponds to the To Vertex of the edge
            var index = 0;
            for (int k = 0; k < polygons.Count; k++)
            {
                for (int i = 0, j = numPoints[k] - 1; i < numPoints[k]; j = i++)
                {
                    var edgeVector = (polygons[k][i] - polygons[k][j]);
                    var length = edgeVector.LengthSquared();
                    lengthsArray[index] = length;
                    edgeLengthQueue.Enqueue(index, length);
                    index++;
                }
            }
            #endregion
            while (numToRemove-- > 0)
            {
                index = edgeLengthQueue.Dequeue();  // take off the lowest edge
                var cornerIndex = index;
                var polygonIndex = 0;
                // the index is from stringing together all the original polygons into one long array
                while (cornerIndex >= polygons[polygonIndex].Count) cornerIndex -= polygons[polygonIndex++].Count;
                int nextIndex = FindValidNeighborIndex(cornerIndex, true, polygons[polygonIndex], numPoints[polygonIndex]); //given that neighbros may have been removed and the polygon wraps around, call special function
                int prevIndex = FindValidNeighborIndex(cornerIndex, false, polygons[polygonIndex], numPoints[polygonIndex]); // special function, can go forward (true) or backward (false) in list.
                int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygons[polygonIndex], numPoints[polygonIndex]); //because the previous point will move, we need to update the one behind that
                if (nextIndex == prevprevIndex) // then reduced to three points. probably should stop, eh?
                    continue;
                var newPoint = (polygons[polygonIndex][cornerIndex] + polygons[polygonIndex][prevIndex]) / 2; //find midpoint on this short line and use as new location for previous point
                polygons[polygonIndex][prevIndex] = newPoint;
                polygons[polygonIndex][cornerIndex] = Vector2.Null; // remove this point
                edgeLengthQueue.UpdatePriority(prevIndex, (newPoint - polygons[polygonIndex][prevprevIndex]).LengthSquared());  // update priorities of previous-previous line
                edgeLengthQueue.UpdatePriority(nextIndex, (polygons[polygonIndex][nextIndex] - newPoint).LengthSquared()); // and the next line
            }

            foreach (var polygon in polygons)
            {
                var resultPolygon = new List<Vector2>();
                foreach (var corner in polygon)
                    if (!corner.IsNull()) resultPolygon.Add(corner);
                if (resultPolygon.Count > 2)
                    yield return resultPolygon;
            }
        }




        #endregion

        #region SimplifyByAreaChange
        /// <summary>
        /// Simplifies the specified polygons no more than the allowable change in area fraction.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="allowableChangeInAreaFraction">The allowable change in area fraction.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static IEnumerable<Polygon> SimplifyByAreaChange(this IEnumerable<Polygon> polygons, double allowableChangeInAreaFraction)
        {
            return polygons.Select(poly => poly.SimplifyByAreaChange(allowableChangeInAreaFraction));
        }

        /// <summary>
        /// Simplifies the specified polygon no more than the allowable change in area fraction.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="allowableChangeInAreaFraction">The allowable change in area fraction.</param>
        /// <returns>Polygon.</returns>
        public static Polygon SimplifyByAreaChange(this Polygon polygon, double allowableChangeInAreaFraction)
        {
            var simplifiedPositivePolygon = new Polygon(polygon.Path.SimplifyByAreaChange(allowableChangeInAreaFraction));
            foreach (var polygonHole in polygon.InnerPolygons)
                simplifiedPositivePolygon.AddInnerPolygon(new Polygon(polygonHole.Path.SimplifyByAreaChange(allowableChangeInAreaFraction)));
            return simplifiedPositivePolygon;
        }

        /// <summary>
        /// Simplifies the specified polygons no more than the allowable change in area fraction.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="allowableChangeInAreaFraction">The allowable change in area fraction.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        public static IEnumerable<IEnumerable<Vector2>> SimplifyByAreaChange(this IEnumerable<IEnumerable<Vector2>> paths, double allowableChangeInAreaFraction)
        {
            return paths.Select(p => SimplifyByAreaChange(p, allowableChangeInAreaFraction));
        }

        /// <summary>
        /// Simplifies the specified polygons no more than the allowable change in area fraction.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<Vector2> SimplifyByAreaChange(this IEnumerable<Vector2> path, double allowableChangeInAreaFraction)
        {
            var polygon = path.RemoveCollinearEdges();
            var numPoints = polygon.Count;
            var origArea = Math.Abs(polygon.Area());
            if (origArea.IsNegligible()) return polygon;

            #region build initial list of cross products

            // queue is sorted on the cross-product at the polygon corner (requiring knowledge of the previous and next points)
            // Here we are using the SimplePriorityQueue from BlueRaja (https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp)
            var convexCornerQueue = new SimplePriorityQueue<int, double>(new ForwardSort());
            var concaveCornerQueue = new SimplePriorityQueue<int, double>(new ReverseSort());

            // cross-products which are kept in the same order as the corners they represent. This is solely used with the above
            // dictionary - to essentially do the reverse lookup. given a corner-index, crossProductsArray will instanly tell us the
            // cross-product. The cross-product is used as the key in the dictionary - to find corner-indices.
            var crossProductsArray = new double[numPoints];

            // make the cross-products. this is a for-loop that is preceded with the first element (requiring the last element, "^1" in
            // C# 8 terms) and succeeded by one for the last corner
            AddCrossProductToOneOfTheLists(polygon[^1], polygon[0], polygon[1], convexCornerQueue, concaveCornerQueue,
                crossProductsArray, 0);
            for (int i = 1; i < numPoints - 1; i++)
                AddCrossProductToOneOfTheLists(polygon[i - 1], polygon[i], polygon[i + 1], convexCornerQueue, concaveCornerQueue,
                crossProductsArray, i);
            AddCrossProductToOneOfTheLists(polygon[^2], polygon[^1], polygon[0], convexCornerQueue, concaveCornerQueue,
                crossProductsArray, numPoints - 1);

            #endregion build initial list of cross products

            // after much thought, the idea to split up into positive and negative sorted lists is so that we don't over remove vertices
            // by bouncing back and forth between convex and concave while staying with the target deltaArea. So, we do as many convex corners
            // before reaching a reduction of deltaArea - followed by a reduction of concave edges so that no more than deltaArea is re-added
            for (int sign = 1; sign >= -1; sign -= 2)
            {
                var deltaArea = 2 * allowableChangeInAreaFraction * origArea; //multiplied by 2 in order to reduce all the divide by 2
                                                                              // that happens when we change cross-product to area of a triangle
                var relevantSortedList = (sign == 1) ? convexCornerQueue : concaveCornerQueue;
                // first we remove any convex corners that would reduce the area
                while (relevantSortedList.Count > 0)
                {
                    var index = relevantSortedList.Dequeue();
                    var smallestArea = crossProductsArray[index];
                    if (deltaArea < sign * smallestArea)
                    { //this was one tricky little bug! in order to keep this fast, we first dequeue before examining
                      // the result. if the resulting index produces more area than we need we switch to the
                      // concave queue. That dequeuing and updating will want this last index on the queues
                      // if it is a neighbor to a new one being removing. Confusing, eh? So, we need to put it
                      // back in. Looks kludge-y but this only happens once, and it's better to do this once
                      // then add more logic to the above statements that would slow it down.
                        relevantSortedList.Enqueue(index, smallestArea);
                        break;
                    }
                    deltaArea -= sign * smallestArea;
                    //  set the corner to null. we'll remove null corners at the end. for now, just set to null.
                    // this is for speed and keep the indices correct in the various collections
                    polygon[index] = Vector2.Null;
                    // find the four neighbors - two on each side. the closest two (prevIndex and nextIndex) need to be updated
                    // which requires each other (now that the corner in question has been removed) and their neighbors on the other side
                    // (nextnextIndex and prevprevIndex)
                    int nextIndex = FindValidNeighborIndex(index, true, polygon, numPoints);
                    int nextnextIndex = FindValidNeighborIndex(nextIndex, true, polygon, numPoints);
                    int prevIndex = FindValidNeighborIndex(index, false, polygon, numPoints);
                    int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygon, numPoints);
                    // if the polygon has been reduced to 2 points, then we're going to delete it
                    if (nextnextIndex == prevIndex || nextIndex == prevprevIndex) // then reduced to two points.
                        continue;

                    // now, add these new crossproducts both to the dictionary and to the sortedLists. Note, that nothing is
                    // removed from the sorted lists here. it is more efficient to just remove them if they bubble to the top of the list,
                    // which is done in PopNextSmallestArea
                    UpdateCrossProductInQueues(polygon[prevIndex], polygon[nextIndex], polygon[nextnextIndex], convexCornerQueue, concaveCornerQueue,
                        crossProductsArray, nextIndex);
                    UpdateCrossProductInQueues(polygon[prevprevIndex], polygon[prevIndex], polygon[nextIndex], convexCornerQueue, concaveCornerQueue,
                            crossProductsArray, prevIndex);
                }
            }
            return polygon.Where(v => !v.IsNull());
        }



        /// <summary>
        /// Simplifies the specified polygon to the target number of points using the minimal area change approach.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>Polygon.</returns>
        public static Polygon SimplifyByAreaChange(this Polygon polygon, int targetNumberOfPoints)
        {
            var simplifiedPaths = polygon.AllPolygons.Select(poly => poly.Path).SimplifyByAreaChange(targetNumberOfPoints);
            return CreateShallowPolygonTreesOrderedListsAndVertices(simplifiedPaths).First();
        }

        /// <summary>
        /// Simplifies the specified polygon to the target number of points using the minimal area change approach.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static IEnumerable<Polygon> SimplifyByAreaChange(this IEnumerable<Polygon> polygons, int targetNumberOfPoints)
        {
            var simplifiedPaths = polygons.SelectMany(poly => poly.AllPolygons.Select(p => p.Path)).SimplifyByAreaChange(targetNumberOfPoints);
            return CreateShallowPolygonTreesOrderedListsAndVertices(simplifiedPaths);
        }

        /// <summary>
        /// Simplifies the specified polygon to the target number of points using the minimal area change approach.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        public static IEnumerable<Vector2> SimplifyByAreaChange(this IEnumerable<Vector2> path, int targetNumberOfPoints)
        { return SimplifyByAreaChange(new[] { path }, targetNumberOfPoints).First(); }

        /// <summary>
        /// Simplifies the specified polygon to the target number of points using the minimal area change approach.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>List&lt;List&lt;Vector2&gt;&gt;.</returns>
        /// <exception cref="ArgumentOutOfRangeException">targetNumberOfPoints - The number of points to remove in PolygonOperations.Simplify"
        ///                   + " is more than the total number of points in the polygon(s).</exception>
        public static IEnumerable<List<Vector2>> SimplifyByAreaChange(this IEnumerable<IEnumerable<Vector2>> paths, int targetNumberOfPoints)
        {
            // first remove collinear points and set up lists
            var polygons = paths.Select(p => p.RemoveCollinearEdges()).ToList();
            var numPoints = polygons.Select(p => p.Count).ToList();
            var numToRemove = numPoints.Sum() - targetNumberOfPoints;
            if (numToRemove <= 0)
                foreach (var item in polygons)
                    yield return item;

            #region build initial list of cross products

            var cornerQueue = new SimplePriorityQueue<int, double>(new AbsoluteValueSort());
            var crossProductsArray = new double[numPoints.Sum()];
            var index = 0;
            for (int j = 0; j < polygons.Count; j++)
            {
                AddCrossProductToQueue(polygons[j][^1], polygons[j][0], polygons[j][1], cornerQueue, crossProductsArray, index++);
                for (int i = 1; i < numPoints[j] - 1; i++)
                    AddCrossProductToQueue(polygons[j][i - 1], polygons[j][i], polygons[j][i + 1], cornerQueue, crossProductsArray, index++);
                AddCrossProductToQueue(polygons[j][^2], polygons[j][^1], polygons[j][0], cornerQueue, crossProductsArray, index++);
            }

            #endregion build initial list of cross products

            while (numToRemove-- > 0)
            {
                index = cornerQueue.Dequeue();
                var cornerIndex = index;
                var polygonIndex = 0;
                // the index is from stringing together all the original polygons into one long array
                while (cornerIndex >= polygons[polygonIndex].Count) cornerIndex -= polygons[polygonIndex++].Count;
                polygons[polygonIndex][cornerIndex] = Vector2.Null;

                // find the four neighbors - two on each side. the closest two (prevIndex and nextIndex) need to be updated
                // which requires each other (now that the corner in question has been removed) and their neighbors on the other side
                // (nextnextIndex and prevprevIndex)
                int nextIndex = FindValidNeighborIndex(cornerIndex, true, polygons[polygonIndex], numPoints[polygonIndex]);
                int nextnextIndex = FindValidNeighborIndex(nextIndex, true, polygons[polygonIndex], numPoints[polygonIndex]);
                int prevIndex = FindValidNeighborIndex(cornerIndex, false, polygons[polygonIndex], numPoints[polygonIndex]);
                int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygons[polygonIndex], numPoints[polygonIndex]);
                // if the polygon has been reduced to 2 points, then we're going to delete it
                if (nextnextIndex == prevIndex || nextIndex == prevprevIndex) // then reduced to two points.
                {
                    polygons[polygonIndex][nextIndex] = Vector2.Null;
                    polygons[polygonIndex][nextnextIndex] = Vector2.Null;
                    numToRemove -= 2;
                }
                var polygonStartIndex = index - cornerIndex;
                // like the AddCrossProductToQueue function used above, we need a global index from stringing together all the polygons.
                // So, polygonStartIndex is used to find the start of this particular polygon's index and then add prevIndex and nextIndex to it.
                UpdateCrossProductInQueue(polygons[polygonIndex][prevprevIndex], polygons[polygonIndex][prevIndex], polygons[polygonIndex][nextIndex],
                    cornerQueue, crossProductsArray, polygonStartIndex + prevIndex);
                UpdateCrossProductInQueue(polygons[polygonIndex][prevIndex], polygons[polygonIndex][nextIndex], polygons[polygonIndex][nextnextIndex],
                    cornerQueue, crossProductsArray, polygonStartIndex + nextIndex);
            }

            foreach (var polygon in polygons)
            {
                var resultPolygon = new List<Vector2>();
                foreach (var corner in polygon)
                    if (!corner.IsNull()) resultPolygon.Add(corner);
                if (resultPolygon.Count > 2)
                    yield return resultPolygon;
            }
        }



        #endregion


        public static Polygon SimplifyFuzzy(this Polygon polygon,
            double lengthTolerance = Constants.LineLengthMinimum,
            double slopeTolerance = Constants.LineSlopeTolerance)
        {
            var simplifiedPositivePolygon = new Polygon(polygon.Path.SimplifyFuzzy(lengthTolerance, slopeTolerance));
            foreach (var polygonHole in polygon.InnerPolygons)
                simplifiedPositivePolygon.AddInnerPolygon(new Polygon(polygonHole.Path.SimplifyFuzzy(lengthTolerance, slopeTolerance)));
            return simplifiedPositivePolygon;
        }

        /// <summary>
        /// Simplifies the specified polygons by reducing the number of points in the polygon
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Vector2> SimplifyFuzzy(this IEnumerable<Vector2> path,
        double lengthTolerance = Constants.LineLengthMinimum,
        double slopeTolerance = Constants.LineSlopeTolerance)
        {
            if (lengthTolerance.IsNegligible()) lengthTolerance = Constants.LineLengthMinimum;
            var squareLengthTolerance = lengthTolerance * lengthTolerance;
            var simplePath = new List<Vector2>(path);
            var n = simplePath.Count;
            if (n < 4) return simplePath;

            //Remove negligible length lines and combine collinear lines.
            var i = 0;
            var j = 1;
            var k = 2;
            var iX = simplePath[i].X;
            var iY = simplePath[i].Y;
            var jX = simplePath[j].X;
            var jY = simplePath[j].Y;
            var kX = simplePath[k].X;
            var kY = simplePath[k].Y;
            while (i < n)
            {
                //We only check line I-J in the first iteration, since later we
                //check line J-K instead.
                if (i == 0 && NegligibleLine(iX, iY, jX, jY, squareLengthTolerance))
                {
                    simplePath.RemoveAt(j);
                    n--;
                    j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    k = (j + 1) % n; //Next position in path. Goes to 0 when j = n-1; 
                                     //Current stays the same.
                                     //j moves to k, k moves forward but has the same index.
                    jX = kX;
                    jY = kY;
                    var kPoint = simplePath[k];
                    kX = kPoint.X;
                    kY = kPoint.Y;
                }
                else if (NegligibleLine(jX, jY, kX, kY, squareLengthTolerance))
                {
                    continue;
                    n--;
                    j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    k = (j + 1) % n; //Next position in path. Goes to 0 when j = n-1; 
                                     //Current and Next stay the same.
                                     //k moves forward but has the same index.
                    var kPoint = simplePath[k];
                    kX = kPoint.X;
                    kY = kPoint.Y;
                }
                //Use an even looser tolerance to determine if slopes are equal.
                else if (LineSlopesEqual(iX, iY, jX, jY, kX, kY, slopeTolerance))
                {
                    simplePath.RemoveAt(j);
                    n--;
                    j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    k = (j + 1) % n; //Next position in path. Goes to 0 when j = n-1; 

                    //Current stays the same.
                    //j moves to k, k moves forward but has the same index.
                    jX = kX;
                    jY = kY;
                    var kPoint = simplePath[k];
                    kX = kPoint.X;
                    kY = kPoint.Y;
                }
                else
                {
                    //Everything moves forward
                    i++;
                    j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    k = (j + 1) % n; //Next position in path. Goes to 0 when j = n-1; 
                    iX = jX;
                    iY = jY;
                    jX = kX;
                    jY = kY;
                    var kPoint = simplePath[k];
                    kX = kPoint.X;
                    kY = kPoint.Y;
                }
            }

            var area1 = Area(path);
            var area2 = Area(simplePath);

            //If the simplification destroys a polygon, do not simplify it.
            if (area2.IsNegligible() ||
                !area1.IsPracticallySame(area2, Math.Abs(area1 * (1 - Constants.HighConfidence))))
            {
                return path.ToList();
            }

            return simplePath;
        }

        private static bool NegligibleLine(double p1X, double p1Y, double p2X, double p2Y, double squaredTolerance)
        {
            var dX = p1X - p2X;
            var dY = p1Y - p2Y;
            return (dX * dX + dY * dY).IsNegligible(squaredTolerance);
        }

        private static bool LineSlopesEqual(double p1X, double p1Y, double p2X, double p2Y, double p3X, double p3Y,
            double tolerance = Constants.LineSlopeTolerance)
        {
            var value = (p1Y - p2Y) * (p2X - p3X) - (p1X - p2X) * (p2Y - p3Y);
            return value.IsNegligible(tolerance);
        }

        private static int FindValidNeighborIndex(int index, bool forward, IList<Vector2> polygon, int numPoints)
        {
            int increment = forward ? 1 : -1;
            var hitLimit = false;
            do
            {
                index += increment;
                if (index < 0)
                {
                    index = numPoints - 1;
                    if (hitLimit)
                    {
                        index = -1;
                        break;
                    }
                    hitLimit = true;
                }
                else if (index == numPoints)
                {
                    index = 0;
                    if (hitLimit)
                    {
                        index = -1;
                        break;
                    }
                    hitLimit = true;
                }
            }
            while (polygon[index].IsNull());
            return index;
        }

        private static void AddCrossProductToOneOfTheLists(Vector2 fromPoint, Vector2 currentPoint, Vector2 nextPoint,
            SimplePriorityQueue<int, double> convexCornerQueue, SimplePriorityQueue<int, double> concaveCornerQueue,
            double[] crossProducts, int index)
        {
            var cross = (currentPoint - fromPoint).Cross(nextPoint - currentPoint);
            crossProducts[index] = cross;
            if (cross < 0) concaveCornerQueue.Enqueue(index, cross);
            else convexCornerQueue.Enqueue(index, cross);
        }

        private static void UpdateCrossProductInQueues(Vector2 fromPoint, Vector2 currentPoint, Vector2 nextPoint,
            SimplePriorityQueue<int, double> convexCornerQueue, SimplePriorityQueue<int, double> concaveCornerQueue,
            double[] crossProducts, int index)
        {
            var oldCross = crossProducts[index];
            var newCross = (currentPoint - fromPoint).Cross(nextPoint - currentPoint);
            crossProducts[index] = newCross;
            if (newCross < 0)
            {
                if (oldCross < 0)
                    concaveCornerQueue.UpdatePriority(index, newCross);
                else //then it used to be positive and needs to be removed from the convexCornerQueue
                {
                    convexCornerQueue.Remove(index);
                    concaveCornerQueue.Enqueue(index, newCross);
                }
            }
            // else newCross is positive and should be on the convexCornerQueue
            else if (oldCross >= 0)
                convexCornerQueue.UpdatePriority(index, newCross);
            else //then it used to be negative and needs to be removed from the concaveCornerQueue
            {
                concaveCornerQueue.Remove(index);
                convexCornerQueue.Enqueue(index, newCross);
            }
        }

        private static void AddCrossProductToQueue(Vector2 fromPoint, Vector2 currentPoint,
            Vector2 nextPoint, SimplePriorityQueue<int, double> cornerQueue,
            double[] crossProducts, int index)
        {
            var cross = Math.Abs((currentPoint - fromPoint).Cross(nextPoint - currentPoint));
            crossProducts[index] = cross;
            cornerQueue.Enqueue(index, cross);
        }

        private static void UpdateCrossProductInQueue(Vector2 fromPoint, Vector2 currentPoint, Vector2 nextPoint,
            SimplePriorityQueue<int, double> cornerQueue, double[] crossProducts, int index)
        {
            var newCross = (currentPoint - fromPoint).Cross(nextPoint - currentPoint);
            crossProducts[index] = newCross;
            cornerQueue.UpdatePriority(index, newCross);
        }

        #endregion Simplify



        #region Complexify

        /// <summary>
        /// Complexifies the specified polygons so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="maxAllowableLength">Maximum length of the allowable.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IEnumerable<Polygon> ComplexifyToNewPolygons(this IEnumerable<Polygon> polygons, double maxAllowableLength)
        {
            var copiedPolygons = polygons.Select(p => p.Copy(true, false));
            Complexify(copiedPolygons, maxAllowableLength);
            return copiedPolygons;
        }

        /// <summary>
        /// Complexifies the specified polygon so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="maxAllowableLength">Maximum length of the allowable.</param>
        /// <returns>Polygon.</returns>
        public static Polygon ComplexifyToNewPolygon(this Polygon polygon, double maxAllowableLength)
        {
            var copiedPolygon = polygon.Copy(true, false);
            Complexify(copiedPolygon, maxAllowableLength);
            return copiedPolygon;
        }

        /// <summary>
        /// Complexifies the specified polygons so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="maxAllowableLength">Maximum length of the allowable.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static void Complexify(this IEnumerable<Polygon> polygons, double maxAllowableLength)
        {
            foreach (var polygon in polygons)
                polygon.Complexify(maxAllowableLength);
        }

        /// <summary>
        /// Complexifies the specified polygon so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="maxAllowableLength">Maximum length of the allowable.</param>
        /// <returns>Polygon.</returns>
        public static void Complexify(this Polygon polygon, double maxAllowableLength)
        {
            var loopID = polygon.Index;
            for (int i = 0; i < polygon.Edges.Length; i++)
            {
                var thisLine = polygon.Edges[i];
                if (thisLine.Length > maxAllowableLength)
                {
                    var numNewPoints = (int)thisLine.Length / maxAllowableLength;
                    for (int j = 0; j < numNewPoints; j++)
                    {
                        var fraction = j / (double)numNewPoints;
                        var newCoordinates = fraction * thisLine.FromPoint.Coordinates + ((1 - fraction) * thisLine.ToPoint.Coordinates);
                        polygon.Vertices.Insert(i, new Vertex2D(newCoordinates, 0, loopID));
                    }
                }
            }
            polygon.Reset();
            foreach (var polygonHole in polygon.InnerPolygons)
                polygonHole.Complexify(maxAllowableLength);
        }

        /// <summary>
        /// Complexifies the specified polygons so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IEnumerable<Polygon> ComplexifyToNewPolygons(this IEnumerable<Polygon> polygons, int targetNumberOfPoints)
        {
            var copiedPolygons = polygons.Select(p => p.Copy(true, false));
            Complexify(copiedPolygons, targetNumberOfPoints);
            return copiedPolygons;
        }

        /// <summary>
        /// Complexifies the specified polygon so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>Polygon.</returns>
        public static Polygon ComplexifyToNewPolygon(this Polygon polygon, int targetNumberOfPoints)
        {
            var copiedPolygon = polygon.Copy(true, false);
            Complexify(copiedPolygon, targetNumberOfPoints);
            return copiedPolygon;
        }
        /// <summary>
        /// Complexifies the specified polygons so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>IEnumerable&lt;Polygon&gt;.</returns>
        public static void Complexify(this IEnumerable<Polygon> polygons, int targetNumberOfPoints)
        {
            //   throw new NotImplementedException();
        }

        /// <summary>
        /// Complexifies the specified polygon so that no edge is longer than the maxAllowableLength.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="targetNumberOfPoints">The target number of points.</param>
        /// <returns>Polygon.</returns>
        public static void Complexify(this Polygon polygon, int targetNumberOfPoints)
        {
            // throw new NotImplementedException();
        }
        #endregion
    }
}