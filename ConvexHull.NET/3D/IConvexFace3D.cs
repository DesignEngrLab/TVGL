namespace PointCloud.NET
{
    public interface IConvexFace3D
    {
        IConvexVertex3D peakVertex { get; set; }
        double peakDistance { get; set; }
        bool Visited { get; set; }

        /// <summary>
        /// Gets the collection of vertices that are on the boundary of the convex hull but are not actively effecting the boundary representation
        /// of the convex hull
        /// </summary>
        List<IConvexVertex3D> InteriorVertices { get; set; }
        Vector3 Normal { get; init; }

        IConvexVertex3D A { get; set; }
        IConvexVertex3D B { get; set; }
        IConvexVertex3D C { get; set; }
        IConvexEdge3D AB { get;set;}
        IConvexEdge3D BC { get;set;}
        IConvexEdge3D CA { get;set;}
    }
}
