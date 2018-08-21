using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using StarMathLib;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Voxelization;
using Constants = TVGL.Constants;


namespace TVGLTest
{
    internal class Program
    {
        private static readonly string[] FileNames = {
           //"../../../TestFiles/Binary.stl",
         //   "../../../TestFiles/ABF.ply",
          //   "../../../TestFiles/Beam_Boss.STL",
         "../../../TestFiles/Beam_Clean.STL",

        "../../../TestFiles/bigmotor.amf",
        "../../../TestFiles/DxTopLevelPart2.shell",
        "../../../TestFiles/Candy.shell",
        "../../../TestFiles/amf_Cube.amf",
        "../../../TestFiles/train.3mf",
        "../../../TestFiles/Castle.3mf",
        "../../../TestFiles/Raspberry Pi Case.3mf",
       //"../../../TestFiles/shark.ply",
       "../../../TestFiles/bunnySmall.ply",
        "../../../TestFiles/cube.ply",
        "../../../TestFiles/airplane.ply",
        "../../../TestFiles/TXT - G5 support de carrosserie-1.STL.ply",
        "../../../TestFiles/Tetrahedron.STL",
        "../../../TestFiles/off_axis_box.STL",
           "../../../TestFiles/Wedge.STL",
        "../../../TestFiles/Mic_Holder_SW.stl",
        "../../../TestFiles/Mic_Holder_JR.stl",
        "../../../TestFiles/3_bananas.amf",
        "../../../TestFiles/drillparts.amf",  //Edge/face relationship contains errors
        "../../../TestFiles/wrenchsns.amf", //convex hull edge contains a concave edge outside of tolerance
        "../../../TestFiles/hdodec.off",
        "../../../TestFiles/tref.off",
        "../../../TestFiles/mushroom.off",
        "../../../TestFiles/vertcube.off",
        "../../../TestFiles/trapezoid.4d.off",
        "../../../TestFiles/ABF.STL",
        "../../../TestFiles/Pump-1repair.STL",
        "../../../TestFiles/Pump-1.STL",
        "../../../TestFiles/SquareSupportWithAdditionsForSegmentationTesting.STL",
        "../../../TestFiles/Beam_Clean.STL",
        "../../../TestFiles/Square_Support.STL",
        "../../../TestFiles/Aerospace_Beam.STL",
        "../../../TestFiles/Rook.amf",
       "../../../TestFiles/bunny.ply",

        "../../../TestFiles/piston.stl",
        "../../../TestFiles/Z682.stl",
        "../../../TestFiles/sth2.stl",
        "../../../TestFiles/Cuboide.stl", //Note that this is an assembly 
        "../../../TestFiles/new/5.STL",
       "../../../TestFiles/new/2.stl", //Note that this is an assembly 
        "../../../TestFiles/new/6.stl", //Note that this is an assembly  //breaks in slice at 1/2 y direction
       "../../../TestFiles/new/4.stl", //breaks because one of its faces has no normal
        "../../../TestFiles/radiobox.stl",
        "../../../TestFiles/brace.stl",  //Convex hull fails in MIconvexHull
        "../../../TestFiles/G0.stl",
        "../../../TestFiles/GKJ0.stl",
        "../../../TestFiles/testblock2.stl",
        "../../../TestFiles/Z665.stl",
        "../../../TestFiles/Casing.stl", //breaks because one of its faces has no normal
        "../../../TestFiles/mendel_extruder.stl",

       "../../../TestFiles/MV-Test files/holding-device.STL",
       "../../../TestFiles/MV-Test files/gear.STL"
        };

