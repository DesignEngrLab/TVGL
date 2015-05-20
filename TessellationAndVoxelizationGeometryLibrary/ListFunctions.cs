using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL.Tessellation;

namespace TVGL
{
    internal static class ListFunctions
    {

        internal static List<PolygonalFace> FacesWithDistinctNormals(List<PolygonalFace> faces)
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
                if (StarMath.IsNegligible(faces[indices[i]].Normal.subtract(faces[indices[i - 1]].Normal))
                    || StarMath.IsNegligible(faces[indices[i]].Normal.subtract(faces[indices[i - 1]].Normal.multiply(-1)))) 
                    distinctList[indices[i]] = null;
            }
            distinctList.RemoveAll(v => v == null);
            return distinctList;
        }

    }
}