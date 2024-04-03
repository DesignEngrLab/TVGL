using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL.Enclosure_Operations
{
    internal class ConvexHull
    {


        /// <summary>
        /// Creates the convex hull for a tessellated solid. This result of the method is stored 
        /// with the "ConvexHull" property of the TessellatedSolid. The vertices of the convex hull
        /// are a subset of the vertices of the TessellatedSolid and are unaffected by the method.
        /// So, while convex hull faces and edges connect to the vertices of the convex hull, the
        /// vertices do not point back to the convex hull faces and edges.
        /// Addtionally, the "PartOfConvexHull" property of the vertices, edges, and faces are set here.
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static bool Create(TessellatedSolid ts)
        {
            if (Create(ts.Vertices, out var convexHull, false, ts.SameTolerance))
            {
                ts.ConvexHull = convexHull;
                foreach (var face in ts.Faces.Where(face => face.Vertices.All(v => v.PartOfConvexHull)))
                {
                    face.PartOfConvexHull = true;
                    foreach (var e in face.Edges)
                        if (e != null) e.PartOfConvexHull = true;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
