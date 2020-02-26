using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TVGL.Numerics;

namespace TVGL
{
    public partial class CrossSectionSolid : Solid
    {
        /// <summary>
        /// Layers are the 2D and 3D layers for every cross section in this feature.
        /// Layer 3D is optional and may be null, but Layer2D cannot be null.
        /// The solid volume representation assumes that the cross sections are defined,
        /// such that the first valid loop and subsequent loops in layer3D will be extruded 
        /// forward along the given direction, but not extruding the last cross section, which
        /// forms the end of the solid. 
        /// 
        /// A few features:
        /// 1) Layer2D and 3D can be empty layers at the start and end of the step indices. This 
        /// can be useful when joining solids of different sizes along the same direction.
        /// 2) Layer2D and 3D can be indexed in the reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. If reversed, it will
        /// extrude backward from the first valid loop in Layer3D up until the last valid loop
        /// in the list.
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, List<List<Vertex>>> Layer3D;
        [JsonIgnore]
        public Dictionary<int, List<PolygonLight>> Layer2D;
        // an alternate approach without using dictionaries should be pursued
        //public List<List<Vertex>>[] Layer3D { get; set; }
        //public List<PolygonLight>[] Layer2D { get; }

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
        public Vector3 Direction { get; set; }
        // in the future, wouldn't this just be
        // { get { return new[] { TranformMatrix[2, 0], TranformMatrix[2, 1], TranformMatrix[2, 2] }; } }

        public Matrix4x4 TranformMatrix { get; set; } = Matrix4x4.Identity;


        public int NumLayers { get; set; }

        [JsonConstructor]
        public CrossSectionSolid(Dictionary<int, double> stepDistances)
        {
            Layer2D = new Dictionary<int, List<PolygonLight>>();
            Layer3D = new Dictionary<int, List<List<Vertex>>>();
            StepDistances = stepDistances;
        }

        public CrossSectionSolid(Vector3 direction, Dictionary<int, double> stepDistances, double sameTolerance, Vector3[] bounds = null, UnitType units = UnitType.unspecified)
            : this(stepDistances)
        {
            Direction = direction.Copy();
            NumLayers = stepDistances.Count;
            if (bounds != null)
                Bounds = new[] { bounds[0].Copy(), bounds[1].Copy() };
            Units = units;
            SameTolerance = sameTolerance;
        }

        public CrossSectionSolid(Vector3 direction, Dictionary<int, double> stepDistances, double sameTolerance, Dictionary<int, List<PolygonLight>> Layer2D, Vector3[] bounds = null,
            UnitType units = UnitType.unspecified)
        {
            NumLayers = stepDistances.Count;
            StepDistances = stepDistances;
            Units = units;
            SameTolerance = sameTolerance;
            Direction = direction.Copy();
            this.Layer2D = Layer2D;
            Layer3D = new Dictionary<int, List<List<Vertex>>>();
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

            var layers = CrossSectionSolid.GetUniformlySpacedSlices(ts, direction, stepDistances[0], numberOfLayers, stepSize);
            var layerDict = new Dictionary<int, List<PolygonLight>>();
            for (int i = 0; i < layers.Length; i++)
                layerDict.Add(i, layers[i]);
            var directionVector = new double[3];
            directionVector[intDir] = Math.Sign((int)direction);
            var cs = new CrossSectionSolid(new Vector3(directionVector), stepDistances, ts.SameTolerance, layerDict, bounds, ts.Units);
            //var cs = new CrossSectionSolid(stepDistances, layers, bounds, ts.Units);
            cs.TranformMatrix = Matrix4x4.Identity;
            return cs;
        }

        public void Add(List<Vertex> feature3D, PolygonLight feature2D, int layer)
        {
            if (!Layer3D.ContainsKey(layer))
            {
                Layer3D[layer] = new List<List<Vertex>>();
                Layer2D[layer] = new List<PolygonLight>();
            }
            //Layer 3D is optional and may be null, but Layer2D cannot be null.
            if (feature3D != null) Layer3D[layer].Add(feature3D);
            Layer2D[layer].Add(feature2D);
        }

        public void SetAllVertices()
        {
            foreach (var layer in Layer2D) SetVerticesByLayer(layer.Key);
        }

        public void SetVerticesByLayer(int i)
        {
            var layer = Layer3D[i] = new List<List<Vertex>>();
            foreach (var polygon in Layer2D[i])
            {
                layer.Add(MiscFunctions.GetVerticesFrom2DPoints(polygon.Path, Direction, StepDistances[i]));
            }
        }

