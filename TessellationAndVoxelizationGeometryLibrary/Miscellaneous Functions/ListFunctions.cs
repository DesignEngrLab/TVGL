using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    public static class ListFunctions
    {

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

        public static List<Flat> Flats(List<PolygonalFace> faces)
        {
            throw new Exception("These checksum values don't seem to be unique");
            var tolerance = 0.002;
            var n = faces.Count;
            var checkSumMultipliers = new double[] { 1, 1e8, 1e16 };
            var checkSums = new double[n];
            for (var i = 0; i < n; i++)
                checkSums[i] = (int)Math.Abs(checkSumMultipliers.dotProduct(faces[i].Normal));
            var indices = StarMath.makeLinearProgression(n);
            indices = indices.OrderBy(index => checkSums[index]).ToArray();

            var listFaces = new List<PolygonalFace>() { faces[indices[n - 1]] };
            var firstFlat = new Flat(listFaces);
            var listFlats = new List<Flat>(){firstFlat};
            var currentFlat = firstFlat;
            for (var i = n - 1; i > 0; i--)
            {
                var n1 = faces[indices[i]].Normal;
                var n2 = faces[indices[i-1]].Normal;
                if (currentFlat.IsNewMemberOf(faces[indices[i - 1]])) currentFlat.UpdateWith(faces[indices[i - 1]]);
                else
                {
                    //Create a new flat and add to list
                    listFaces[0] = faces[indices[i - 1]];
                    var newFlat = new Flat(listFaces);
                    listFlats.Add(newFlat);
                    currentFlat = newFlat;
                }
            }
            return listFlats;
        }

    }
}