        [STAThread]
        private static void Main(string[] args)
        {
            var writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            DirectoryInfo dir;
            try
            {
                //x64
                dir = new DirectoryInfo("../../../../TestFiles");
            }
            catch
            {
                //x86
                dir = new DirectoryInfo("../../../TestFiles");
            }
            var fileNames = dir.GetFiles("*");
            //Casing = 18
            //SquareSupport = 75
            for (var i = 18; i < fileNames.Count(); i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                List<TessellatedSolid> ts;
                if (!File.Exists(filename)) continue;
                using (fileStream = File.OpenRead(filename))
                    ts = IO.Open(fileStream, filename);
                if (!ts.Any()) continue;
               // Presenter.ShowAndHang(ts);

                //Put your test function here
                var silhouette = TVGL.Silhouette.Run(ts[0], new[] { 0.5, 0.0, 0.5 });
                foreach (var positivePolygon in silhouette.Where(p => MiscFunctions.AreaOfPolygon(p) > 0))
                {
                    var area = MiscFunctions.AreaOfPolygon(positivePolygon);
                    //Presenter.ShowAndHang(positivePolygon);
                    var sampled = PolygonOperations.SampleWithEdgeLength(positivePolygon, MiscFunctions.Perimeter(positivePolygon) / 600);
                    var smaller = PolygonOperations.OffsetRound(sampled, -0.001 * MiscFunctions.Perimeter(positivePolygon)).Select(p => new PolygonLight(p)).First();
                    //Presenter.ShowAndHang(sampled);
                    //var circlePaths = new List<List<PointLight>>();
                    //var medialAxisPoints = GetMedialAxisPoints(new PolygonLight(sampled));
                    //circlePaths.Add(medialAxisPoints);
                    //circlePaths.Add(sampled);
                    //Presenter.ShowAndHang(circlePaths);

                    //Delaunay Medial Axis
                    var allTriangles = new List<List<PointLight>>();     
                    allTriangles.Add(sampled);
                    var delaunay = MIConvexHull.Triangulation.CreateDelaunay(sampled);
                    var lines = new List<List<Point>>();
                    foreach (var triangle in delaunay.Cells)
                    {
                        var triangleCenterLineVertices = new List<Point>();
                        var edge1Center = new Point(triangle.Vertices[0].Position.add(triangle.Vertices[1].Position)
                            .divide(2));
                        if (MiscFunctions.IsPointInsidePolygon(smaller, edge1Center.Light))
                        {
                            triangleCenterLineVertices.Add(edge1Center);
                        }

                        var edge2Center = new Point(triangle.Vertices[1].Position.add(triangle.Vertices[2].Position)
                            .divide(2));
                        if (MiscFunctions.IsPointInsidePolygon(smaller, edge2Center.Light))
                        {
                            triangleCenterLineVertices.Add(edge2Center);
                        }

                        var edge3Center = new Point(triangle.Vertices[2].Position.add(triangle.Vertices[0].Position)
                            .divide(2));
                        if (MiscFunctions.IsPointInsidePolygon(smaller, edge3Center.Light))
                        {
                            triangleCenterLineVertices.Add(edge3Center);
                        }

                        if (triangleCenterLineVertices.Any())
                        {
                            if (triangleCenterLineVertices.Count == 1)
                            {
                                continue; // This vertex has no line associated with it. 
                                //If the vertex should be attached to a line, it will show up again for another triangle.
                            }
                            if (triangleCenterLineVertices.Count == 3)
                            {
                                //If there are two long edges with one short, collapse the long edges to the middle
                                //of the short edge.
                                //If 
                                //Order the points, such that the larger edge is not included
                                var d0 = edge1Center.Position.subtract(edge2Center.Position).norm2();
                                var d1 = edge2Center.Position.subtract(edge3Center.Position).norm2();
                                var d2 = edge3Center.Position.subtract(edge1Center.Position).norm2();
                                var ds = new List<double>() {d0, d1, d2};
                                ds.Sort();
                                if (ds[0] - ds[1] > ds[1] - ds[2])
                                {
                                    //There is a bigger difference in length between the longest edge than the other two
                                    //Therefore, remove the longest edge.
                                    if (d0 > d1 && d0 > d2)
                                    {
                                        //If d0 is the largest edge, it should be point 2,3,1
                                        lines.Add(new List<Point> {edge2Center, edge3Center});
                                        lines.Add(new List<Point> {edge3Center, edge1Center});
                                    }
                                    else if (d1 > d2)
                                    {
                                        lines.Add(new List<Point> {edge3Center, edge1Center});
                                        lines.Add(new List<Point> {edge1Center, edge2Center});
                                    }
                                    else
                                    {
                                        lines.Add(new List<Point> {edge1Center, edge2Center});
                                        lines.Add(new List<Point> {edge2Center, edge3Center});
                                    }
                                }
                                else
                                {
                                    Point newPoint;
                                    //Create a new center point on the shortest line and set three point sets
                                    if (d0 < d1 && d0 < d2)
                                    {
                                        newPoint = new Point(
                                            edge1Center.Position.add(edge2Center.Position).divide(2, 2));
                                    }
                                    else if (d1 < d2)
                                    {
                                        newPoint = new Point(
                                            edge2Center.Position.add(edge3Center.Position).divide(2, 2));
                                    }
                                    else
                                    {
                                        newPoint = new Point(
                                            edge3Center.Position.add(edge1Center.Position).divide(2, 2));
                                    }

                                    lines.Add(new List<Point> {edge1Center, newPoint});
                                    lines.Add(new List<Point> {edge2Center, newPoint});
                                    lines.Add(new List<Point> {edge3Center, newPoint});
                                }
                            }
                            else
                            {
                                lines.Add(triangleCenterLineVertices);
                            }
                        }
                    }

                    //Merge all the points 
                    for (var j = 0; j < lines.Count - 1; j++)
                    {
                        var l1 = lines[j];
                        var sameLineInt = -1;
                        for (var k = j + 1 ; k < lines.Count; k++)
                        {
                            var sameLineCount = 0;
                            var l2 = lines[k];
                            if (l1[0].Position.subtract(l2[0].Position).norm2().IsNegligible(0.0001))
                            {
                                l2[0] = l1[0];
                                sameLineCount++;
                            }
                            else if (l1[0].Position.subtract(l2[1].Position).norm2().IsNegligible(0.0001))
                            {
                                l2[1] = l1[0];
                                sameLineCount++;
                            }
                            if (l1[1].Position.subtract(l2[0].Position).norm2().IsNegligible(0.0001))
                            {
                                l2[0] = l1[1];
                                sameLineCount++;
                            }
                            else if (l1[1].Position.subtract(l2[1].Position).norm2().IsNegligible(0.0001))
                            {
                                l2[1] = l1[1];
                                sameLineCount++;
                            }
                            if (sameLineCount == 2)
                            {
                                sameLineInt = k;
                            }
                        }
                        if (sameLineInt != -1)
                        {
                            //lines.RemoveAt(sameLineInt);
                        }
                    }

                    //Get all the points
                    var points = new HashSet<Point>();
                    foreach (var line in lines)
                    {
                        points.Add(line[0]);
                        points.Add(line[1]);
                    }

                    //Get all the node points
                    //Also create a new branch for each line that attached to this node
                    var nodes = new HashSet<Point>();
                    var branches = new List<List<Point>>();
                    foreach (var p1 in points)
                    {
                        var adjacentLineCount = 0;
                        var adjacentLinesOtherPoints = new List<Point>();
                        foreach (var line in lines)
                        {
                            for (var p = 0; p < 2; p++)
                            {
                                var p2 = line[p];
                                if (p1 != p2) continue;
                                adjacentLineCount++;
                                if (p == 0) adjacentLinesOtherPoints.Add(line[1]);
                                else adjacentLinesOtherPoints.Add(line[0]);
                            }
                        }
                        if (adjacentLineCount > 2)
                        {
                            nodes.Add(p1);
                            //Add a new branch for each adjacent line
                            foreach (var p2 in adjacentLinesOtherPoints)
                            {
                                branches.Add(new List<Point> { p1, p2 });
                            }
                        }
                    }

                    while (branches.Any())
                    {
                        //Pop off the first branch
                        var branch = branches.First();
                        branches.RemoveAt(0);

                        //Continue adding points to this branch until it reaches another node
                        //Or the branch reaches its end
                        var hitEndOfBranch = false;
                        var hitNode = false;
                        while (!hitEndOfBranch && !hitNode)
                        {
                            //Search until we find the next line (2-points) that attaches to this branch
                            //Once it is added, remove it from the list of lines to make future searching faster
                            var p0 = branch.First();
                            var p1 = branch.Last();

                            //Check if we reached a node
                            //If we reached a node, then we need to remove a branch from that node
                            foreach (var node in nodes)
                            {
                                if (p1 != node || branch[0] == node) continue;
                                hitNode = true;
                                //Get the branch that has this node and the prior point
                                var p2 = branch[branch.Count - 2];
                                for (var j = 0; j < branches.Count; j++)
                                {
                                    if (branches[j][0].Position.subtract(p1.Position).norm2().IsNegligible(0.0001) &&
                                        branches[j][1].Position.subtract(p2.Position).norm2().IsNegligible(0.0001))
                                    {
                                        branches.RemoveAt(j);
                                        break;
                                    }
                                }
                                break;
                            }
                            if (hitNode) continue;

                            hitEndOfBranch = true;
                            for (var j = 0; j < lines.Count; j++)
                            {
                                var line = lines[j];
                                if (line[0] == p1) //.Position.subtract(p1.Position).norm2().IsNegligible(0.0001))
                                {
                                    if (line[1] == p0)
                                    {
                                        //This line is the starting line for the branch. 
                                        continue;
                                    }
                                    branch.Add(line[1]);
                                    hitEndOfBranch = false;
                                    lines.RemoveAt(j);
                                    break;
                                }
                                if (line[1] == p1) // .Position.subtract(p1.Position).norm2().IsNegligible(0.0001))
                                {
                                    if (line[0] == p0)
                                    {
                                        //This line is the starting line for the branch. 
                                        continue;
                                    }
                                    branch.Add(line[0]);
                                    hitEndOfBranch = false;
                                    lines.RemoveAt(j);
                                    break;
                                }
                            }
                        }

                        //if(hitEndOfBranch && branch.Count < 4)
                        allTriangles.Add(branch.Select(p => p.Light).ToList());
                        //Presenter.ShowAndHang(allTriangles, "", Plot2DType.Line, false);
                        //allTriangles.Add(nodes.Select(p => p.Light).ToList());
                    }
                    //Note: some lines are never removed (if they are the first line in a branch)
                    Presenter.ShowAndHang(allTriangles, "", Plot2DType.Line, false);
                }
            }
            Console.WriteLine("Completed.");
        }

