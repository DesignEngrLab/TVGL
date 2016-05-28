// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="ListFunctions.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     Miscilaneous list functions
    /// </summary>
    public static class ListFunctions
    {
        /// <summary>
        ///     Gets all the faces with distinct normals. NOT SURE THIS WORKS PROPERLY.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns>List&lt;PolygonalFace&gt;.</returns>
        public static List<PolygonalFace> FacesWithDistinctNormals(List<PolygonalFace> faces)
        {
            var distinctList = new List<PolygonalFace>(faces);
            var n = faces.Count;
            var checkSumMultipliers = new[] {1, 1e8, 1e16};
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
        ///     Gets a list of flats, given a list of faces.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="minSurfaceArea">The minimum surface area.</param>
        /// <returns>List&lt;Flat&gt;.</returns>
        public static List<Flat> Flats(IList<PolygonalFace> faces, double tolerance = Constants.ErrorForFaceInSurface,
            double minSurfaceArea = 0.01)
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
    }
}