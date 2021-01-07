// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TVGL.Boolean_Operations;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    public partial class CrossSectionSolid : Solid
    {
        /// <summary>
        /// Layers are the 2D polygons for every cross section in this feature.
        /// The solid volume representation assumes that the cross sections are defined,
        /// such that the first valid loop and subsequent loops in  will be extruded 
        /// forward along the given direction, but not extruding the last cross section, which
        /// forms the end of the solid. 
        /// 
        /// A few features:
        /// 1) Layer2D can be empty layers at the start and end of the step indices. This 
        /// can be useful when joining solids of different sizes along the same direction.
        /// 2) Layer2D can be indexed in the reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. If reversed, it will
        /// extrude backward from the first valid loop in Layer3D up until the last valid loop
        /// in the list.
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, IList<Polygon>> Layer2D;

        // an alternate approach without using dictionaries could be pursued
        //public List<Polygon>[] Layer2D { get; }

        /// <summary>
        /// Step distances stores the distance along direction for each index.
        /// It can be bigger than either of the above dictionaries if, for example,
        /// you wanted to define multiple ParallelCrossSectionSolids along the same direction.
        /// <summary>
        public Dictionary<int, double> StepDistances { get; }
        // an alternate approach without using dictionaries should be pursued
        //public Vector2 StepDistances { get; }
        /// <summary>
        /// This is the direction that the cross sections will be extruded along
        /// </summary>
        public Vector3 Direction => BackTransform.ZBasisVector;

        /// <summary>
        /// Gets or sets the transform matrix.
        /// </summary>
        /// <value>The transform matrix.</value>
        public Matrix4x4 TransformMatrix { get; set; }


        /// <summary>
        /// Gets or sets the transform matrix.
        /// </summary>
        /// <value>The transform matrix.</value>
        public Matrix4x4 BackTransform { get; set; }


        public int NumLayers { get; set; }

        [JsonConstructor]
        public CrossSectionSolid(Dictionary<int, double> stepDistances)
        {
            Layer2D = new Dictionary<int, IList<Polygon>>();
            StepDistances = stepDistances;
        }

        public CrossSectionSolid(Vector3 direction, Dictionary<int, double> stepDistances, double sameTolerance, Vector3[] bounds = null, UnitType units = UnitType.unspecified)
            : this(stepDistances)
        {
            TransformMatrix = direction.TransformToXYPlane(out var backTransform);
            BackTransform = backTransform;
            NumLayers = stepDistances.Count;
            if (bounds != null)
                Bounds = new[] { bounds[0].Copy(), bounds[1].Copy() };
            Units = units;
            SameTolerance = sameTolerance;
        }

        public CrossSectionSolid(Vector3 direction, Dictionary<int, double> stepDistances, double sameTolerance,
            Dictionary<int, IList<Polygon>> Layer2D, Vector3[] bounds = null, UnitType units = UnitType.unspecified)
        {
            NumLayers = stepDistances.Count;
            StepDistances = stepDistances;
            Units = units;
            SameTolerance = sameTolerance;
            TransformMatrix = direction.TransformToXYPlane(out var backTransform);
            BackTransform = backTransform;
            this.Layer2D = Layer2D;
            if (bounds == null)
            {
                var xmin = double.PositiveInfinity;
                var xmax = double.NegativeInfinity;
                var ymin = double.PositiveInfinity;
                var ymax = double.NegativeInfinity;
                foreach (var layer in Layer2D)
                    foreach (var polygon in layer.Value)
                        foreach (var point in polygon.Path)
                        {
                            if (xmin > point.X) xmin = point.X;
                            if (ymin > point.Y) ymin = point.Y;
                            if (xmax < point.X) xmax = point.X;
                            if (ymax < point.Y) ymax = point.Y;
                        }
                Bounds = new[] {
                new Vector3(xmin, ymin, StepDistances[0]),
                new Vector3(xmax, ymax, StepDistances[NumLayers-1])
                };
            }
            else Bounds = new[] { bounds[0].Copy(), bounds[1].Copy() };
        }

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

        public void Add(List<Vertex> feature3D, Polygon feature2D, int layer)
        {
            if (!Layer2D.ContainsKey(layer))
                Layer2D[layer] = new List<Polygon>();
            Layer2D[layer].Add(feature2D);
            _volume = double.NaN;
            _center = Vector3.Null;
            _inertiaTensor = Matrix3x3.Null;
            _surfaceArea = double.NaN;
        }


        /// <summary>
        /// Layer2D and 3D can be indexed in the forward or reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. In both cases, it will start 
        /// by extruding the first valid loop in Layer3D and stop before extruding the last one (the 
        /// n-1 extrusion will have ended on at the distance of the final cross section). 
        /// If reversed, it will simply extrude backward instead of forward.
        /// </summary>
        public TessellatedSolid ConvertToTessellatedExtrusions(bool extrudeBack, bool createFullVersion)
        {
            //if (!Layer3D.Any()) SetAllVertices();
            var start = Layer2D.FirstOrDefault(p => p.Value.Count > 0).Key;
            var stop = Layer2D.LastOrDefault(p => p.Value.Count > 0).Key;
            var increment = start < stop ? 1 : -1;
            //var direction = increment == 1 ? Direction : -1 * Direction;
            var faces = new List<PolygonalFace>();
            //If extruding back, then we skip the first loop, and extrude backward from the remaining loops.
            //Otherwise, extrude the first loop and all other loops forward, except the last loop.
            //Which of these extrusion options you choose depends on how the cross sections were defined.
            //But both methods, only result in material between the cross sections.
            if (extrudeBack)
            {
                //  direction = -1 * direction;
                start += increment;
            }
            else stop -= increment;
            for (var i = start; i * increment <= stop * increment; i += increment) //Include the last index, since we already modified start or stop
            {
                if (!Layer2D[i].Any()) continue; //THere can be gaps in layer3D if this actually represents more than one solid body
                var basePlaneDistance = extrudeBack ? StepDistances[i - increment] : StepDistances[i];
                var topPlaneDistance = extrudeBack ? StepDistances[i] : StepDistances[i + increment];
                //if (Layer2D[i].CreateShallowPolygonTrees(true, true, out var polygons, out _))
                var layerfaces = Layer2D[i].SelectMany(polygon => polygon.ExtrusionFacesFrom2DPolygons(Direction,
                    basePlaneDistance, topPlaneDistance - basePlaneDistance)).ToList();
                faces.AddRange(layerfaces);
            }
            return new TessellatedSolid(faces, createFullVersion, false);
        }

        public TessellatedSolid ConvertToLoftedTessellatedSolid()
        {
            var polygons = new Polygon[Layer2D.Count][];
            var faces = new List<PolygonalFace>();
            var previousLayerWasEmpty = true;
            var i = 0;
            foreach (var layer in Layer2D)
            {
                /*
                if (layer.Value == null)
                {
                    if (!previousLayerWasEmpty) //then need to triangulate the last layer facing up
                    {
                        polygons[i-1].tri
                    }
                    previousLayerWasEmpty = true;
                }
                else
                {
                    polygons[i] = layer.Value.CreateShallowPolygonTrees();
                    previousKey = layer.Key;
                    if (previousLayerWasEmpty) // then need to triangulate this layer facing down
                    {

                    }
                    previousLayerWasEmpty = false;
                }
                */
                i++;
            }

            return new TessellatedSolid(faces, false, false);
        }

        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes(double gridSize)
        {
            var marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this, gridSize);
            return marchingCubesAlgorithm.Generate();
        }
        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes(int approximateNumberOfTriangles = -1)
        {
            MarchingCubesCrossSectionSolid marchingCubesAlgorithm;
            if (approximateNumberOfTriangles == -1)
                marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this);
            else
            {
                var solidDimensions = Bounds[1] - Bounds[0];
                var bbVolume = solidDimensions.X * solidDimensions.Y * solidDimensions.Z;
                var biggestSideArea = bbVolume / Math.Min(solidDimensions.X, Math.Min(solidDimensions.Y, solidDimensions.Z));
                var areaPerTriangle = biggestSideArea / (MarchingCubesCrossSectionSolid.NumTrianglesOnSideFactor * approximateNumberOfTriangles);
                var discretization = 2 * Math.Sqrt(areaPerTriangle);
                marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this, discretization);
            }
            return marchingCubesAlgorithm.Generate();
        }

        public CrossSectionSolid Copy()
        {
            var solid = new CrossSectionSolid(Direction, StepDistances, SameTolerance, Bounds, Units);
            //Recreate the loops, so that the lists are not linked to the original.
            foreach (var layer in Layer2D)
            {
                solid.Layer2D.Add(layer.Key, new List<Polygon>(layer.Value));
            }
            solid._volume = _volume;
            solid._center = _center;
            solid._inertiaTensor = _inertiaTensor;
            solid._surfaceArea = _surfaceArea;
            return solid;
        }


        public override void Transform(Matrix4x4 transformMatrix)
        {
            //It is really easy to rotate Layer2D, just change the direction. But, it is more complicated to get the transform distances correct.
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            throw new NotImplementedException();
        }

        [JsonProperty]
        private int FirstIndex { get; set; }
        [JsonProperty]
        private int LastIndex { get; set; }


        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            serializationData = new Dictionary<string, JToken>();
            serializationData.Add("CrossSections",
                JToken.FromObject(Layer2D.Values.Select(polygonlist => polygonlist.Select(p => p.Path.ConvertTo1DDoublesCollection()))));
            FirstIndex = Layer2D.Keys.First();
            LastIndex = Layer2D.Keys.Last();
        }

        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["CrossSections"];
            var layerArray = jArray.ToObject<double[][][]>();
            Layer2D = new Dictionary<int, IList<Polygon>>();
            var j = 0;
            for (int i = FirstIndex; i <= LastIndex; i++)
            {
                var layer = new List<Polygon>();
                foreach (var coordinates in layerArray[j])
                    layer.Add(new Polygon(coordinates.ConvertToVector2s().ToList()));
                Layer2D.Add(i, layer);
                j++;
            }
        }

        protected override void CalculateCenter()
        {
            throw new NotImplementedException();
        }

        protected override void CalculateVolume()
        {
            _volume = 0.0;
            var index = StepDistances.Keys.First();
            var prevDistance = StepDistances.Values.First();
            var layer2D = Layer2D.ContainsKey(index) ? Layer2D[index] : null;
            var prevArea = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Area);
            foreach (var stepDistanceKVP in StepDistances.Skip(1))  // skip the first, this is shown above.
            {
                index = stepDistanceKVP.Key;
                var distance = stepDistanceKVP.Value;
                layer2D = Layer2D.ContainsKey(index) ? Layer2D[index] : null;
                var area = layer2D == null || layer2D.Count == 0 ? 0.0 : layer2D.Sum(p => p.Area);
                _volume += (prevArea + area) * (distance - prevDistance);
                prevArea = area;
                prevDistance = distance;
            }
            _volume *= 0.5; //actually, this is the trapezoidal volume between every layer. It's subtle, but since
            // we sum the area (5 lines above) instead of averaging them (to get the trapezoid area), we simply
            // divide by 2 at the end for simplicity/efficiency/accuracy.
            if (_volume < 0) _volume = -_volume;
        }

        protected override void CalculateSurfaceArea()
        {
            throw new NotImplementedException();
        }

        protected override void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }
    }
}