        public static List<Point> SimplifyStraights(List<Point> points)
        {

            return null;
        }

        public static List<PointLight> GetMedialAxisPoints(PolygonLight polygon)
        {
            var medialAxisPoints = new List<PointLight>();
            var circles = new List<Circle>();

            //Start with large radius
            var startRadius = polygon.Length / 2;

            for (var j = 0; j < polygon.Path.Count; j++)
            {
                //Define the start circle
                var i = j - 1;
                if (j == 0) i = polygon.Path.Count - 1;
                var k = j + 1;
                if (j == polygon.Path.Count - 1) k = 0;
                var startPoint = polygon.Path[j];

                //Get angle between the three points
                var interiorAngle =
                    MiscFunctions.InteriorAngleBetweenEdgesInCCWList(polygon.Path[i], startPoint, polygon.Path[k]);
                var sv1 = startPoint.Position.subtract(polygon.Path[i].Position, 2).normalize(2);
                var sv2 = polygon.Path[k].Position.subtract(startPoint.Position, 2).normalize(2);

                double[] radiusVector;
                var ik = polygon.Path[k].Position.add(polygon.Path[i].Position, 2).divide(2, 2);
                if (interiorAngle.IsPracticallySame(Math.PI, TVGL.Constants.SameFaceNormalDotTolerance))
                {
                    var temp = polygon.Path[k].Position.subtract(polygon.Path[i].Position, 2);
                    //(y, -x) for clockwise rotation
                    radiusVector = new[] { -temp[1], temp[0] }.normalize(2);
                }
                //If angle > 180
                else if (interiorAngle > Math.PI)
                {
                    radiusVector = sv1.subtract(sv2).normalize(2);
                }
                else
                {
                    radiusVector = sv2.subtract(sv1).normalize(2);

                }
                var startCenter = new PointLight(startPoint.Position.add(radiusVector.multiply(startRadius), 2));
                var circle = new Circle(startCenter, startRadius);

                var pointsInCircle = new HashSet<PointLight>(polygon.Path);
                pointsInCircle.Remove(startPoint);
                var finalPoint = new PointLight();
                var priorArea = Math.PI * startRadius * startRadius;
                var deltaArea = priorArea;
                while (pointsInCircle.Any() && !deltaArea.IsNegligible(0.1))
                {
                    //Reset
                    pointsInCircle = new HashSet<PointLight>(polygon.Path);
                    pointsInCircle.Remove(startPoint);

                    var minDistance = circle.Radius;
                    //Find the closes point to the center. This point will be part of the definition of the new circle
                    for (var p = 0; p < polygon.Path.Count; p++)
                    {
                        if (p == j) continue;
                        var point = polygon.Path[p];
                        //If the distance between point and center is greater than radius, it is outside the circle
                        var d = circle.Center.Position.subtract(point.Position, 2).norm2();
                        var dif = d - minDistance;
                        if (dif.IsLessThanNonNegligible(0.01))
                        {
                            minDistance = d;
                            finalPoint = point;
                        }
                        else 
                        {
                            pointsInCircle.Remove(point);
                        }
                    }
                    // throw new Exception("Error in GetMedialAxisPoints");

                    if (finalPoint.Position == null)
                    {
                        Presenter.ShowAndHang(new List<List<PointLight>>{MiscFunctions.CreateCirclePath(new Point(circle.Center), circle.Radius)
                                 .Select(p => new PointLight(p)).ToList(),polygon.Path});
                        radiusVector = radiusVector.multiply(-1);
                        startCenter = new PointLight(startPoint.Position.add(radiusVector.multiply(startRadius), 2));
                        circle = new Circle(startCenter, startRadius);
                        Presenter.ShowAndHang(new List<List<PointLight>>
                        {
                            MiscFunctions.CreateCirclePath(new Point(circle.Center), circle.Radius)
                                .Select(p => new PointLight(p)).ToList(),
                            polygon.Path
                        });
                    };
                    //Shrink the circle
                    var v1 = new[] { radiusVector[0], radiusVector[1], 0.0 }.normalize();
                    //Start by getting the median point between the final two points
                    var p3 = finalPoint.Position.add(startPoint.Position, 2).divide(2, 2);
                    //The center point of the circle will be perpendicular to p2-p1
                    var temp = finalPoint.Position.subtract(startPoint.Position, 2).normalize(2);
                    var v2 = new[] { -temp[1], temp[0], 0.0 };
                    var dot = v1.dotProduct(v2);
                    //if (dot < 0) v2 = v2.multiply(-1); //Make sure v2 points toward the middle of the circle
                    if (dot.IsPracticallySame(1.0, Constants.SameFaceNormalDotTolerance))
                    {
                        //These lines are parallel. 

                    }
                    MiscFunctions.SkewedLineIntersection(new[] { startPoint.X, startPoint.Y, 0.0 }, v1,
                        new[] { p3[0], p3[1], 0 }, v2, out var centerPosition);
                    var centerPoint = new PointLight(centerPosition);
                    var radius = centerPoint.Position.subtract(startPoint.Position).norm2();
                    var area = Math.PI * radius * radius;
                    deltaArea = priorArea - area;
                    if (deltaArea < 0 || deltaArea.IsNegligible(0.01)) break; //No, or even bad change. Stop
                    priorArea = area;

                    circle = new Circle(centerPoint, radius);
                    pointsInCircle.Remove(finalPoint);
                    //Presenter.ShowAndHang(new List<List<PointLight>>{MiscFunctions.CreateCirclePath(new Point(circle.Center), circle.Radius)
                    //    .Select(p => new PointLight(p)).ToList(),polygon.Path});
                }

                if (MiscFunctions.IsPointInsidePolygon(polygon, circle.Center))
                {
                    circles.Add(circle);
                    medialAxisPoints.Add(circle.Center);
                }
                //Presenter.ShowAndHang(new List<List<PointLight>>{MiscFunctions.CreateCirclePath(new Point(circle.Center), circle.Radius)
                //    .Select(p => new PointLight(p)).ToList(),polygon.Path, medialAxisPoints});
            }

            double GetRadius(PointLight p1, PointLight p2, PointLight c)
            {
                var v1 = p1.Position.subtract(p2.Position);
                var v2 = p1.Position.subtract(c.Position);
                //Cosine theta = the dot product of the 2 vectors divided by their magnitudes
                var cosTheta = v1.dotProduct(v2) / (v1.norm2() * v2.norm2());
                return v1.norm2() / (2 * cosTheta);
            }

            return medialAxisPoints;
        }

