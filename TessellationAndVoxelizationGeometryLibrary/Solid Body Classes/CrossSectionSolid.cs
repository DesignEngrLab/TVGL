using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    public class CrossSectionSolid : Solid
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
        public Dictionary<int, List<List<Vertex>>> Layer3D;
        public Dictionary<int, List<PolygonLight>> Layer2D;

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
        Dictionary<int, double> StepDistances;

        /// <summary>
        /// This is the direction that the cross sections will be extruded along
        /// </summary>
        public double[] Direction;

        public double SameTolerance;

        public CrossSectionSolid(double [] direction, Dictionary<int, double> stepDistances, double sameTolerance, UnitType units = UnitType.unspecified)
        {
            Layer2D = new Dictionary<int, List<PolygonLight>>();
            Layer3D = new Dictionary<int, List<List<Vertex>>>();
            Direction = direction;
            StepDistances = stepDistances;
            Units = units;
            SameTolerance = sameTolerance;
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
            foreach(var layer in Layer2D) SetVerticesByLayer(layer.Key);
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
                if(extrudeBack) distance = Math.Abs(StepDistances[i] - StepDistances[i - reverse]);//current - prior (reverse extrusion)        
                else distance = Math.Abs(StepDistances[i + reverse] - StepDistances[i]); //next - current (forward extrusion)
                Volume += distance * Layer2D[i].Sum(p => p.Area);
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
            var start = Layer3D.Where(p => p.Value.Any()).FirstOrDefault().Key;
            var stop = Layer3D.Where(p => p.Value.Any()).LastOrDefault().Key;
            var reverse = start < stop ? 1 : -1;
            var direction = reverse == 1 ? Direction : Direction.multiply(-1); 
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
            var solid = new CrossSectionSolid(Direction, StepDistances, SameTolerance, Units);
            //Recreate the loops, so that the lists are not linked to the original.
            //Since polygonlight is a struct, it will not be linked.
            foreach(var layer in Layer2D)
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
                    foreach(var vertex in loop)
                    {
                        newLoop.Add(vertex.Copy());
                    }
                    newLoops.Add(newLoop);
                }
                solid.Layer3D.Add(layer.Key, newLoops);
            }
            return solid;
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
