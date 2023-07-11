namespace TVGL.PointMatcherNet
{
    public class KdTreeMatcherFactory : IMatcherFactory
    {
        private int knn;
        private float epsilon;
        private float maxDist;

        public KdTreeMatcherFactory(int knn = 1, float epsilon = 0, float maxDist = float.PositiveInfinity)
        {
            this.knn = knn;
            this.epsilon = epsilon;
            this.maxDist = maxDist;
        }

        public IMatcher ConstructMatcher(DataPoints reference)
        {
            return new KdTreeMatcher(reference, this.knn, this.epsilon, this.maxDist);
        }

        public class KdTreeMatcher : IMatcher
        {
            private KdTreeNearestNeighborSearch kdtree;
            private int knn;
            private float epsilon;
            private float maxDist;

            public KdTreeMatcher(DataPoints cloud, int knn, float epsilon, float maxDist)
            {
                this.knn = knn;
                this.epsilon = epsilon;
                this.maxDist = maxDist;

                this.kdtree = new KdTreeNearestNeighborSearch(CreateCloudMatrix(cloud));
            }

            public Matches FindClosests(DataPoints filteredReading)
            {
                int n = filteredReading.points.Length;
                var results = DenseColumnMajorMatrixStorage<float>.OfInit(1, n, (i,j) => 0);
                var resultDistances = DenseColumnMajorMatrixStorage<float>.OfInit(1, n, (i, j) => 0);
                Vector<float> maxRadii = DenseVector.Create(n, i => this.maxDist);

                var query = CreateCloudMatrix(filteredReading);
                kdtree.knn(query, results, resultDistances, maxRadii, this.knn, this.epsilon, SearchOptionFlags.AllowSelfMatch);
                return new Matches
                {
                    Ids = results,
                    Dists = resultDistances
                };
            }

            private static DenseColumnMajorMatrixStorage<float> CreateCloudMatrix(DataPoints points)
            {
                var pts = points.points;
                var result = new DenseMatrix(3, pts.Length);
                for (int i = 0; i < pts.Length; i++)
                {
                    result.At(0, i, pts[i].point.X);
                    result.At(1, i, pts[i].point.Y);
                    result.At(2, i, pts[i].point.Z);
                }

                return (DenseColumnMajorMatrixStorage<float>)result.Storage;
            }
        }
    }
}
