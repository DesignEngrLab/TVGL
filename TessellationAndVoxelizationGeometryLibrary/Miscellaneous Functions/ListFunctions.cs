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
           double minSurfaceArea = 0.01, int minNumberOfFacesPerFlat = 2)
        {
            //Note: This function has been optimized to run very fast for large amount of faces
            //Used hashet for "Contains" function calls 
            var unusedFaces = new HashSet<PolygonalFace>(faces);

            var listFlats = new List<Flat>();
            //Use an IEnumerable class (List) for iterating through each part, and then the 
            //"Contains" function to see if it was already used. This is actually much faster
            //than using a while loop with a ".Any" and ".First" call on the Hashset.
            foreach (var startFace in faces)
            {
                //If this faces has already been used, continue to the next face
                if (!unusedFaces.Contains(startFace)) continue;

                //Stacks a fast for "Push" and "Pop".
                //Add all the adjecent faces from the first face to the stack for 
                //consideration in the while loop below.
                var stack = new Stack<PolygonalFace>();
                foreach (var adjacentFace in startFace.AdjacentFaces)
                {
                    stack.Push(adjacentFace);
                }

                //Get the distance to origin
                var distanceToOrigin = startFace.Normal.dotProduct(startFace.Vertices[0].Position);
                unusedFaces.Remove(startFace);

                //Get all the faces that should be used on this flat
                //Use a hashset so we can use the ".Contains" function
                var flatFaces = new HashSet<PolygonalFace> { startFace };
                while (stack.Any())
                {
                    var newFace = stack.Pop();
                    //If the new face does not fit the criteria for the flat, continue
                    //This criteria includes 
                    //1. Must not already be included in the face list
                    if (flatFaces.Contains(newFace)) continue;

                    //2. Must have nearly the same normal
                    if (!newFace.Normal.dotProduct(startFace.Normal).IsPracticallySame(1.0, tolerance)) continue;

                    //3. Must be nearly the same distance from the origin (this may not be strictly neccessary 
                    //since we are wrapping along the surface by using the adjacent faces
                    //Note that the dotProduct term and distance to origin, must have the same sign, 
                    //so there is no additional need moth absolute value methods.
                    //NOTE:During testing this step (3) proved to actually produce worse results.
                    //if (newFace.Vertices.All(v => !startFace.Normal.dotProduct(v.Position).IsPracticallySame(distanceToOrigin, tolerance))) continue;

                    //If the face has already been used on another flat, continue
                    if (!unusedFaces.Contains(newFace)) continue;

                    //Add the new face to the flat's face list
                    flatFaces.Add(newFace);

                    //Remove the new face, since it is now being used
                    unusedFaces.Remove(newFace);

                    //Add new adjacent faces to the stack for consideration
                    //if the faces are already listed in the flat faces, the first
                    //"if" statement in the while loop will ignore them.
                    foreach (var adjacentFace in newFace.AdjacentFaces)
                    {
                        stack.Push(adjacentFace);
                    }
                }

                //Build the flat from the collected faces
                var flat = new Flat(flatFaces) { Tolerance = tolerance };

                //Criteria of whether it should be a flat should be inserted here.
                if (flat.Faces.Count < minNumberOfFacesPerFlat) continue;
                if (flat.Area < minSurfaceArea) continue;
                listFlats.Add(flat);
            }
            return listFlats;
        }
    }
}