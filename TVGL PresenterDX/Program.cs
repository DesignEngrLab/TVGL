using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Voxelization;


namespace TVGLPresenterDX
{
    internal class Program
    {
        private static readonly string[] FileNames =
        {
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
            "../../../TestFiles/drillparts.amf", //Edge/face relationship contains errors
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
            "../../../TestFiles/brace.stl", //Convex hull fails in MIconvexHull
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
            //Difference2();
            var writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            DirectoryInfo dir;
            try
            {
                //x64
                dir = new DirectoryInfo("../../../TestFiles");
            }
            catch
            {
                //x86
                dir = new DirectoryInfo("../../TestFiles");
            }
            var random = new Random();
            var fileNames = dir.GetFiles("*").OrderBy(x => random.Next()).ToArray();
            //Casing = 18
            //SquareSupport = 75
            for (var i = 0; i < fileNames.Count(); i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                TessellatedSolid ts;
                if (!File.Exists(filename)) continue;
                using (fileStream = File.OpenRead(filename))
                    IO.Open(fileStream, filename, out ts);
                if (ts.Errors != null) continue;
                Color color = new Color(KnownColors.AliceBlue);
                ts.SolidColor = new Color(KnownColors.MediumSeaGreen)
                {
                    Af = 0.25f
                };
                PresenterShowAndHang(ts);
                TestVoxelization(ts, filename);

                // var stopWatch = new Stopwatch();
                // Color color = new Color(KnownColors.AliceBlue);
                //ts[0].SetToOriginAndSquare(out var backTransform);
                //ts[0].Transform(new double[,]
                //  {
                //{1,0,0,-(ts[0].XMax + ts[0].XMin)/2},
                //{0,1,0,-(ts[0].YMax+ts[0].YMin)/2},
                //{0,0,1,-(ts[0].ZMax+ts[0].ZMin)/2},
                //  });
                // stopWatch.Restart();
                //PresenterShowAndHang(ts);
                // Console.WriteLine("Voxelizing Tesselated File " + filename);
                //  var vs1 = new VoxelizedSolid(ts[0], VoxelDiscretization.Coarse, false);//, bounds);
                // Presenter.ShowAndHang(vs1);
                //TestVoxelization(ts[0]);
                //bounds = vs1.Bounds;
            }

            Console.WriteLine("Completed.");
            Console.ReadKey();
        }

