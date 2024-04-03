namespace ConvexHull.NET
{
    public class ConvexHullEdge : IConvexEdge3D
    {
        public IConvexFace3D OwnedFace { get; set; }
        public IConvexFace3D OtherFace { get ; set ; }
        public IConvexVertex3D From { get; set; }
        public IConvexVertex3D To { get; set; }

        public bool Equals(IConvexEdge3D? other) => this == other;

        public ConvexHullEdge()
        {
            //OwnedFace = face1;
            //OtherFace = face2;
        }
    }

}