        public class Circle
        {
            public PointLight Center { get; set; }
            public double Radius { get; set; }

            public Circle(PointLight center, double radius)
            {
                Center = center;
                Radius = radius;
            }
        }

        public static void TestMachinability(TessellatedSolid ts, string _fileName)
        {
            var vs1 = new VoxelizedSolid(ts, VoxelDiscretization.Coarse, false);
            Presenter.ShowAndHang(vs1);
            //var vs1ts = vs1.ConvertToTessellatedSolid(color);
            //var savename = "voxelized_" + _fileName;
            //IO.Save(vs1ts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in X Positive...");
            var vs1xpos = vs1.DraftToNewSolid(VoxelDirections.XPositive);
            Presenter.ShowAndHang(vs1xpos);
            //var vs1xposts = vs1xpos.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1xpos_" + _fileName;
            //IO.Save(vs1xposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in X Negative...");
            var vs1xneg = vs1.DraftToNewSolid(VoxelDirections.XNegative);
            //PresenterShowAndHang(vs1xneg);
            //var vs1xnegts = vs1xneg.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1xneg_" + _fileName;
            //IO.Save(vs1xnegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Y Positive...");
            var vs1ypos = vs1.DraftToNewSolid(VoxelDirections.YPositive);
            //PresenterShowAndHang(vs1ypos);
            //var vs1yposts = vs1ypos.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1ypos_" + _fileName;
            //IO.Save(vs1yposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Y Negative...");
            var vs1yneg = vs1.DraftToNewSolid(VoxelDirections.YNegative);
            ////PresenterShowAndHang(vs1yneg);
            ////var vs1ynegts = vs1yneg.ConvertToTessellatedSolid(color);
            ////Console.WriteLine("Saving Solid...");
            ////savename = "vs1yneg_" + _fileName;
            ////IO.Save(vs1ynegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Z Positive...");
            var vs1zpos = vs1.DraftToNewSolid(VoxelDirections.ZPositive);
            ////PresenterShowAndHang(vs1zpos);
            ////var vs1zposts = vs1zpos.ConvertToTessellatedSolid(color);
            ////Console.WriteLine("Saving Solid...");
            ////savename = "vs1zpos_" + _fileName;
            ////IO.Save(vs1zposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Z Negative...");
            var vs1zneg = vs1.DraftToNewSolid(VoxelDirections.ZNegative);
            //PresenterShowAndHang(vs1zneg);
            //var vs1znegts = vs1zneg.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1zneg_" + _fileName;
            //IO.Save(vs1znegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Intersecting Drafted Solids...");
            var intersect = vs1xpos.IntersectToNewSolid(vs1xneg, vs1ypos, vs1zneg, vs1yneg, vs1zpos);
            //PresenterShowAndHang(intersect);
            //var intersectts = intersect.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "intersect_" + _fileName;
            //IO.Save(intersectts, savename, FileType.STL_ASCII);

            Console.WriteLine("Subtracting Original Voxelized Shape From Intersect...");
            var unmachinableVoxels = intersect.SubtractToNewSolid(vs1);
            Presenter.ShowAndHang(unmachinableVoxels);
            //var uvts = unmachinableVoxels.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "unmachinable_" + _fileName;
            //IO.Save(uvts, savename, FileType.STL_ASCII);

            Console.WriteLine("Totals for Original Voxel Shape: " + vs1.GetTotals[0] + "; " + vs1.GetTotals[1] + "; " + vs1.GetTotals[2] + "; " + vs1.GetTotals[3]);
            Console.WriteLine("Totals for X Positive Draft: " + vs1xpos.GetTotals[0] + "; " + vs1xpos.GetTotals[1] + "; " + vs1xpos.GetTotals[2] + "; " + vs1xpos.GetTotals[3]);
            Console.WriteLine("Totals for X Negative Draft: " + vs1xneg.GetTotals[0] + "; " + vs1xneg.GetTotals[1] + "; " + vs1xneg.GetTotals[2] + "; " + vs1xneg.GetTotals[3]);
            Console.WriteLine("Totals for Y Positive Draft: " + vs1ypos.GetTotals[0] + "; " + vs1ypos.GetTotals[1] + "; " + vs1ypos.GetTotals[2] + "; " + vs1ypos.GetTotals[3]);
            Console.WriteLine("Totals for Y Negative Draft: " + vs1yneg.GetTotals[0] + "; " + vs1yneg.GetTotals[1] + "; " + vs1yneg.GetTotals[2] + "; " + vs1yneg.GetTotals[3]);
            Console.WriteLine("Totals for Z Positive Draft: " + vs1zpos.GetTotals[0] + "; " + vs1zpos.GetTotals[1] + "; " + vs1zpos.GetTotals[2] + "; " + vs1zpos.GetTotals[3]);
            Console.WriteLine("Totals for Z Negative Draft: " + vs1zneg.GetTotals[0] + "; " + vs1zneg.GetTotals[1] + "; " + vs1zneg.GetTotals[2] + "; " + vs1zneg.GetTotals[3]);
            Console.WriteLine("Totals for Intersected Voxel Shape: " + intersect.GetTotals[0] + "; " + intersect.GetTotals[1] + "; " + intersect.GetTotals[2] + "; " + intersect.GetTotals[3]);
            Console.WriteLine("Totals for Unmachinable Voxels: " + unmachinableVoxels.GetTotals[0] + "; " + unmachinableVoxels.GetTotals[1] + "; " + unmachinableVoxels.GetTotals[2] + "; " + unmachinableVoxels.GetTotals[3]);

            //PresenterShowAndHang(vs1);
            //PresenterShowAndHang(vs1xpos);
            //PresenterShowAndHang(vs1xneg);
            //PresenterShowAndHang(vs1ypos);
            //PresenterShowAndHang(vs1yneg);
            //PresenterShowAndHang(vs1zpos);
            //PresenterShowAndHang(vs1zneg);
            //PresenterShowAndHang(intersect);
            //PresenterShowAndHang(unmachinableVoxels);
            unmachinableVoxels.SolidColor = new Color(KnownColors.DeepPink);
            unmachinableVoxels.SolidColor.A = 200;

            Presenter.ShowAndHang(new Solid[] { ts, unmachinableVoxels });

            //PresenterShowAndHang(new Solid[] { intersect });
            //var unmachinableVoxelsSolid = new Solid[] { unmachinableVoxels };
            //PresenterShowAndHang(unmachinableVoxelsSolid);

            //var originalTS = new Solid[] { ts };
        }