        public static void TestVoxelization(TessellatedSolid ts, string _fileName)
        {
            var vs1 = new VoxelizedSolid(ts, 10);
            Console.WriteLine("done constructing, now ...");
            PresenterShowAndHang(vs1);
            //var vs1ts = vs1.ConvertToTessellatedSolid(color);
            //var savename = "voxelized_" + _fileName;
            //IO.Save(vs1ts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in X Positive...");
            var vs1xpos = vs1.DraftToNewSolid(CartesianDirections.XPositive);
            //Presenter.ShowAndHang(vs1xpos);
            //var vs1xposts = vs1xpos.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1xpos_" + _fileName;
            //IO.Save(vs1xposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in X Negative...");
            var vs1xneg = vs1.DraftToNewSolid(CartesianDirections.XNegative);
            //Presenter.ShowAndHang(vs1xneg);
            //var vs1xnegts = vs1xneg.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1xneg_" + _fileName;
            //IO.Save(vs1xnegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Y Positive...");
            var vs1ypos = vs1.DraftToNewSolid(CartesianDirections.YPositive);
            //Presenter.ShowAndHang(vs1ypos);
            //var vs1yposts = vs1ypos.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1ypos_" + _fileName;
            //IO.Save(vs1yposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Y Negative...");
            var vs1yneg = vs1.DraftToNewSolid(CartesianDirections.YNegative);
            //Presenter.ShowAndHang(vs1yneg);
            ////var vs1ynegts = vs1yneg.ConvertToTessellatedSolid(color);
            ////Console.WriteLine("Saving Solid...");
            ////savename = "vs1yneg_" + _fileName;
            ////IO.Save(vs1ynegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Z Positive...");
            var vs1zpos = vs1.DraftToNewSolid(CartesianDirections.ZPositive);
            //Presenter.ShowAndHang(vs1zpos);
            ////var vs1zposts = vs1zpos.ConvertToTessellatedSolid(color);
            ////Console.WriteLine("Saving Solid...");
            ////savename = "vs1zpos_" + _fileName;
            ////IO.Save(vs1zposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Z Negative...");
            var vs1zneg = vs1.DraftToNewSolid(CartesianDirections.ZNegative);
            //Presenter.ShowAndHang(vs1zneg);
            //var vs1znegts = vs1zneg.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1zneg_" + _fileName;
            //IO.Save(vs1znegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Intersecting Drafted Solids...");
            var intersect = vs1xpos.IntersectToNewSolid(vs1xneg, vs1ypos, vs1zneg, vs1yneg, vs1zpos);
            //Presenter.ShowAndHang(intersect);
            //return;
            //var intersectts = intersect.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "intersect_" + _fileName;
            //IO.Save(intersectts, savename, FileType.STL_ASCII);

            Console.WriteLine("Subtracting Original Voxelized Shape From Intersect...");
            var unmachinableVoxels = intersect.SubtractToNewSolid(vs1);
            //Presenter.ShowAndHang(unmachinableVoxels);
            //var uvts = unmachinableVoxels.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "unmachinable_" + _fileName;
            //IO.Save(uvts, savename, FileType.STL_ASCII);

            //Console.WriteLine("Totals for Original Voxel Shape: " + vs1.GetTotals[0] + "; " + vs1.GetTotals[1] + "; " + vs1.GetTotals[2] + "; " + vs1.GetTotals[3]);
            //Console.WriteLine("Totals for X Positive Draft: " + vs1xpos.GetTotals[0] + "; " + vs1xpos.GetTotals[1] + "; " + vs1xpos.GetTotals[2] + "; " + vs1xpos.GetTotals[3]);
            //Console.WriteLine("Totals for X Negative Draft: " + vs1xneg.GetTotals[0] + "; " + vs1xneg.GetTotals[1] + "; " + vs1xneg.GetTotals[2] + "; " + vs1xneg.GetTotals[3]);
            //Console.WriteLine("Totals for Y Positive Draft: " + vs1ypos.GetTotals[0] + "; " + vs1ypos.GetTotals[1] + "; " + vs1ypos.GetTotals[2] + "; " + vs1ypos.GetTotals[3]);
            //Console.WriteLine("Totals for Y Negative Draft: " + vs1yneg.GetTotals[0] + "; " + vs1yneg.GetTotals[1] + "; " + vs1yneg.GetTotals[2] + "; " + vs1yneg.GetTotals[3]);
            //Console.WriteLine("Totals for Z Positive Draft: " + vs1zpos.GetTotals[0] + "; " + vs1zpos.GetTotals[1] + "; " + vs1zpos.GetTotals[2] + "; " + vs1zpos.GetTotals[3]);
            //Console.WriteLine("Totals for Z Negative Draft: " + vs1zneg.GetTotals[0] + "; " + vs1zneg.GetTotals[1] + "; " + vs1zneg.GetTotals[2] + "; " + vs1zneg.GetTotals[3]);
            //Console.WriteLine("Totals for Intersected Voxel Shape: " + intersect.GetTotals[0] + "; " + intersect.GetTotals[1] + "; " + intersect.GetTotals[2] + "; " + intersect.GetTotals[3]);
            //Console.WriteLine("Totals for Unmachinable Voxels: " + unmachinableVoxels.GetTotals[0] + "; " + unmachinableVoxels.GetTotals[1] + "; " + unmachinableVoxels.GetTotals[2] + "; " + unmachinableVoxels.GetTotals[3]);
            Console.WriteLine("orig volume = {0}, intersect vol = {1}, and subtract vol = {2}", vs1.Volume, intersect.Volume, unmachinableVoxels.Volume);
            //PresenterShowAndHang(vs1);
            //PresenterShowAndHang(vs1xpos);
            //PresenterShowAndHang(vs1xneg);
            //PresenterShowAndHang(vs1ypos);
            //PresenterShowAndHang(vs1yneg);
            //PresenterShowAndHang(vs1zpos);
            //PresenterShowAndHang(vs1zneg);
            //PresenterShowAndHang(intersect);
            //PresenterShowAndHang(unmachinableVoxels);
            //unmachinableVoxels.SolidColor = new Color(KnownColors.DeepPink);
            //unmachinableVoxels.SolidColor.A = 200;
            if (unmachinableVoxels.Volume == 0)
                Console.WriteLine("no unmachineable sections!!\n\n");
            else
            {
                PresenterShowAndHang(unmachinableVoxels, ts);
                PresenterShowAndHang(unmachinableVoxels);
            }

            //PresenterShowAndHang(new Solid[] { intersect });
            //var unmachinableVoxelsSolid = new Solid[] { unmachinableVoxels };
            //PresenterShowAndHang(unmachinableVoxelsSolid);

            //var originalTS = new Solid[] { ts };
        }


