// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        static Stopwatch sw = new Stopwatch();
        const string timeFiles = "times.csv";
        private static bool Compare(List<Polygon> tvglResult, List<Polygon> clipperResult, string operationString, TimeSpan clipTime, TimeSpan tvglTime)
        {
            lock (sw)
            {
                if (!File.Exists(timeFiles))
                {
                    using (var newFs = File.Create(timeFiles)) { }

                }
                using (var fs = File.Open(timeFiles, FileMode.Append))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(clipTime.Ticks.ToString() + "," + tvglTime.Ticks.ToString() + "\n");
                    fs.Write(info, 0, info.Length);
                }

                var tolerance = 0.2;
                var clipperMinX = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Min(p => p.MinX);
                var clipperMinY = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Min(p => p.MinY);
                var clipperMaxX = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Max(p => p.MaxX);
                var clipperMaxY = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Max(p => p.MaxY);
                var tvglMinX = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Min(p => p.MinX);
                var tvglMinY = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Min(p => p.MinY);
                var tvglMaxX = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Max(p => p.MaxX);
                var tvglMaxY = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Max(p => p.MaxY);
                var extremaTolerance = tolerance * (new[] { clipperMaxX - clipperMinX, clipperMaxY - clipperMinY, tvglMaxX - tvglMinX, tvglMaxY - tvglMinY }).Min();
                var numPolygonsTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(poly => poly.AllPolygons.Count());
                var numPolygonsClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(poly => poly.AllPolygons.Count());
                var vertsTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
                var vertsClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
                var areaTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(p => p.Area);
                var areaClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(p => p.Area);
                var perimeterTVGL = tvglResult == null || !tvglResult.Any() ? 0 : tvglResult.Sum(p => p.Perimeter);
                var perimeterClipper = clipperResult == null || !clipperResult.Any() ? 0 : clipperResult.Sum(p => p.Perimeter);
                if (//numPolygonsTVGL == numPolygonsClipper
                    //&& //vertsTVGL == vertsClipper &&
                    !Math.Abs(areaTVGL).IsGreaterThanNonNegligible(areaClipper, Math.Max(1e-6, (areaTVGL + areaClipper) * tolerance))
                     //areaTVGL.IsPracticallySame(areaClipper, Math.Max(1e-6,(areaTVGL + areaClipper) * tolerance) )
                     //&&
                     //perimeterTVGL.IsPracticallySame(perimeterClipper, (perimeterTVGL + perimeterClipper) * tolerance) &&
                     //tvglMinX.IsPracticallySame(clipperMinX, extremaTolerance) &&
                     //tvglMinY.IsPracticallySame(clipperMinY, extremaTolerance) &&
                     //tvglMaxX.IsPracticallySame(clipperMaxX, extremaTolerance) &&
                     //tvglMaxY.IsPracticallySame(clipperMaxY, extremaTolerance)
                    )
                {
                    Debug.WriteLine("***** " + operationString + " matches");
                    Debug.WriteLine("clipper time = {0}; tvgl time = {1}", clipTime, tvglTime);
                    return false;
                }
                else
                {
                    if (numPolygonsClipper == 0) return false;
                    Debug.WriteLine(operationString + " does not match");
                    Debug.WriteLine("clipper time = {0}; tvgl time = {1}", clipTime, tvglTime);
                    if (numPolygonsTVGL == numPolygonsClipper)
                        Debug.WriteLine("+++ both have {0} polygon(s)", numPolygonsTVGL, numPolygonsClipper);
                    else Debug.WriteLine("    --- polygons: TVGL={0}  : Clipper={1} ", numPolygonsTVGL, numPolygonsClipper);
                    if (vertsTVGL == vertsClipper)
                        Debug.WriteLine("+++ both have {0} vertices(s)", vertsTVGL);
                    else Debug.WriteLine("    --- verts: TVGL= {0}  : Clipper={1} ", vertsTVGL, vertsClipper);

                    if (areaTVGL.IsPracticallySame(areaClipper, tolerance))
                        Debug.WriteLine("+++ both have area of {0}", areaTVGL);
                    else
                        Debug.WriteLine("    --- area: TVGL= {0}  : Clipper={1} ", areaTVGL, areaClipper);
                    if (perimeterTVGL.IsPracticallySame(perimeterClipper, tolerance))
                        Debug.WriteLine("+++ both have perimeter of {0}", perimeterTVGL);
                    else
                        Debug.WriteLine("    --- perimeter: TVGL={0}  : Clipper={1} ", perimeterTVGL, perimeterClipper);
                    if (perimeterClipper - perimeterTVGL > 0 && Math.Round(perimeterClipper - perimeterTVGL) % 2 == 0)
                        Debug.WriteLine("<><><><><><><> clipper is connecting separate poly's :", (int)(perimeterClipper - perimeterTVGL) / 2);
                    return true;
                }
            }
        }



        // while the main executing methods are provided in this file (all of which can be invoked as Extensions), the code that perform the new polygon creation
        // is provided in the following four non-Static classes. These are non-static because they all inherit from the BooleanBase class. Each of these only needs
        // to be instantiated once as no data is stored in the class objects. So, this is a sort of singleton model but it's too bad we can have static classes inherit from
        // other static classes.
        private static PolygonUnion polygonUnion;
        private static PolygonIntersection polygonIntersection;
        private static PolygonRemoveIntersections polygonRemoveIntersections;

        #region Union Public Methods

        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            var relationship = GetPolygonInteraction(polygonA, polygonB);

            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctUnion, new[] { polygonA },
                  new[] { polygonB });
            sw.Stop();
