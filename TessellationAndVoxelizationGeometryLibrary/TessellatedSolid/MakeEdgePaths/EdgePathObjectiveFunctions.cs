using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL
{
    public interface IEdgePathPairEvaluator
    {
        Vector3 CharacterizeAsVector(Vertex vertex, EdgePath edgePath);
        double ScorePair(Vector3 vector1, Vector3 vector2);
    }

    public class EdgePathLoopsAroundInputFaces : IEdgePathPairEvaluator
    {
        private readonly HashSet<TriangleFace> innerFaces;

        public EdgePathLoopsAroundInputFaces(IEnumerable<TriangleFace> inputFaces)
        {
            innerFaces =inputFaces as HashSet<TriangleFace>?? inputFaces.ToHashSet();
        }
        public Vector3 CharacterizeAsVector(Vertex vertex, EdgePath edgePath)
        {
            var edge = (edgePath.FirstVertex == vertex) ? edgePath.EdgeList[0] : edgePath.EdgeList[^1];
            var edgeUnitVector = edge.Vector.Normalize();
            if (edge.OwnedFace != null && innerFaces.Contains(edge.OwnedFace))
                return edge.OwnedFace.Normal.Cross(edgeUnitVector);
            else if (edge.OtherFace != null && innerFaces.Contains(edge.OtherFace))
                return edgeUnitVector.Cross(edge.OtherFace.Normal);
            else return Vector3.NaN;

        }

        public virtual double ScorePair(Vector3 vector1, Vector3 vector2)
        {
            return MiscFunctions.SmallerAngleBetweenVectorsEndToEnd(vector1, vector2);

        }
    }
    public class GetEdgePathLoopsAroundNullBorder : IEdgePathPairEvaluator
    {
        public Vector3 CharacterizeAsVector(Vertex vertex, EdgePath edgePath)
        {
            var edge = (edgePath.FirstVertex == vertex) ? edgePath.EdgeList[0] : edgePath.EdgeList[^1];
            var edgeUnitVector = edge.Vector.Normalize();
            if (edge.OwnedFace == null && edge.OtherFace != null)
                return edge.OtherFace.Normal.Cross(edgeUnitVector);
            else if (edge.OtherFace == null && edge.OwnedFace != null)
                return edgeUnitVector.Cross(edge.OwnedFace.Normal);
            else return Vector3.NaN;
        }

        public virtual double ScorePair(Vector3 vector1, Vector3 vector2)
        {
            return MiscFunctions.SmallerAngleBetweenVectorsEndToEnd(vector1, vector2);
        }
    }

}