        private static void PresenterShowAndHang(params Solid[] solids)
        {
            PresenterShowAndHang(solids.ToList());
        }

        private static void PresenterShowAndHang(IList<Solid> solids)
        {
            var mainWindow = new MainWindow();
            mainWindow.AddSolids(solids);
            mainWindow.view1.ZoomExtents();
            mainWindow.ShowDialog();
        }

        private static List<int[]> findIntersectingVoxelCoords(double[] startPoint, double[] endPoint)
        {
            var vectorNorm = endPoint.subtract(startPoint).normalize();
            var intersections = new List<double[]>();
            for (var dim = 0; dim < 3; dim++)
            {
                var start = (int)Math.Floor(startPoint[dim]);
                var end = (int)Math.Floor(endPoint[dim]);
                var forwardX = end > start;
                var uDim = (dim + 1) % 3;
                var vDim = (dim + 2) % 3;
                var t = start;
                while (t != end)
                {
                    if (forwardX) t++;
                    var d = (t - startPoint[dim]) / vectorNorm[dim];
                    var intersection = new double[3];
                    intersection[dim] = t;
                    intersection[uDim] = startPoint[uDim] + d * vectorNorm[uDim];
                    intersection[vDim] = startPoint[vDim] + d * vectorNorm[vDim];
                    intersections.Add(intersection);
                    //If going reverse, do not decriment until after using this voxel index.
                    if (!forwardX) t--;
                }
            }

            return expandIntersections(intersections);
        }

        /// <summary>
        /// Is the double currently at an integer value?
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        private static bool atIntegerValue(double d)
        {
            return Math.Ceiling(d) == d;
        }

        private static List<int[]> expandIntersections(List<double[]> intersections)
        {
            var voxelCoords = new List<int[]>();
            foreach (var intersection in intersections)
            {
                //Convert the intersection values to integers. 
                var ijk = new[] { (int)intersection[0], (int)intersection[1], (int)intersection[2] };
                var dimensionsAsIntegers = intersection.Select(atIntegerValue).ToList();
                var numAsInt = dimensionsAsIntegers.Count(c => c); //Counts number of trues

                //If one/ three dimensions lands on an integer, the edge goes through a voxel face.
                //If two/ three, a voxel edge. If three/ three, a corner. 

                //In any case that it goes through a face, there must be a voxel located on both sides of this face.
                //This is captured by the intersection conversion to bytes and the decrement along the dimension 
                //with the integer. 

                //If two/ three x,y,z values of the intersection are integers, this can be represented by drawing a 
                //2D and ignoring the non-integer dimension.The intersection of interest is when the line goes intersects 
                //the two axis(box corner). If you apply the decrement rule above, there are no real issues until you 
                //try a negative slope line that intersects multiple box corners.Not only is there significant 
                //inconsistency with the positive slope version, but it downright misses all the voxels with a line 
                //through them.I am sure this same issue applies to lines through multiple voxel corners or a mix of 
                //voxel corners and lines.

                //The simplest and most robust solution I can think of is to add voxels at all the decemented integer 
                //intersections. For voxel edge intersections, this forms 4 voxels around the intersection. For voxel
                //corner intersections, this forms 8 voxels around the intersection. This can be expressed as:
                //numVoxels = 2^numAsInt
                var numVoxels = 0;
                var allCombinations = new List<int[]>()
                {
                    new[] {0, 0, 0},
                    new[] {-1, 0, 0},
                    new[] {0, -1, 0},
                    new[] {0, 0, -1},
                    new[] {-1, -1, 0},
                    new[] {-1, 0, -1},
                    new[] {0, -1, -1},
                    new[] {-1, -1, -1},
                };
                foreach (var combination in allCombinations)
                {
                    var valid = true;
                    for (var j = 0; j < 3; j++)
                    {
                        if (dimensionsAsIntegers[j]) continue;
                        if (combination[j] == 0) continue;
                        //If not an integer and not 0, then do not add it to the list
                        valid = false;
                        break;
                    }

                    if (!valid) continue;
                    //This is a valid combination, so make it a voxel
                    var newIjk = new[] { ijk[0] + combination[0], ijk[1] + combination[1], ijk[2] + combination[2] };
                    voxelCoords.Add(newIjk);
                    numVoxels++;
                }

                if (numVoxels != (int)Math.Pow(2, numAsInt)) throw new Exception("Error in implementation");
            }

            return voxelCoords;
        }




    }
}