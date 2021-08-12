// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Boolean_Operations;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    public partial class CrossSectionSolid : Solid
    {

        /// <summary>
        /// Creates from tessellated solid.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="numberOfLayers">The number of layers.</param>
        /// <returns>CrossSectionSolid.</returns>
        public static CrossSectionSolid CreateFromTessellatedSolid(TessellatedSolid ts, CartesianDirections direction, int numberOfLayers)
        {
            var intDir = Math.Abs((int)direction) - 1;
            var max = intDir == 0 ? ts.Bounds[1].X : intDir == 1 ? ts.Bounds[1].Y : ts.Bounds[1].Z;
            var min = intDir == 0 ? ts.Bounds[0].X : intDir == 1 ? ts.Bounds[0].Y : ts.Bounds[0].Z;
            var lengthAlongDir = max - min;
            var stepSize = lengthAlongDir / numberOfLayers;
            var stepDistances = new Dictionary<int, double>();
            //var stepDistances = new double[numberOfLayers];
            stepDistances.Add(0, min + 0.5 * stepSize);
            //stepDistances[0] = ts.Bounds[0][intDir] + 0.5 * stepSize;
            for (int i = 1; i < numberOfLayers; i++)
                stepDistances.Add(i, stepDistances[i - 1] + stepSize);
            //stepDistances[i] = stepDistances[i - 1] + stepSize;
            var bounds = new[] { ts.Bounds[0].Copy(), ts.Bounds[1].Copy() };

            var layers = ts.GetUniformlySpacedCrossSections(direction, stepDistances[0], numberOfLayers, stepSize);
            var layerDict = new Dictionary<int, IList<Polygon>>();
            for (int i = 0; i < layers.Length; i++)
                layerDict.Add(i, layers[i]);
            var directionVector = Vector3.UnitVector(direction);
            return new CrossSectionSolid(directionVector, stepDistances, ts.SameTolerance, layerDict, bounds, ts.Units);
        }


        /// <summary>
        /// Creates from tessellated solid.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="numberOfLayers">The number of layers.</param>
        /// <returns>CrossSectionSolid.</returns>
        public static CrossSectionSolid CreateFromTessellatedSolid(TessellatedSolid ts, Vector3 direction, int numberOfLayers)
        {
            direction = direction.Normalize();
            var (min, max) = ts.Vertices.GetDistanceToExtremeVertex(direction, out _, out _);
            var lengthAlongDir = max - min;
            var stepSize = lengthAlongDir / numberOfLayers;
            var stepDistances = new Dictionary<int, double>();
            //var stepDistances = new double[numberOfLayers];
            stepDistances.Add(0, min + 0.5 * stepSize);
            //stepDistances[0] = ts.Bounds[0][intDir] + 0.5 * stepSize;
            for (int i = 1; i < numberOfLayers; i++)
                stepDistances.Add(i, stepDistances[i - 1] + stepSize);
            //stepDistances[i] = stepDistances[i - 1] + stepSize;
            var bounds = new[] { ts.Bounds[0].Copy(), ts.Bounds[1].Copy() };

            var layers = ts.GetUniformlySpacedCrossSections(direction, stepDistances[0], numberOfLayers, stepSize);
            var layerDict = new Dictionary<int, IList<Polygon>>();
            for (int i = 0; i < layers.Length; i++)
                layerDict.Add(i, layers[i]);
            return new CrossSectionSolid(direction, stepDistances, ts.SameTolerance, layerDict, bounds, ts.Units);
        }

        /// <summary>
        /// Creates the uniform cross section solid.
        /// </summary>
        /// <param name="buildDirection">The build direction.</param>
        /// <param name="distanceOfPlane">The distance of plane.</param>
        /// <param name="extrudeThickness">The extrude thickness.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <param name="units">The units.</param>
        /// <returns>CrossSectionSolid.</returns>
        public static CrossSectionSolid CreateConstantCrossSectionSolid(Vector3 buildDirection, double distanceOfPlane, double extrudeThickness,
            IEnumerable<Polygon> shape, double sameTolerance, UnitType units)
        {
            var shapeList = shape as IList<Polygon> ?? shape.ToList();
            var stepDistances = new Dictionary<int, double> { { 0, distanceOfPlane }, { 1, distanceOfPlane + extrudeThickness } };
            var layers2D = new Dictionary<int, IList<Polygon>> { { 0, shapeList }, { 1, shapeList } };
            return new CrossSectionSolid(buildDirection, stepDistances, sameTolerance, layers2D, null, units);
        }


        /// <summary>
        /// Creates the uniform cross section solid.
        /// </summary>
        /// <param name="buildDirection">The build direction.</param>
        /// <param name="distanceOfPlane">The distance of plane.</param>
        /// <param name="extrudeThickness">The extrude thickness.</param>
        /// <param name="shape">The shape.</param>
        /// <param name="sameTolerance">The same tolerance.</param>
        /// <param name="units">The units.</param>
        /// <returns>CrossSectionSolid.</returns>
        public static CrossSectionSolid CreateConstantCrossSectionSolid(Vector3 buildDirection, double distanceOfPlane, double extrudeThickness,
            Polygon shape, double sameTolerance, UnitType units)
        {
            var stepDistances = new Dictionary<int, double> { { 0, distanceOfPlane }, { 1, distanceOfPlane + extrudeThickness } };
            var layers2D = new Dictionary<int, IList<Polygon>> { { 0, new[] { shape } }, { 1, new[] { shape } } };
            return new CrossSectionSolid(buildDirection, stepDistances, sameTolerance, layers2D, null, units);
        }


        public Vector3[][][] GetCrossSectionsAs3DLoops()
        {
            var result = new Vector3[Layer2D.Count][][];
            int k = 0;
            foreach (var layerKeyValuePair in Layer2D)
            {
                var index = layerKeyValuePair.Key;
                var zValue = StepDistances[index];
                var numLoops = layerKeyValuePair.Value.Sum(poly => 1 + poly.NumberOfInnerPolygons);
                var layer = new Vector3[numLoops][];
                result[k++] = layer;
                int j = 0;

                //Check that the loop does not contain any duplicate points
                var skipHoles = false;
                var layer2DLoops = layerKeyValuePair.Value;
                if (ContainsDuplicatePoints(layer2DLoops))
                {
                    //try offsetting in by a small amount
                    layer2DLoops = PolygonOperations.OffsetSquare(layer2DLoops, -Constants.LineSlopeTolerance, Constants.LineSlopeTolerance);
                    //Offsetting did not work, so try offsetting by a larger amount.
                    if (ContainsDuplicatePoints(layer2DLoops))
                    {
                        layer2DLoops = PolygonOperations.OffsetSquare(layer2DLoops, -Constants.LineSlopeTolerance * 10, Constants.LineSlopeTolerance);
                        //If still no luck, then skip the inner loops
                        skipHoles = ContainsDuplicatePoints(layer2DLoops);
                    }            
                }
     
                foreach (var poly in layer2DLoops)
                {
                    if (skipHoles)
                    {
                        var loop = new Vector3[poly.Path.Count];
                        layer[j] = loop;
                        for (int i = 0; i < loop.Length; i++)
                            loop[i] = new Vector3(poly.Path[i], zValue).Transform(BackTransform);
                        j++;
                    }
                    else
                    {
                        foreach (var innerPoly in poly.AllPolygons)
                        {
                            var loop = new Vector3[innerPoly.Path.Count];
                            layer[j] = loop;
                            for (int i = 0; i < loop.Length; i++)
                                loop[i] = new Vector3(innerPoly.Path[i], zValue).Transform(BackTransform);
                            j++;
                        }
                    }                    
                }                  
            }
            return result;
        }

        private static bool ContainsDuplicatePoints(IList<Polygon> layer)
        {
            var allPoints = new HashSet<Vector2>();
            foreach (var poly in layer)
            {
                foreach (var innerPoly in poly.AllPolygons)
                {
                    foreach (var point in innerPoly.Path)
                    {
                        if (allPoints.Contains(point))
                            return true;
                        else
                            allPoints.Add(point);
                    }
                }
            }
            return false;
        }
    }
}