        public static void TestVoxelization(TessellatedSolid ts)
        {
            var stopWatch = new Stopwatch();
            ts.Transform(new double[,]
            {
                {1, 0, 0, -(ts.XMax + ts.XMin) / 2},
                {0, 1, 0, -(ts.YMax + ts.YMin) / 2},
                {0, 0, 1, -(ts.ZMax + ts.ZMin) / 2},
            });
            stopWatch.Restart();
            var vs1 = new VoxelizedSolid(ts, VoxelDiscretization.Coarse, true); //, bounds);

            stopWatch.Stop();
            Console.WriteLine("Coarse: tsvol:{0}\tvol:{1}\t#voxels:{2}\ttime{3}",
                ts.Volume, vs1.Volume, vs1.Count, stopWatch.Elapsed.TotalSeconds);
            stopWatch.Restart();
            Presenter.ShowAndHang(new Solid[] { ts, vs1 });
            // var vs2 = (VoxelizedSolid)vs1.Copy();
            //var vs2 = new VoxelizedSolid(ts2, VoxelDiscretization.Coarse, false, bounds);
            //vs1.Subtract(vs2);
            //PresenterShowAndHang(new Solid[] { vs1 });

            //var vsPos = vs1.DraftToNewSolid(VoxelDirections.XPositive);
            //PresenterShowAndHang(new Solid[] { vsPos });
            //var vsNeg = vs1.DraftToNewSolid(VoxelDirections.XNegative);
            //PresenterShowAndHang(new Solid[] { vsNeg });

            //var vsInt = vsNeg.IntersectToNewSolid(vsPos);

            //stopWatch.Stop();
            //Console.WriteLine("Intersection: tsvol:{0}\tvol:{1}\ttime:{2}",
            //    ts.Volume, vsInt.Volume, stopWatch.Elapsed.TotalSeconds);
            //PresenterShowAndHang(new Solid[] { vsInt });
        }

