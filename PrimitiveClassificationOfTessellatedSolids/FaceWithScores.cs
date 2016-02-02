using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;

namespace PrimitiveClassificationOfTessellatedSolids
{
  internal  class FaceWithScores
  {
      internal PolygonalFace Face;

      internal FaceWithScores(PolygonalFace face)
      {
          Face = face;
      }



      ////////////////////////////////////////////////////////////////////
      /// <summary>
      /// gets the Area * edge ratio of the face. Edge ratio is the the length of longest
      /// edge to the length of the shortest edge
      /// </summary>
      /// <value>
      /// AreaRatio
      /// </value>
      internal double AreaRatio { get; set; }
      /// <summary>
      /// Dictionary with possible face category obtained from different 
      /// combinatons  of its edges' groups 
      /// </summary>
      /// <value>
      /// Face Category
      /// </value>
      internal Dictionary<PrimitiveSurfaceType, double> FaceCat { get; set; }
      /// <summary>
      /// Dictionary with faceCat on its key and the combinaton which makes the category on its value
      /// </summary>
      /// <value>
      /// Category to combination
      /// </value>
      internal Dictionary<PrimitiveSurfaceType, int[]> CatToCom { get; set; }
      /// <summary>
      /// Dictionary with edge combinations on key and edges obtained from face rules on its value
      /// </summary>
      /// <value>
      /// Combination to Edges
      /// </value>
      internal Dictionary<int[], Edge[]> ComToEdge { get; set; }
      /// <summary>
      /// Dictionary with faceCat on key and edges lead to the category on its value
      /// </summary>
      /// <value>
      /// Edges lead to desired category
      /// </value>
      internal Dictionary<PrimitiveSurfaceType, List<Edge>> CatToELDC { get; set; }
  }
  internal  class EdgeWithScores
    {
        internal Edge Edge;
        private Edge e;

        internal EdgeWithScores(Edge edge)
        {
            Edge = edge;
        }
        /// <summary>
        /// A dictionary with 5 groups of Flat, Cylinder, Sphere, Flat to Curve and Sharp Edge and their 
        /// probabilities.
        /// </summary>
        /// <value>
        /// Group Probability
        /// </value>
        internal Dictionary<int, double> CatProb { get; set; }
    }
}