#if PRESENT
            Presenter.ShowAndHang(pClipper);
#endif
            var clipTime = sw.Elapsed;
            sw.Restart();
            relationship = GetPolygonInteraction(polygonA, polygonB);
            var pTVGL = Union(polygonA, polygonB, relationship, outputAsCollectionType);
            sw.Stop();
#if PRESENT
            Presenter.ShowAndHang(pTVGL);
#endif
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "Union", clipTime, tvglTime))
            {
#if !PRESENT
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                TVGL.IOFunctions.IO.Save(polygonA, fileNameStart + ".A.json");
                TVGL.IOFunctions.IO.Save(polygonB, fileNameStart + ".B.json");
#endif
            }
            return pClipper;
        }


        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonInteraction">The polygon relationship.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonA</exception>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonB</exception>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord polygonInteraction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (!polygonA.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonA));
            if (!polygonB.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonB));
            if (!polygonInteraction.CoincidentEdges && (polygonInteraction.Relationship == PolygonRelationship.Separated ||
                polygonInteraction.Relationship == PolygonRelationship.AIsInsideHoleOfB ||
                polygonInteraction.Relationship == PolygonRelationship.BIsInsideHoleOfA))
                return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
            if (polygonInteraction.Relationship == PolygonRelationship.BInsideA ||
               polygonInteraction.Relationship == PolygonRelationship.Equal)
                return new List<Polygon> { polygonA.Copy(true, false) };
            if (polygonInteraction.Relationship == PolygonRelationship.AInsideB)
                return new List<Polygon> { polygonB.Copy(true, false) };
            polygonUnion ??= new PolygonUnion();
            return polygonUnion.Run(polygonA, polygonB, polygonInteraction, outputAsCollectionType, tolerance);
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ANY of the provided polygons. Notice this is called UnionPolygons here to distinguish
        /// it from the LINQ function Union, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> UnionPolygons(this IEnumerable<Polygon> polygons, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            var polygonList = polygons.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctUnion, polygonList);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(polygonList[i], polygonList[j]);
                    if (interaction.Relationship == PolygonRelationship.BInsideA
                        || interaction.Relationship == PolygonRelationship.Equal)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else if (interaction.Relationship == PolygonRelationship.AInsideB)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (interaction.CoincidentEdges || interaction.Relationship == PolygonRelationship.Intersection)
                    {
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(polygonList[i], polygonList[j], interaction, outputAsCollectionType);
                        //Debug.WriteLine("i = {0}, j = {1}", i, j);
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(newPolygons);
                        polygonList.RemoveAt(i);
                        polygonList.RemoveAt(j);
                        polygonList.AddRange(newPolygons);
                        i = polygonList.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(polygonList, pClipper, "UnionLists", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(polygons);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(polygonList);
#else
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygons)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
#endif
            }
            return pClipper;
            //return polygonList;
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of the two collections of polygons. Notice this is called UnionPolygons 
        /// here to distinguish it from the LINQ function Union, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> UnionPolygons(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles,
            double tolerance = double.NaN)
        {
            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);
            var unionedPolygons = polygonsA.ToList();
            var polygonBList = polygonsB.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctUnion, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;

            sw.Restart();
            List<Polygon> pTVGL = null;
            for (int i = unionedPolygons.Count - 1; i >= 0; i--)
            {
                for (int j = polygonBList.Count - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(unionedPolygons[i], polygonBList[j]);
                    if (interaction.Relationship == PolygonRelationship.BInsideA
                        || interaction.Relationship == PolygonRelationship.Equal)
                    {  // remove polygon B
                        polygonBList.RemoveAt(j);
                    }
                    else if (interaction.Relationship == PolygonRelationship.AInsideB)
                    {                            // remove polygon A
                        unionedPolygons[i] = polygonBList[j];
                        polygonBList.RemoveAt(j);
                        break; // to stop the inner loop
                    }
                    else if (interaction.CoincidentEdges || interaction.Relationship == PolygonRelationship.Intersection)
                    {
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(unionedPolygons[i], polygonBList[j], interaction, outputAsCollectionType, tolerance);
                        //Debug.WriteLine("i = {0}, j = {1}", i, j);
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(newPolygons);
                        unionedPolygons.RemoveAt(i);
                        polygonBList.RemoveAt(j);
                        unionedPolygons.AddRange(newPolygons);
                        i = unionedPolygons.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            pTVGL = UnionPolygons(unionedPolygons.Where(p => p.IsPositive), outputAsCollectionType);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "UnionTwoLists", clipTime, tvglTime))
            {
#if !PRESENT
                var fileNameStart = "unionFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "A.json");
                i = 0;
                foreach (var poly in polygonsB)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "B.json");
#endif
            }
            return pClipper;
        }

        #endregion Union Public Methods

        #region Intersect Public Methods

        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctIntersection, new[] { polygonA },
                    new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            var pTVGL = Intersect(polygonA, polygonB, relationship, outputAsCollectionType, tolerance);


            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "Intersect", clipTime, tvglTime))
            {
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                TVGL.IOFunctions.IO.Save(polygonA, fileNameStart + "." + "A.json");
                TVGL.IOFunctions.IO.Save(polygonB, fileNameStart + "." + "B.json");
            }
            return pClipper;
        }

        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B. By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interaction">The interaction.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (interaction.IntersectionWillBeEmpty())
                return new List<Polygon>();
            else
            {
                polygonIntersection ??= new PolygonIntersection();
                return polygonIntersection.Run(polygonA, polygonB, interaction, outputAsCollectionType, tolerance);
            }
        }

        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the two collections of polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygons(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);
            var polygonAList = new List<Polygon>(polygonsA);
            var polygonBList = polygonsB.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctIntersection, polygonsA, polygonsB);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();

            foreach (var polyB in polygonBList)
            {
                for (int i = polygonAList.Count - 1; i >= 0; i--)
                {
                    var newPolygons = polygonAList[i].Intersect(polyB, outputAsCollectionType);
                    polygonAList.RemoveAt(i);
                    foreach (var newPoly in newPolygons)
                        polygonAList.Insert(i, newPoly);
                }
            }
            //return polygonAList;
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(polygonAList, pClipper, "IntersectTwoList", clipTime, tvglTime))
            {
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "A.json");
                i = 0;
                foreach (var poly in polygonsA)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "B.json");
            }
            return pClipper;
            //return polygonList;
        }


        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of ALL of the provided polygons. Notice this is called IntersectPolygons here 
        /// to distinguish it from the LINQ function Intersect, which is also a valid extension for any IEnumerable collection.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> IntersectPolygons(this IEnumerable<Polygon> polygons, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            var polygonList = polygons.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctIntersection, polygons);
            sw.Stop();
            //if (!pClipper.Any()) return pClipper;
