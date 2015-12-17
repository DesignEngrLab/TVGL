using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
//using PrimitiveClassificationOfTessellatedSolids;

namespace TVGL
{
    public static class ListFunctions
    {

        /// <summary>
        /// Gets all the faces with distinct normals. NOT SURE THIS WORKS PROPERLY.
        /// </summary>
        /// <param name="faces"></param>
        /// <returns></returns>
        public static List<PolygonalFace> FacesWithDistinctNormals(List<PolygonalFace> faces)
        {
            var distinctList = new List<PolygonalFace>(faces);
            var n = faces.Count;
            var checkSumMultipliers = new double[] { 1, 1e8, 1e16 };
            var checkSums = new double[n];
            for (var i = 0; i < n; i++)
                checkSums[i] = Math.Abs(checkSumMultipliers.dotProduct(faces[i].Normal));
            var indices = StarMath.makeLinearProgression(n);
            indices = indices.OrderBy(index => checkSums[index]).ToArray();
            for (var i = n - 1; i > 0; i--)
            {
                if (faces[indices[i]].Normal.subtract(faces[indices[i - 1]].Normal).IsNegligible()
                    || faces[indices[i]].Normal.subtract(faces[indices[i - 1]].Normal.multiply(-1)).IsNegligible()) 
                    distinctList[indices[i]] = null;
            }
            distinctList.RemoveAll(v => v == null);
            return distinctList;
        }

        /// <summary>
        /// Gets a list of flats, given a list of faces.
        /// </summary>
        /// <param name="faces"></param>
        /// <param name="tolerance"></param>
        /// <param name="minSurfaceArea"></param>
        /// <returns></returns>
        public static List<Flat> Flats(IList<PolygonalFace> faces, double tolerance = Constants.ErrorForFaceInSurface, double minSurfaceArea = 0.01)
        {
            var listFaces = new List<PolygonalFace>(faces);
            var listFlats = new List<Flat>();
            while (listFaces.Any())
            {
                var startFace = listFaces[0];
                var stack = new Stack<PolygonalFace>();
                foreach (var adjacentFace in startFace.AdjacentFaces)
                {
                    stack.Push(adjacentFace);
                }
                //Create new flat from start face
                var flat = new Flat(new List<PolygonalFace> {startFace}) {Tolerance = tolerance};
                listFaces.Remove(startFace); 
                var hashFaces = new HashSet<PolygonalFace>(listFaces);
                while (stack.Any())
                {
                    var newFace = stack.Pop();
                    if (hashFaces.Contains(newFace))
                    {
                        //Only visit once per iteration
                        hashFaces.Remove(newFace);
                        if (flat.IsNewMemberOf(newFace))
                        {
                            flat.UpdateWith(newFace);
                            listFaces.Remove(newFace);
                            foreach (var adjacentFace in newFace.AdjacentFaces)
                            {
                                stack.Push(adjacentFace);
                            }
                        }
                    } 
                }

                //Criteria of whether it should be a flat should be inserted here.
                if (flat.Faces.Count < 2) continue;
                if (flat.Area < minSurfaceArea) continue;
                listFlats.Add(new Flat(flat.Faces));
            }
            return listFlats;
        }

        
        /// <summary>
        /// Flattens all the faces on a flat to be on exactly the same plane. 
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static void RepairFlats(ref TessellatedSolid ts, ref List<Flat> flats)
        {
            var allEdgesRequiringUpdate = new HashSet<Edge>();
            var allFacesRequiringUpdate = new HashSet<PolygonalFace>();
            foreach (var flat in flats)
            {
                //Adjust flat normals to be equal to whatever the flat normal is set to.
                //and distance from origin to be equal to the flat's distance from origin.
                foreach (var face in flat.Faces)
                {
                    var adjustedVertices = new HashSet<Vertex>();
                    //Accomplish this, by adjusting all the vertices 
                    foreach (var vertex in face.Vertices)
                    {
                        //Skip, if this vertex has already been adjusted.
                        if (adjustedVertices.Contains(vertex)) continue;

                        //Find distance from origin and check if it is a negligible difference
                        var distanceToOrigin = vertex.Position.dotProduct(flat.Normal);
                        var difference = flat.DistanceToOrigin - distanceToOrigin;
                        if (difference.IsNegligible()) continue;
                        
                        //Else, move the vertex as necessary, along the direction of the normal
                        List<Edge> edgesRequiringUpdate;
                        List<PolygonalFace> facesRequiringUpdate;
                        var moveVector = flat.Normal.multiply(difference);
                        throw new Exception("not yet implemented. Needs to create a NEW vertex. Taboo to move vertices.");
                        //vertex.MoveByVector(moveVector, out edgesRequiringUpdate, out facesRequiringUpdate);
                        distanceToOrigin = vertex.Position.dotProduct(flat.Normal);
                        difference = flat.DistanceToOrigin - distanceToOrigin;
                        if (Math.Abs(difference) > 1E-12) throw new Exception();
                        foreach (var edgeRequiringUpdate in edgesRequiringUpdate)
                        {
                            if (!allEdgesRequiringUpdate.Contains(edgeRequiringUpdate)) allEdgesRequiringUpdate.Add(edgeRequiringUpdate);
                        }
                        foreach (var faceRequiringUpdate in facesRequiringUpdate)
                        {
                            if (!allFacesRequiringUpdate.Contains(faceRequiringUpdate)) allFacesRequiringUpdate.Add(faceRequiringUpdate);
                        }
                    }
                }
            }
            foreach (var edge in allEdgesRequiringUpdate)
            {
                edge.Update();
            }
            foreach (var face in allFacesRequiringUpdate)
            {
                face.Update();
            }
        }
    }
}