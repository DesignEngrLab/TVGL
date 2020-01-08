using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

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
        public List<List<Vertex>>[] Layer3D { get; set; }
        public List<PolygonLight>[] Layer2D { get; }

        /// <summary>
        /// These faces are a visual representation of this solid. They are made
        /// up from extruding each layer as its own solid, such that this is essentially
        /// a collection of solids (i.e., many faces will be in the same planes, but have
        /// opposite normals). This is really meant for visualization only.
        /// </summary>
        public List<PolygonalFace> Faces;

        /// <summary>
        /// Step distances stores the distance along direction for each index.
        /// It can be bigger than either of the above dictionaries if, for example,
        /// you wanted to define multiple ParallelCrossSectionSolids along the same direction.
        /// <summary>
        public double[] StepDistances { get; }

        public double[,] TranformMatrix { get; set; } = StarMath.makeIdentity(4);
        double[] directionOfLayers
        { get { return new[] { TranformMatrix[2, 0], TranformMatrix[2, 1], TranformMatrix[2, 2] }; } }

        public double SameTolerance;


        public int NumLayers { get; }
        public CrossSectionSolid(Dictionary<int, double> stepDistances,
            UnitType units = UnitType.unspecified)
        {
            NumLayers = stepDistances.Count;
            StepDistances = stepDistances.Values.ToArray();
            Units = units;
        }
        public CrossSectionSolid(double[] stepDistances, List<PolygonLight>[] Layer2D, double[][] bounds = null, UnitType units = UnitType.unspecified)
        {
            NumLayers = stepDistances.Length;
            StepDistances = stepDistances;
            Units = units;
            this.Layer2D = Layer2D;
            if (bounds == null)
            {
                var xmin = double.PositiveInfinity;
                var xmax = double.NegativeInfinity;
                var ymin = double.PositiveInfinity;
                var ymax = double.NegativeInfinity;
                foreach (var layer in Layer2D)
                    foreach (var polygon in layer)
                        foreach (var point in polygon.Path)
                        {
                            if (xmin > point.X) xmin = point.X;
                            if (ymin > point.Y) ymin = point.Y;
                            if (xmax < point.X) xmax = point.X;
                            if (ymax < point.Y) ymax = point.Y;
                        }
                Bounds = new[] {
                new[] {xmin, ymin, StepDistances[0]},
                new[] {xmax, ymax, StepDistances[NumLayers-1]}
                };
            }
            else Bounds = bounds;
        }

        public static CrossSectionSolid CreateFromTessellatedSolid(TessellatedSolid ts, CartesianDirections direction, int numberOfLayers)
        {
            var intDir = Math.Abs((int)direction) - 1;
            var lengthAlongDir = ts.Bounds[1][intDir] - ts.Bounds[0][intDir];
            var stepSize = lengthAlongDir / numberOfLayers;
            var stepDistances = new double[numberOfLayers];
            stepDistances[0] = ts.Bounds[0][intDir] + 0.5 * stepSize;
            for (int i = 1; i < numberOfLayers; i++)
                stepDistances[i] = stepDistances[i - 1] + stepSize;
            var layers = CrossSectionSolid.GetUniformlySpacedSlices(ts, direction, stepDistances[0], numberOfLayers, stepSize);
            var bounds = new[] { (double[])ts.Bounds[0].Clone(), (double[])ts.Bounds[1].Clone() };
            var cs = new CrossSectionSolid(stepDistances, layers, bounds, ts.Units);
            cs.TranformMatrix = StarMath.makeIdentity(4);
            return cs;
        }

        public void Add(List<Vertex> feature3D, PolygonLight feature2D, int layer)
        {
            if (layer >= NumLayers)
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
            for (int i = 0; i < NumLayers; i++)
                SetVerticesByLayer(i);
        }

        public void SetVerticesByLayer(int i)
        {
            var layer = Layer3D[i] = new List<List<Vertex>>();
            foreach (var polygon in Layer2D[i])
            {
                layer.Add(MiscFunctions.GetVerticesFrom2DPoints(polygon.Path.Select(p => new Point(p.X, p.Y)), directionOfLayers, StepDistances[i]));
            }
        }

        public void SetVolume(bool extrudeBack = true)
        {
            Volume = 0.0;
            for (var i = 0; i < NumLayers - 1; i++) //Include the last index, since we already modified start or stop
            {
                if (Layer2D[i] == null || !Layer2D[i].Any() || Layer2D[i + 1] == null || !Layer2D[i + 1].Any()) continue;
                var halfThickness = 0.5 * (StepDistances[i + 1] - StepDistances[i]);
                Volume += halfThickness * (Layer2D[i].Sum(p => p.Area) + Layer2D[i].Sum(p => p.Area));
            }
        }

        /// <summary>
        /// Layer2D and 3D can be indexed in the forward or reverse order from the step distances.
        /// This can be useful when working in a bi-directional scope. In both cases, it will start 
        /// by extruding the first valid loop in Layer3D and stop before extruding the last one (the 
        /// n-1 extrusion will have ended on at the distance of the final cross section). 
        /// If reversed, it will simply extrude backward instead of forward.
        /// </summary>
        public void SetSolidRepresentation(bool extrudeBack = true)
        {
            if (!Layer3D.Any()) SetAllVertices();
            var start = 0;
            while (Layer2D[start] == null || !Layer2D[start].Any()) start++;
            var stop = NumLayers - 1;
            while (Layer2D[stop] == null || !Layer2D[stop].Any()) stop--;
            var reverse = start < stop ? 1 : -1;
            var direction = reverse == 1 ? directionOfLayers : directionOfLayers.multiply(-1);
            Faces = new List<PolygonalFace>();
            //If extruding back, then we skip the first loop, and extrude backward from the remaining loops.
            //Otherwise, extrude the first loop and all other loops forward, except the last loop.
            //Which of these extrusion options you choose depends on how the cross sections were defined.
            //But both methods, only result in material between the cross sections.
            if (extrudeBack)
            {
                direction = direction.multiply(-1);
                start += reverse;
            }
            else stop -= reverse;
            for (var i = start; i * reverse <= stop * reverse; i += reverse) //Include the last index, since we already modified start or stop
            {
                if (!Layer3D[i].Any()) continue; //THere can be gaps in layer3D if this actually represents more than one solid body
                double distance;
                if (extrudeBack) distance = Math.Abs(StepDistances[i] - StepDistances[i - reverse]);//current - prior (reverse extrusion)        
                else distance = Math.Abs(StepDistances[i + reverse] - StepDistances[i]); //next - current (forward extrusion)
                var faces = Extrude.ReturnFacesFromLoops(Layer3D[i], direction, distance, false);
                if (faces == null) continue;
                Faces.AddRange(faces);
            }
        }


        public override Solid Copy()
        {
            var solid = new CrossSectionSolid(StepDistances, Layer2D, Bounds, Units);
            //Recreate the loops, so that the lists are not linked to the original.
            //Since polygonlight is a struct, it will not be linked.
            for (int i = 0; i < NumLayers; i++)
            {
                solid.Layer2D[i] = Layer2D[i].ToList();
                //To create an unlinked copy for layer3D, we need to create new lists and copy the vertices
                var newLoops = new List<List<Vertex>>();
                foreach (var loop in Layer3D[i])
                {
                    var newLoop = new List<Vertex>();
                    foreach (var vertex in loop)
                    {
                        newLoop.Add(vertex.Copy());
                    }
                    newLoops.Add(newLoop);
                }
                solid.Layer3D[i] = newLoops;
            }
            return solid;
        }

        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes()
        {
            var marchingCubesAlgorithm = new MarchingCubesCrossSectionSolid(this);
            return marchingCubesAlgorithm.Generate();
        }

        public override void Transform(double[,] transformMatrix)
        {
            //It is really easy to rotate Layer2D, just change the direction. But, it is more complicated to get the transform distances correct.
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(double[,] transformationMatrix)
        {
            throw new NotImplementedException();
        }
    }
}
