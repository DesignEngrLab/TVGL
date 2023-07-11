using System.Linq;
using System.Numerics;

namespace TVGL.PointMatcherNet
{
    public struct DataPoint
    {
        public Vector3 point;
        public Vector3 normal;
    }

    public class DataPoints
    {
        public DataPoint[] points;
        public bool containsNormals;
    }

    public class Matches
    {		
        /// <summary>
        /// Squared distances to closest points
        /// Columns represent different query points, rows are k matches
        /// </summary>
		public double[] Dists;

        /// <summary>
        /// Identifiers of closest points
        /// </summary>
		public int[,] Ids;

    }

    public interface IMatcherFactory
    {
        IMatcher ConstructMatcher(DataPoints reference);
    }

    public interface IMatcher
    {
        Matches FindClosests(DataPoints filteredReading);
    }

    public interface IErrorMinimizer
    {
        EuclideanTransform SolveForTransform(ErrorElements mPts);
    }

    public interface IInspector
    {
        void Inspect(DataPoints pointSet, string name);
    }

    public class NoOpInspector : IInspector
    {
        public void Inspect(DataPoints pointSet, string name)
        {
            
        }
    }
}