        public static void TestSegmentation(TessellatedSolid ts)
        {
            var obb = MinimumEnclosure.OrientedBoundingBox(ts);
            var startTime = DateTime.Now;

            var averageNumberOfSteps = 500;

            //Do the average # of slices slices for each direction on a box (l == w == h).
            //Else, weight the average # of slices based on the average obb distance
            var obbAverageLength = (obb.Dimensions[0] + obb.Dimensions[1] + obb.Dimensions[2]) / 3;
            //Set step size to an even increment over the entire length of the solid
            var stepSize = obbAverageLength / averageNumberOfSteps;

            foreach (var direction in obb.Directions)
            {
                Dictionary<int, double> stepDistances;
                Dictionary<int, double> sortedVertexDistanceLookup;
                var segments = DirectionalDecomposition.UniformDirectionalSegmentation(ts, direction,
                    stepSize, out stepDistances, out sortedVertexDistanceLookup);
                //foreach (var segment in segments)
                //{
                //    var vertexLists = segment.DisplaySetup(ts);
                //    Presenter.ShowVertexPathsWithSolid(vertexLists, new List<TessellatedSolid>() { ts });
                //}
            }

            // var segments = AreaDecomposition.UniformDirectionalSegmentation(ts, obb.Directions[2].multiply(-1), stepSize);
            var totalTime = DateTime.Now - startTime;
            Debug.WriteLine(totalTime.TotalMilliseconds + " Milliseconds");
            //CheckAllObjectTypes(ts, segments);
        }

