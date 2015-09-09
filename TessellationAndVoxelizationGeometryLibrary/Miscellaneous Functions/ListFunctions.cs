using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
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
        /// <param name="ts"></param>
        /// <returns></returns>
        public static List<Flat> Flats(TessellatedSolid ts, double minSurfaceArea = 0.01)
        {
            //throw new Exception("Not Implemented Correctly, yet");
            var faces = new List<PolygonalFace>(ts.Faces);
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
                var flat = new Flat(new [] {startFace});
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
                listFlats.Add(flat);
            }
            return listFlats;
        }
    }
}