        public void SetVolume(bool extrudeBack = true)
        {
            Volume = 0.0;
            var start = Layer2D.Where(p => p.Value.Any()).FirstOrDefault().Key;
            var stop = Layer2D.Where(p => p.Value.Any()).LastOrDefault().Key;
            var reverse = start < stop ? 1 : -1;
            //If extruding back, then we skip the first loop, and extrude backward from the remaining loops.
            //Otherwise, extrude the first loop and all other loops forward, except the last loop.
            //Which of these extrusion options you choose depends on how the cross sections were defined.
            //But both methods, only result in material between the cross sections.
            if (extrudeBack) start += reverse;
            else stop -= reverse;
            for (var i = start; i * reverse <= stop * reverse; i += reverse) //Include the last index, since we already modified start or stop
            {
                double distance;
                if (extrudeBack) distance = Math.Abs(StepDistances[i] - StepDistances[i - reverse]);//current - prior (reverse extrusion)        
                else distance = Math.Abs(StepDistances[i + reverse] - StepDistances[i]); //next - current (forward extrusion)
                Volume += distance * Layer2D[i].Sum(p => p.Area);
            }
        }
        //public void SetVolume()
        //{
        //    Volume = 0.0;
        //    for (var i = 0; i < NumLayers - 1; i++) //Include the last index, since we already modified start or stop
        //    {
        //        if (Layer2D[i] == null || !Layer2D[i].Any() || Layer2D[i + 1] == null || !Layer2D[i + 1].Any()) continue;
        //        var halfThickness = 0.5 * (StepDistances[i + 1] - StepDistances[i]);
        //        Volume += halfThickness * (Layer2D[i].Sum(p => p.Area) + Layer2D[i].Sum(p => p.Area));
        //    }
        //}

        /// <summary>
        /// Layer2D and 3D can be indexed in the forward or reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. In both cases, it will start 
        /// by extruding the first valid loop in Layer3D and stop before extruding the last one (the 
        /// n-1 extrusion will have ended on at the distance of the final cross section). 
        /// If reversed, it will simply extrude backward instead of forward.
        /// </summary>
        public IReadOnlyCollection<PolygonalFace> ConvertToTessellatedExtrusions(bool extrudeBack = true)
        {
            if (!Layer3D.Any()) SetAllVertices();
            var start = Layer3D.Where(p => p.Value.Any()).FirstOrDefault().Key;
            var stop = Layer3D.Where(p => p.Value.Any()).LastOrDefault().Key;
            var reverse = start < stop ? 1 : -1;
            var direction = reverse == 1 ? Direction : -1 * Direction;
            var faces = new List<PolygonalFace>();
            //If extruding back, then we skip the first loop, and extrude backward from the remaining loops.
            //Otherwise, extrude the first loop and all other loops forward, except the last loop.
            //Which of these extrusion options you choose depends on how the cross sections were defined.
            //But both methods, only result in material between the cross sections.
            if (extrudeBack)
            {
                direction = -1 * direction;
                start += reverse;
            }
            else stop -= reverse;
            for (var i = start; i * reverse <= stop * reverse; i += reverse) //Include the last index, since we already modified start or stop
            {
                if (!Layer3D[i].Any()) continue; //THere can be gaps in layer3D if this actually represents more than one solid body
                double distance;
                if (extrudeBack) distance = Math.Abs(StepDistances[i] - StepDistances[i - reverse]);//current - prior (reverse extrusion)        
                else distance = Math.Abs(StepDistances[i + reverse] - StepDistances[i]); //next - current (forward extrusion)
                var layerfaces = Extrude.ReturnFacesFromLoops(Layer3D[i], direction, distance, false);
                if (layerfaces == null) continue;
                faces.AddRange(layerfaces);
            }
            return faces;
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

        public override Solid Copy()
        {
            var solid = new CrossSectionSolid(Direction, StepDistances, SameTolerance, Bounds, Units);
            //Recreate the loops, so that the lists are not linked to the original.
            //Since polygonlight is a struct, it will not be linked.
            foreach (var layer in Layer2D)
            {
                solid.Layer2D.Add(layer.Key, new List<PolygonLight>(layer.Value));
            }
            //To create an unlinked copy for layer3D, we need to create new lists and copy the vertices
            foreach (var layer in Layer3D)
            {
                var newLoops = new List<List<Vertex>>();
                foreach (var loop in layer.Value)
                {
                    var newLoop = new List<Vertex>();
                    foreach (var vertex in loop)
                    {
                        newLoop.Add(vertex.Copy());
                    }
                    newLoops.Add(newLoop);
                }
                solid.Layer3D.Add(layer.Key, newLoops);
            }
            if (!Volume.IsNegligible()) solid.Volume = Volume;
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


        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            serializationData = new Dictionary<string, JToken>();
            serializationData.Add("CrossSections",
                JToken.FromObject(Layer2D.Values.Select(polygonlist => polygonlist.Select(p => p.ConvertToDoublesArray()))));
        }


        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["CrossSections"];
            var layerArray = jArray.ToObject<Vector2[][]>();
            Layer2D = new Dictionary<int, List<PolygonLight>>();
            var keysArray = StepDistances.Keys.ToArray();
            for (int i = 0; i < layerArray.Length; i++)
            {
                var layer = new List<PolygonLight>();
                var key = keysArray[i];
                foreach (var coordinates in layerArray[i])
                    layer.Add(PolygonLight.MakeFromBinaryString(coordinates));
                Layer2D.Add(key, layer);
            }

        }

    }
}