        private static void CheckAllObjectTypes(TessellatedSolid ts, IEnumerable<DirectionalDecomposition.DirectionalSegment> segments)
        {
            var faces = new HashSet<PolygonalFace>(ts.Faces);
            var vertices = new HashSet<Vertex>(ts.Vertices);
            var edges = new HashSet<Edge>(ts.Edges);


            foreach (var face in faces)
            {
                face.Color = new Color(KnownColors.Gray);
            }

            foreach (var segment in segments)
            {
                foreach (var face in segment.ReferenceFaces)
                {
                    faces.Remove(face);
                }
                foreach (var edge in segment.ReferenceEdges)
                {
                    edges.Remove(edge);
                }
                foreach (var vertex in segment.ReferenceVertices)
                {
                    vertices.Remove(vertex);
                }
            }

            ts.HasUniformColor = false;
            //Turn the remaining faces red
            foreach (var face in faces)
            {
                face.Color = new Color(KnownColors.Red);
            }
            Presenter.ShowAndHang(ts);

            //Make sure that every face, edge, and vertex is accounted for
            //Assert.That(!edges.Any(), "edges missed");
            //Assert.That(!faces.Any(), "faces missed");
            //Assert.That(!vertices.Any(), "vertices missed");
        }

        public static void TestSilhouette(TessellatedSolid ts)
        {
            var silhouette = TVGL.Silhouette.Run(ts, new[] { 0.5, 0.0, 0.5 });
            Presenter.ShowAndHang(silhouette);
        }