#if PRESENT
            Presenter.ShowAndHang(pClipper);
#endif
            var clipTime = sw.Elapsed;
            sw.Restart();
            for (int i = polygonList.Count - 2; i > 0; i--)
            {
                var clippingPoly = polygonList[i];
                polygonList.RemoveAt(i);
                for (int j = polygonList.Count - 1; j >= i; j--)
                {
                    var interaction = GetPolygonInteraction(clippingPoly, polygonList[j]);
                    if (interaction.IntersectionWillBeEmpty())
                        polygonList.RemoveAt(j);
                    else
                    {
                        var newPolygons = Intersect(clippingPoly, polygonList[j], interaction, outputAsCollectionType);
                        foreach (var newPolygon in newPolygons)
                            polygonList.Insert(j, newPolygon);
                    }
                }
            }
            //return polygonList;
            sw.Stop();
#if PRESENT
            Presenter.ShowAndHang(polygonList);
#else
            var tvglTime = sw.Elapsed;
            if (Compare(polygonList, pClipper, "IntersectList", clipTime, tvglTime))
            {
                var fileNameStart = "intersectFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in polygons)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
            }
#endif
            return pClipper;
            //return polygonList;
        }

        #endregion Intersect Public Methods

        #region Subtract Public Methods

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A).
        /// </summary>
        /// <param name="minuend">The polygon a.</param>
        /// <param name="subtrahend">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this Polygon minuend, Polygon subtrahend,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctDifference, new[] { minuend },
                new[] { subtrahend });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var polygonBInverted = subtrahend.Copy(true, true);
            var relationship = GetPolygonInteraction(minuend, polygonBInverted);
            var pTVGL = Intersect(minuend, polygonBInverted, relationship, outputAsCollectionType, tolerance);

            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "Subtract", clipTime, tvglTime))
            {
#if PRESENT

                Presenter.ShowAndHang(new[] { minuend, subtrahend });
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(pTVGL);
#else
                var fileNameStart = "subtractFail" + DateTime.Now.ToOADate().ToString();
                TVGL.IOFunctions.IO.Save(minuend, fileNameStart + "." + "min.json");
                TVGL.IOFunctions.IO.Save(subtrahend, fileNameStart + "." + "sub.json");
#endif
            }
            return pClipper;

        }

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A). By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="minuend">The polygon a.</param>
        /// <param name="subtrahend">The polygon b.</param>
        /// <param name="interaction">The polygon relationship.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">The minuend is already a negative polygon (i.e. hole). Consider another operation"
        /// +" to accomplish this function, like Intersect. - polygonA</exception>
        public static List<Polygon> Subtract(this Polygon minuend, Polygon subtrahend, PolygonInteractionRecord interaction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            interaction = interaction.InvertPolygonInRecord(subtrahend, out var invertedPolygonB);
            return Intersect(minuend, invertedPolygonB, interaction, outputAsCollectionType, tolerance);
        }


        /// <summary>
        /// Returns the list of polygons that are the sub-shapes of the minuends and that are not part of the subtrahends. 
        /// Notice also that any overlap between the polygons in A or the polygons in B are ignored. Finally, all inputs must be positive.
        /// 
        /// </summary>
        /// <param name="minuends">The polygons a.</param>
        /// <param name="subtrahends">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this IEnumerable<Polygon> minuends, IEnumerable<Polygon> subtrahends,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            var minuendsList = minuends.ToList();
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctDifference, minuends, subtrahends);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();

            foreach (var polyB in subtrahends)
            {
                for (int i = minuendsList.Count - 1; i >= 0; i--)
                {
                    var newPolygons = minuendsList[i].Subtract(polyB, outputAsCollectionType, tolerance);
                    minuendsList.RemoveAt(i);
                    foreach (var newPoly in newPolygons)
                        minuendsList.Insert(i, newPoly);
                }
            }
            //return minuendsList;
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(minuendsList, pClipper, "SubtractLists", clipTime, tvglTime))
            {
                var fileNameStart = "subtractFail" + DateTime.Now.ToOADate().ToString();
                int i = 0;
                foreach (var poly in minuends)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "min.json");
                i = 0;
                foreach (var poly in subtrahends)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + "sub.json");
            }
            return pClipper;
            //return polygonList;
        }


        #endregion Subtract Public Methods

        #region Exclusive-OR Public Methods

        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            sw.Restart();
            var pClipper = BooleanViaClipper(ClipperLib.PolyFillType.pftPositive, ClipperLib.ClipType.ctXor, new[] { polygonA }, new[] { polygonB });
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            var pTVGL = ExclusiveOr(polygonA, polygonB, relationship, outputAsCollectionType);

            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "XOR", clipTime, tvglTime))
            {
                var fileNameStart = "xorFail" + DateTime.Now.ToOADate().ToString();
                TVGL.IOFunctions.IO.Save(polygonA, fileNameStart + "." + "A.json");
                TVGL.IOFunctions.IO.Save(polygonB, fileNameStart + "." + "B.json");
            }
            return pClipper;
        }

        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both. By providing the intersections between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interactionRecord">The interaction record.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interactionRecord,
           PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (interactionRecord.IntersectionWillBeEmpty())
                return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
            else if (interactionRecord.Relationship == PolygonRelationship.BInsideA &&
                !interactionRecord.CoincidentEdges && !interactionRecord.CoincidentVertices)
            {
                var polygonACopy1 = polygonA.Copy(true, false);
                polygonACopy1.AddInnerPolygon(polygonB.Copy(true, true));
                return new List<Polygon> { polygonACopy1 };
            }
            else if (interactionRecord.Relationship == PolygonRelationship.AInsideB &&
                !interactionRecord.CoincidentEdges && !interactionRecord.CoincidentVertices)
            {
                var polygonBCopy2 = polygonB.Copy(true, false);
                polygonBCopy2.AddInnerPolygon(polygonA.Copy(true, true));
                return new List<Polygon> { polygonBCopy2 };
            }
            else
            {
                var result = polygonA.Subtract(polygonB, interactionRecord, outputAsCollectionType, tolerance);
                result.AddRange(polygonB.Subtract(polygonA, interactionRecord, outputAsCollectionType, tolerance));
                return result;
            }
        }

        #endregion Exclusive-OR Public Methods

        #region RemoveSelfIntersections Public Method

        /// <summary>
        /// Removes the self intersections.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="knownWrongPoints">The known wrong points.</param>
        /// <param name="maxNumberOfPolygons">The maximum number of polygons.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon, ResultType resultType,
            List<bool> knownWrongPoints = null, int maxNumberOfPolygons = int.MaxValue)
        {
            var intersections = polygon.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
            if (intersections.Count == 0)
                return new List<Polygon> { polygon };
            polygonRemoveIntersections ??= new PolygonRemoveIntersections();
            return polygonRemoveIntersections.Run(polygon, intersections, resultType, knownWrongPoints, maxNumberOfPolygons);
        }

        #endregion RemoveSelfIntersections Public Method
    }
}