        private static void TestOBB(string InputDir)
        {
            var di = new DirectoryInfo(InputDir);
            var fis = di.EnumerateFiles();
            var numVertices = new List<int>();
            var data = new List<double[]>();
            foreach (var fileInfo in fis)
            {
                try
                {
                    var ts = IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name);
                    foreach (var tessellatedSolid in ts)
                    {
                        List<double> times, volumes;
                        MinimumEnclosure.OrientedBoundingBox_Test(tessellatedSolid, out times, out volumes);//, out VolumeData2);
                        data.Add(new[] { tessellatedSolid.ConvexHull.Vertices.Count(), tessellatedSolid.Volume,
                            times[0], times[1],times[2], volumes[0],  volumes[1], volumes[2] });
                    }
                }
                catch { }
            }
            // TVGLTest.ExcelInterface.PlotEachSeriesSeperately(VolumeData1, "Edge", "Angle", "Volume");
            TVGLTest.ExcelInterface.CreateNewGraph(new[] { data }, "", "Methods", "Volume", new[] { "PCA", "ChanTan" });
        }

        private static void TestSimplify(TessellatedSolid ts)
        {
            ts.Simplify(.9);
            Debug.WriteLine("number of vertices = " + ts.NumberOfVertices);
            Debug.WriteLine("number of edges = " + ts.NumberOfEdges);
            Debug.WriteLine("number of faces = " + ts.NumberOfFaces);
            TVGL.Presenter.ShowAndHang(ts);
        }  
    }
}