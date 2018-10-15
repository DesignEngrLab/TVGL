using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Excel;
using OxyPlot;
using OxyPlot.Axes;
using PropertyTools.DataAnnotations;
using StarMathLib;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Voxelization;
using Constants = TVGL.Voxelization.Constants;

namespace TVGLPresenterDX
{
    internal class Program
    {
        static readonly Stopwatch stopwatch = new Stopwatch();

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
            //Difference2();
            var writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            DirectoryInfo dir;
            if (Directory.Exists("../../../../TestFiles"))
            {
                //x64
                dir = new DirectoryInfo("../../../../TestFiles");
            }
            else
            {
                //x86
                dir = new DirectoryInfo("../../../TestFiles");
            }
            var random = new Random();
            //var fileNames = dir.GetFiles("*").OrderBy(x => random.Next()).ToArray();
            //var fileNames = dir.GetFiles("*SquareSupportWithAdditionsForSegmentationTesting*").ToArray();
            //var fileNames = dir.GetFiles("*Mic_Holder_SW*").ToArray(); //causes error in extrusion
            //var fileNames = dir.GetFiles("*Candy*").ToArray(); //only one machining setup required
            //var fileNames = dir.GetFiles("*Table*").ToArray();
            //var fileNames = dir.GetFiles("*Casing*").ToArray(); //5 pareto points
            var fileNames = dir.GetFiles("*testblock2*").ToArray(); //oblique holes
            //Casing = 18
            //SquareSupport = 75
            for (var i = 0; i < fileNames.Count(); i ++)
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
                //Presenter.ShowAndHang(ts);
                //TestVoxelization(ts, filename);
                //TestSearch1(ts);
                //TestSearchAll(ts);
                //TestSearch5Axis(ts);
                //try
                //{
                //    SearchComparison(ts, filename);
                //}
                //catch
                //{
                //    continue;
                //}
                FindAlternateSearchDirections(ts, out List<double> sd);

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
            stopwatch.Start();
            var vs1 = new VoxelizedSolid(ts, 8);
            Console.WriteLine("done constructing, now ...");
            //Presenter.ShowAndHang(vs1,2);
            //var vs1ts = vs1.ConvertToTessellatedSolid(color);
            //var savename = "voxelized_" + _fileName;
            //IO.Save(vs1ts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in X Positive...");
            var vs1xpos = vs1.ExtrudeToNewSolid(VoxelDirections.XPositive);
            //Presenter.ShowAndHang(vs1xpos);
            //var vs1xposts = vs1xpos.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1xpos_" + _fileName;
            //IO.Save(vs1xposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in X Negative...");
            var vs1xneg = vs1.ExtrudeToNewSolid(VoxelDirections.XNegative);
            //Presenter.ShowAndHang(vs1xneg);
            //var vs1xnegts = vs1xneg.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1xneg_" + _fileName;
            //IO.Save(vs1xnegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Y Positive...");
            var vs1ypos = vs1.ExtrudeToNewSolid(VoxelDirections.YPositive);
            // Presenter.ShowAndHang(vs1ypos);
            //var vs1yposts = vs1ypos.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1ypos_" + _fileName;
            //IO.Save(vs1yposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Y Negative...");
            var vs1yneg = vs1.ExtrudeToNewSolid(VoxelDirections.YNegative);
            // Presenter.ShowAndHang(vs1yneg);
            ////var vs1ynegts = vs1yneg.ConvertToTessellatedSolid(color);
            ////Console.WriteLine("Saving Solid...");
            ////savename = "vs1yneg_" + _fileName;
            ////IO.Save(vs1ynegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Z Positive...");
            var vs1zpos = vs1.ExtrudeToNewSolid(VoxelDirections.ZPositive);
            // Presenter.ShowAndHang(vs1zpos);
            ////var vs1zposts = vs1zpos.ConvertToTessellatedSolid(color);
            ////Console.WriteLine("Saving Solid...");
            ////savename = "vs1zpos_" + _fileName;
            ////IO.Save(vs1zposts, savename, FileType.STL_ASCII);

            Console.WriteLine("Drafting Solid in Z Negative...");
            var vs1zneg = vs1.ExtrudeToNewSolid(VoxelDirections.ZNegative);
            //Presenter.ShowAndHang(vs1zneg);
            //var vs1znegts = vs1zneg.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "vs1zneg_" + _fileName;
            //IO.Save(vs1znegts, savename, FileType.STL_ASCII);

            Console.WriteLine("Intersecting Drafted Solids...");
            var intersect = vs1xpos.IntersectToNewSolid(vs1xneg, vs1ypos, vs1zneg, vs1yneg, vs1zpos);
            Presenter.ShowAndHang(intersect);
            //return;
            //var intersectts = intersect.ConvertToTessellatedSolid(color);
            //Console.WriteLine("Saving Solid...");
            //savename = "intersect_" + _fileName;
            //IO.Save(intersectts, savename, FileType.STL_ASCII);


            Console.WriteLine("Subtracting Original Voxelized Shape From Intersect...");
            var unmachinableVoxels = intersect.SubtractToNewSolid(vs1);
            // Presenter.ShowAndHang(unmachinableVoxels);
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
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed);
            if (unmachinableVoxels.Volume == 0)
                Console.WriteLine("no unmachineable sections!!\n\n");
            else
            {
                Presenter.ShowAndHang(unmachinableVoxels, ts);
                Presenter.ShowAndHang(unmachinableVoxels);
            }

            //PresenterShowAndHang(new Solid[] { intersect });
            //var unmachinableVoxelsSolid = new Solid[] { unmachinableVoxels };
            //PresenterShowAndHang(unmachinableVoxelsSolid);

            //var originalTS = new Solid[] { ts };
        }

        public static void SearchComparison(TessellatedSolid ts, string fn)
        {
            Console.WriteLine("Voxelizing and Extruding...");
            //Convert tesselated solid to voxelized solid
            var vs = new VoxelizedSolid(ts, 8);
            var unitdictionary = new Dictionary<string, string>()
            {
                {"unspecified", "mm" },
                { "millimeter", "mm" },
                {"micron", "um" },
                {"cemtimeter", "cm" },
                {"inch", "in" },
                {"foot", "ft" },
                {"meter", "m" }
            };
            var units = unitdictionary[vs.Units.ToString()];
            //Perform extrusions in all six directions
            var vd = new Dictionary<VoxelDirections, VoxelizedSolid>()
            {
                { VoxelDirections.XNegative, vs.ExtrudeToNewSolid(VoxelDirections.XNegative) },
                { VoxelDirections.XPositive, vs.ExtrudeToNewSolid(VoxelDirections.XPositive) },
                { VoxelDirections.YNegative, vs.ExtrudeToNewSolid(VoxelDirections.YNegative) },
                { VoxelDirections.YPositive, vs.ExtrudeToNewSolid(VoxelDirections.YPositive) },
                { VoxelDirections.ZNegative, vs.ExtrudeToNewSolid(VoxelDirections.ZNegative) },
                { VoxelDirections.ZPositive, vs.ExtrudeToNewSolid(VoxelDirections.ZPositive) }
            };
            Console.WriteLine("Searching for optimal manufacturing plans...\n");

            TestSearchAll(vs, vd, out TimeSpan elapsedAll, out Candidate AllCand, out List<Candidate> cands);
            Console.WriteLine("All Possible Setups\nRequired Setups: {0}\n{1}\n", AllCand, elapsedAll);
            var reqdstps = AllCand.RequiredSetups;

            var ind1 = fn.LastIndexOf('.');
            var ind2 = fn.LastIndexOf('\\');
            fn = fn.Remove(ind1).Remove(0, ind2 + 1);
            var userprof = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dpath = userprof + "\\MachinabilitySearch\\";
            Directory.CreateDirectory(dpath);
            var rootfn = dpath + fn + "_";

            var allSeries = new OxyPlot.Series.ScatterSeries { Title = "All Solutions",
                MarkerType = MarkerType.Circle, MarkerFill = OxyColors.Black, MarkerStroke = OxyColors.Black };
            var paretoSeries = new OxyPlot.Series.ScatterSeries { Title = "Pareto Front",
                MarkerType = MarkerType.Square, MarkerFill = OxyColors.Red, MarkerStroke = OxyColors.Black };
            var gSeries = new OxyPlot.Series.ScatterSeries { Title = "Greedy Search",
                MarkerType = MarkerType.Diamond, MarkerFill = OxyColors.Green, MarkerStroke = OxyColors.Black };
            var g2Series = new OxyPlot.Series.ScatterSeries { Title = "Modified Greedy",
                MarkerType = MarkerType.Triangle, MarkerFill = OxyColors.Blue, MarkerStroke = OxyColors.Black };

            var allPoints = new List<OxyPlot.Series.ScatterPoint>();
            var paretoPoints = new List<OxyPlot.Series.ScatterPoint>();
            var gPoints = new List<OxyPlot.Series.ScatterPoint>();
            var g2Points = new List<OxyPlot.Series.ScatterPoint>();

            var paretofront = new List<Candidate>();
            foreach (Candidate candidate in cands)
            {
                var pareto = true;
                foreach (Candidate compare in cands)
                {
                    if (candidate == compare) continue;
                    else if (((candidate.RequiredSetups == compare.RequiredSetups) && (candidate.Volume > compare.Volume)) ||
                             ((candidate > compare) && (Math.Abs(candidate.Volume - compare.Volume) < AllCand.Volume *0.001)))
                    {
                        pareto = false;
                        break;
                    }
                    else if ((candidate <= compare) || (candidate.Volume <= compare.Volume)) continue;
                    pareto = false;
                    break;
                }

                if (pareto)
                {
                    paretoPoints.Add(new OxyPlot.Series.ScatterPoint(candidate.Volume, candidate.RequiredSetups, 10));
                    paretofront.Add(candidate);
                }
                else
                {
                    allPoints.Add(new OxyPlot.Series.ScatterPoint(candidate.Volume, candidate.RequiredSetups, 8));
                }
            }

            TestSearchGreedy2(vs, vd, out TimeSpan elapsedGreedy2, out Candidate Greedy2, out List<Candidate> G2Cands);
            Console.WriteLine("Modified Greedy Search\nRequired Setups: {0}\n{1}\n", Greedy2, elapsedGreedy2);

            TestSearchGreedy(vs, vd, out TimeSpan elapsedGreedy, out Candidate Greedy, out List<Candidate> GCands);
            Console.WriteLine("Greedy Search\nRequired Setups: {0}\n{1}\n", Greedy, elapsedGreedy);

            //TestSearchBFS(vs, vd, out TimeSpan elapsedBFS, out Candidate BFS, out List<Candidate> BFSCands);
            //Console.WriteLine("Breadth-First-Search\nRequired Setups: {0}\n{1}\n", BFS, elapsedBFS);

            //TestSearchBest(vs, vd, out TimeSpan elapsedBest, out Candidate Best, out List<Candidate> BestCands);
            //Console.WriteLine("Best-First-Search\nRequired Setups: {0}\n{1}\n", Best, elapsedBest);

            //TestSearch5Axis(vs, vd, out TimeSpan elapsed5Axis, out Candidate Axis5);
            //Console.WriteLine("Searching all 5-Axis Combinations\nRequired Setups: {0}\n{1}", Axis5, elapsed5Axis);
            Console.WriteLine("\n");

            foreach (Candidate cd in G2Cands)
            {
                g2Points.Add(new OxyPlot.Series.ScatterPoint(cd.Volume, cd.RequiredSetups, 8));
            }
            foreach (Candidate cd in GCands)
            {
                gPoints.Add(new OxyPlot.Series.ScatterPoint(cd.Volume, cd.RequiredSetups, 8));
            }

            var g2p = false;
            var gp = false;
            foreach (Candidate pc in paretofront)
            {
                if ((pc.RequiredSetups == 1) || (pc.RequiredSetups == 6)) continue;
                if ((Greedy2.RequiredSetups == pc.RequiredSetups) &&
                    (Math.Abs(Greedy2.Volume - pc.Volume) < pc.Volume * 0.001))
                {
                    g2p = true;
                }
                if ((Greedy.RequiredSetups == pc.RequiredSetups) &&
                    (Math.Abs(Greedy.Volume - pc.Volume) < pc.Volume * 0.001))
                {
                    gp = true;
                }
            }

            var truedictionary = new Dictionary<Boolean, string>() { { false, "No" }, { true, "Yes" } };
            var AllPlot = new PlotModel
            {
                Title = "Pareto: " + fn + "\nRequired Setups: " + reqdstps.ToString() +
                                                          "\nDoes Greedy search reach Pareto Front: " + truedictionary[gp] +
                                                          "\nDoes modified Greedy search reach Parteo Front: " +
                                                          truedictionary[g2p]
            };

            AllPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Number of Machining setups",
                MajorGridlineStyle = LineStyle.Solid, MajorStep = 1, MinorStep = 1});
            AllPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Part volume post-machining [" + units + "^{3}]",
                MajorGridlineStyle = LineStyle.Solid });

            allSeries.Points.AddRange(allPoints);
            paretoSeries.Points.AddRange(paretoPoints);
            gSeries.Points.AddRange(gPoints);
            g2Series.Points.AddRange(g2Points);

            AllPlot.Series.Add(allSeries);
            AllPlot.Series.Add(paretoSeries);
            AllPlot.Series.Add(gSeries);
            AllPlot.Series.Add(g2Series);


            var Allfn = rootfn + "pareto.png";
            using (var stream = File.Create(Allfn))
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter { Width = 1050, Height = 800, Background = OxyColors.White };
                pngExporter.Export(AllPlot, stream);
            }
        }

        public class Candidate : IEquatable<Candidate>
        {
            public double Volume { get; }
            public List<VoxelDirections> ManufacturingPlan { get; }
            public int RequiredSetups { get { return ManufacturingPlan.Count; } }
            public Candidate(Dictionary<VoxelDirections, VoxelizedSolid> ex, params VoxelDirections[] vd)
            {
                ManufacturingPlan = vd.ToList();
                var vs = ex[ManufacturingPlan[0]];
                var vs1 = new List<VoxelizedSolid>();
                for (int i = 1; i < ManufacturingPlan.Count; i++)
                {
                    vs1.Add(ex[ManufacturingPlan[i]]);
                }
                Volume = vs.IntersectToNewSolid(vs1.ToArray()).Volume;
            }
            public Candidate(Candidate cd, Dictionary<VoxelDirections, VoxelizedSolid> ex, params VoxelDirections[] vd)
            {
                ManufacturingPlan = new List<VoxelDirections>();
                ManufacturingPlan.AddRange(cd.ManufacturingPlan);
                ManufacturingPlan.AddRange(vd.ToList());
                var vs = ex[ManufacturingPlan[0]];
                var vs1 = new List<VoxelizedSolid>();
                for (int i = 1; i < ManufacturingPlan.Count; i++)
                {
                    vs1.Add(ex[ManufacturingPlan[i]]);
                }
                Volume = vs.IntersectToNewSolid(vs1.ToArray()).Volume;
            }
            public override string ToString()
            {
                var vxd = new Dictionary<VoxelDirections, string>()
                    {
                        {VoxelDirections.XNegative, "-x" },
                        {VoxelDirections.XPositive, "+x" },
                        {VoxelDirections.YNegative, "-y" },
                        {VoxelDirections.YPositive, "+y" },
                        {VoxelDirections.ZNegative, "-z" },
                        {VoxelDirections.ZPositive, "+z" }
                    };
                string tostring = vxd[ManufacturingPlan[0]];
                for (int i = 1; i < ManufacturingPlan.Count; i++)
                {
                    tostring = tostring + ", " + vxd[ManufacturingPlan[i]];
                }
                return tostring;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Candidate);
            }

            public bool Equals(Candidate cd)
            {
                return (this == cd);
            }

            public static bool operator ==(Candidate lhs, Candidate rhs)
            {
                if (lhs.RequiredSetups != rhs.RequiredSetups) return false;
                else if (lhs.ManufacturingPlan.Intersect(rhs.ManufacturingPlan).Count() == lhs.RequiredSetups) return true;
                else return false;
            }

            public static bool operator !=(Candidate lhs, Candidate rhs)
            {
                if (lhs.RequiredSetups != rhs.RequiredSetups) return true;
                else if (lhs.ManufacturingPlan.Intersect(rhs.ManufacturingPlan).Count() == lhs.RequiredSetups) return false;
                else return true;
            }

            public static bool operator >(Candidate lhs, Candidate rhs)
            {
                if (lhs.RequiredSetups > rhs.RequiredSetups) return true;
                else return false;
            }
            public static bool operator <(Candidate lhs, Candidate rhs)
            {
                if (lhs.RequiredSetups < rhs.RequiredSetups) return true;
                else return false;
            }
            public static bool operator >=(Candidate lhs, Candidate rhs)
            {
                if (lhs.RequiredSetups >= rhs.RequiredSetups) return true;
                else return false;
            }
            public static bool operator <=(Candidate lhs, Candidate rhs)
            {
                if (lhs.RequiredSetups <= rhs.RequiredSetups) return true;
                else return false;
            }
        }
        public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
        {
            public int Compare(TKey x, TKey y)
            {
                var result = x.CompareTo(y);
                return result == 0 ? 1 : result;
            }
        }

        public static bool SimilarAxis(Cylinder ps1, Cylinder ps2)
        {
            var similar = false;
            if ((Math.Abs(ps1.Axis[0] - ps2.Axis[0]) < 0.02) &&
                (Math.Abs(ps1.Axis[1] - ps2.Axis[1]) < 0.02) &&
                (Math.Abs(ps1.Axis[2] - ps2.Axis[2]) < 0.02))
            {
                similar = true;
            }
            else if ((Math.Abs(ps1.Axis[0] + ps2.Axis[0]) < 0.02) &&
                     (Math.Abs(ps1.Axis[1] + ps2.Axis[1]) < 0.02) &&
                     (Math.Abs(ps1.Axis[2] + ps2.Axis[2]) < 0.02))
            {
                similar = true;
                ps2.Axis[0] *= -1;
                ps2.Axis[1] *= -1;
                ps2.Axis[2] *= -1;
            }
            return similar;
        }

        public static void FindAlternateSearchDirections(TessellatedSolid ts, out List<double> sd)
        {
            sd = new List<double>();
            var maxbb = new List<double>(new double[]
            {
                ts.XMax - ts.XMin,
                ts.YMax - ts.XMin,
                ts.ZMax - ts.ZMin
            }).Max();
            var primitives = PrimitiveClassification.ClassifyPrimitiveSurfaces(ts);
            var primcyl = new List<PrimitiveSurface>();
            foreach (PrimitiveSurface ps in primitives)
            {
                if (ps.Type != PrimitiveSurfaceType.Cylinder) continue;
                var cyl = ps as Cylinder;
                if ((Math.Abs(Math.Abs(cyl.Axis.Sum()) - 1) > 0.1) && (cyl.Radius > maxbb * 0.02))
                {
                    primcyl.Add(cyl);
                }
            }

            var indices = new List<HashSet<int>>();
            foreach (Cylinder cyl in primcyl)
            {
                foreach (Cylinder cyl1 in primcyl)
                {
                    if (cyl == cyl1) continue;
                    if (SimilarAxis(cyl, cyl1))
                    {
                        if (indices.Count == 0) indices.Add(new HashSet<int>(new int[] {primcyl.IndexOf(cyl)}));
                        else
                        {
                            var newdir = true;
                            foreach (HashSet<int> dir in indices)
                            {
                                var cyl2 = primcyl[dir.First()] as Cylinder;
                                if (SimilarAxis(cyl, cyl2))
                                {
                                    dir.Add(primcyl.IndexOf(cyl));
                                    newdir = false;
                                }
                            }

                            if (newdir) indices.Add(new HashSet<int>(new int[] { primcyl.IndexOf(cyl) }));
                        }
                    }
                }
            }

            var dirs = new List<double[]>();
            var i = 0;
            foreach (HashSet<int> hindex in indices)
            {
                dirs.Add(new double[3]);
                foreach (int index in hindex)
                {
                    var cyl = primcyl[index] as Cylinder;
                    dirs[i][0] += cyl.Axis[0] / hindex.Count;
                    dirs[i][1] += cyl.Axis[1] / hindex.Count;
                    dirs[i][2] += cyl.Axis[2] / hindex.Count;
                }

                var j = 0;
                foreach (double dir in dirs[i])
                {
                    if (Math.Abs(dir) < 0.01) dirs[i][j] = 0;
                    j++;
                }

                i++;
            }

            foreach (double[] dir in dirs)
            {
                dir.normalizeInPlace();
            }

            Presenter.ShowAndHang(ts);
        }

        public static void TestSearchGreedy(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
            out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var complete = new Candidate(vd, vd.Keys.ToArray());
            var targetVolume = complete.Volume;
            var tol = targetVolume * 0.001;
            var candidates = new List<List<Candidate>>
            {
                new List<Candidate>(new Candidate[]
                {
                    new Candidate(vd, VoxelDirections.XNegative),
                    new Candidate(vd, VoxelDirections.YNegative),
                    new Candidate(vd, VoxelDirections.ZNegative)
                })
            };
            candidates[0].Sort((x, y) => x.Volume.CompareTo(y.Volume));

            int i = 0;
            while (Math.Abs(candidates[i][0].Volume - targetVolume) > tol)
            {
                if (i == 5)
                {
                    candidates.Add(new List<Candidate>(new Candidate[] { complete }));
                    break;
                }
                candidates.Add(new List<Candidate>());
                foreach (KeyValuePair<VoxelDirections, VoxelizedSolid> vdvs in vd)
                {
                    if (!candidates[i][0].ManufacturingPlan.Contains(vdvs.Key))
                    {
                        candidates[i+1].Add(new Candidate(candidates[i][0], vd, vdvs.Key));
                    }
                }
                candidates[i+1].Sort((x, y) => x.Volume.CompareTo(y.Volume));
                i++;
            }

            cd = candidates[i][0];
            cds = new List<Candidate>();
            foreach (List<Candidate> c1 in candidates)
            {
                cds.AddRange(c1);
            }

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
        }

        public static void TestSearchGreedy2(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
            out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var complete = new Candidate(vd, vd.Keys.ToArray());
            var targetVolume = complete.Volume;
            var tol = targetVolume * 0.001;
            var candidates = new List<List<Candidate>>
            {
                new List<Candidate>(new Candidate[]
                {
                    new Candidate(vd, VoxelDirections.XNegative, VoxelDirections.XPositive),
                    new Candidate(vd, VoxelDirections.YNegative, VoxelDirections.YPositive),
                    new Candidate(vd, VoxelDirections.ZNegative, VoxelDirections.ZPositive)
                })
            };
            candidates[0].Sort((x, y) => x.Volume.CompareTo(y.Volume));

            int i = 0;
            while (Math.Abs(candidates[i][0].Volume - targetVolume) > tol)
            {
                if (i < 2)
                {
                    candidates.Add(new List<Candidate>(new Candidate[]
                        {
                            new Candidate(candidates[i][0], vd, candidates[0][i+1].ManufacturingPlan[0]),
                            new Candidate(candidates[i][0], vd, candidates[0][i+1].ManufacturingPlan[1])
                        }));
                    candidates[i + 1].Sort((x, y) => x.Volume.CompareTo(y.Volume));
                }
                else if (i == 2)
                {
                    candidates.Add(new List<Candidate>(new Candidate[]
                        {
                            new Candidate(candidates[i][0], vd, candidates[1][1].ManufacturingPlan[
                                candidates[1][1].ManufacturingPlan.Count-1]),
                            new Candidate(candidates[i][0], vd, candidates[2][1].ManufacturingPlan[
                                candidates[2][1].ManufacturingPlan.Count-1])
                        }));
                    candidates[i + 1].Sort((x, y) => x.Volume.CompareTo(y.Volume));
                }
                else if (i == 3)
                {
                    candidates.Add(new List<Candidate>(new Candidate[] {complete}));
                }
                else break;
                i++;
            }

            cd = candidates[i][0];
            cds = new List<Candidate>();
            foreach (List<Candidate> c1 in candidates)
            {
                cds.AddRange(c1);
            }

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
        }

        public static void TestSearchBFS(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
            out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var complete = new Candidate(vd, vd.Keys.ToArray());
            var targetVolume = complete.Volume;
            var tol = targetVolume * 0.001;
            var candidates = new Queue<Candidate>();
            var noImprovement = new List<KeyValuePair<VoxelDirections, List<VoxelDirections>>>();

            foreach (VoxelDirections voxd in vd.Keys)
            {
                candidates.Enqueue(new Candidate(vd, voxd));
            }

            while (Math.Abs(candidates.Peek().Volume - targetVolume) > tol)
            {
                var qp = candidates.Dequeue();
                var dirs = vd.Keys.Except(qp.ManufacturingPlan).ToList();
                Parallel.ForEach(dirs.Cast<VoxelDirections>(), dir =>
                {
                    var skip = false;
                    foreach (KeyValuePair<VoxelDirections, List<VoxelDirections>> kvp in noImprovement)
                    {
                        var oldKeys = kvp.Value.ToList();
                        oldKeys.Add(kvp.Key);
                        var newKeys = qp.ManufacturingPlan.ToList();
                        newKeys.Add(dir);
                        if (oldKeys.Intersect(newKeys).Count() != oldKeys.Count) continue;
                        if ((dir == kvp.Key) && (kvp.Value.Intersect(qp.ManufacturingPlan).Count() == kvp.Value.Count))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (!skip)
                    {
                        var unique = true;
                        foreach (Candidate cnd in candidates)
                        {
                            var manplan = new List<VoxelDirections>(new VoxelDirections[] { dir });
                            manplan.AddRange(qp.ManufacturingPlan);
                            if (manplan.Intersect(cnd.ManufacturingPlan).Count() == manplan.Count)
                            {
                                unique = false;
                                break;
                            }
                        }
                        if (unique)
                        {
                            var cand = new Candidate(qp, vd, dir);
                            if (Math.Abs(qp.Volume - cand.Volume) > tol)
                            {
                                candidates.Enqueue(cand);
                            }
                            else
                            {
                                noImprovement.Add(new KeyValuePair<VoxelDirections, List<VoxelDirections>>
                                    (dir, qp.ManufacturingPlan.ToList()));
                            }
                        }
                    }
                });
            }

            cd = candidates.Peek();
            cds = candidates.ToList();

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
        }

        //Doesn't work very well
        public static double BFSCost(Candidate cd)
        {
            var cost = cd.Volume * Math.Pow(2 , (cd.RequiredSetups - 1));
            return cost;
        }

        public static void TestSearchBest(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
            out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var complete = new Candidate(vd, vd.Keys.ToArray());
            var targetVolume = complete.Volume;
            var tol = targetVolume * 0.001;
            var candidates = new SortedList<double, Candidate>(new DuplicateKeyComparer<double>());

            foreach (VoxelDirections voxd in vd.Keys)
            {
                var cand = new Candidate(vd, voxd);
                candidates.Add(BFSCost(cand), cand);
            }

            var setups1 = new List<VoxelDirections>();
            var volume1 = new double();

            while (Math.Abs(candidates.Values[0].Volume - targetVolume) > tol)
            {
                var cn = candidates.Values[0];
                if ((setups1 == cn.ManufacturingPlan) && (volume1 == cn.Volume))
                {
                    candidates.RemoveAt(0);
                    continue;
                }
                    
                var dirs = vd.Keys.Except(cn.ManufacturingPlan).ToList();
                foreach (VoxelDirections dir in dirs)
                {
                    var unique = true;
                    foreach (Candidate cnd in candidates.Values)
                    {
                        var i = 0;
                        var manplan = new List<VoxelDirections>(new VoxelDirections[] { dir });
                        manplan.AddRange(cn.ManufacturingPlan);
                        foreach (VoxelDirections mp in manplan)
                        {
                            if (cnd.ManufacturingPlan.Contains(mp)) i++;
                        }

                        if (i == manplan.Count)
                        {
                            unique = false;
                            break;
                        }
                    }
                    if (unique)
                    {
                        var cand = new Candidate(cn, vd, dir);
                        if (Math.Abs(cn.Volume - cand.Volume) > tol)
                        {
                            candidates.Add(BFSCost(cand), cand);
                        }
                    }

                }

                setups1 = cn.ManufacturingPlan.ToList();
                volume1 = cn.Volume;
            }

            cd = candidates.Values[0];
            cds = candidates.Values.ToList();

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
        }

        public static void TestSearchAll(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
            out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var directions = new List<VoxelDirections>(new VoxelDirections[]
            {
                VoxelDirections.XNegative,
                VoxelDirections.XPositive,
                VoxelDirections.YNegative,
                VoxelDirections.YPositive,
                VoxelDirections.ZNegative,
                VoxelDirections.ZPositive
            });
            
            var complete = new Candidate(vd, directions.ToArray());
            var targetVolume = complete.Volume;
            var tol = targetVolume * 0.001;

            //Intersect all non-repeating combinatinos of directions
            var combinations = new List<List<int>>(64);
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        for (int l = 0; l < 2; l++)
                        {
                            for (int m = 0; m < 2; m++)
                            {
                                for (int n = 0; n < 2; n++)
                                {
                                    var permutation = new List<int>(new int[] {i, j, k, l, m, n});
                                    combinations.Add(permutation);
                                }
                            }
                        }
                    }
                }
            }

            combinations.RemoveAt(63);
            combinations.RemoveAt(0);
            var intersections = new List<Candidate>(63) { complete };

            Parallel.ForEach(combinations.Cast<List<int>>(), combination =>
            {
                var indices = Enumerable.Range(0, combination.Count).Where(i => combination[i] == 1).ToList();
                var vds = new List<VoxelDirections>();
                foreach (int index in indices) { vds.Add(directions[index]); }
                intersections.Add(new Candidate(vd, vds.ToArray()));
            });

            intersections.Sort((x, y) => x.Volume.CompareTo(y.Volume));
            var bests = intersections.FindAll(delegate(Candidate inter) { return Math.Abs(inter.Volume - intersections[0].Volume) < tol; });
            bests.Sort((x, y) => x.RequiredSetups.CompareTo(y.RequiredSetups));
            cd = bests[0];
            cds = intersections;

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;

        }

        public static void TestSearch5Axis(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd, out TimeSpan elapsed, out Candidate cd)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var complete = new Candidate(vd, vd.Keys.ToArray());
            var targetVolume = complete.Volume;
            var tol = targetVolume * 0.001;

            var setups = new List<Candidate>(7)
            {
                complete,
                new Candidate(vd, VoxelDirections.XNegative, VoxelDirections.XPositive,
                VoxelDirections.YNegative, VoxelDirections.YPositive, VoxelDirections.ZPositive),
                new Candidate(vd, VoxelDirections.XNegative, VoxelDirections.XPositive,
                VoxelDirections.YNegative, VoxelDirections.ZNegative, VoxelDirections.ZPositive),
                new Candidate(vd, VoxelDirections.XNegative, VoxelDirections.XPositive,
                VoxelDirections.YPositive, VoxelDirections.ZNegative, VoxelDirections.ZPositive),
                new Candidate(vd, VoxelDirections.XNegative, VoxelDirections.YNegative,
                VoxelDirections.YPositive, VoxelDirections.ZNegative, VoxelDirections.ZPositive),
                new Candidate(vd, VoxelDirections.XPositive, VoxelDirections.YNegative,
                VoxelDirections.YPositive, VoxelDirections.ZNegative, VoxelDirections.ZPositive),
            };

            setups.Sort((x, y) => x.Volume.CompareTo(y.Volume));
            if (Math.Abs(setups[0].Volume - targetVolume) < tol)
            {
                cd = setups[0];
            }
            else
            {
                cd = complete;
            }

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
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
                    IO.Open(fileInfo.Open(FileMode.Open), fileInfo.Name, out TessellatedSolid tessellatedSolid);
                    List<double> times, volumes;
                    MinimumEnclosure.OrientedBoundingBox_Test(tessellatedSolid, out times, out volumes);//, out VolumeData2);
                    data.Add(new[] { tessellatedSolid.ConvexHull.Vertices.Count(), tessellatedSolid.Volume,
                            times[0], times[1],times[2], volumes[0],  volumes[1], volumes[2] });
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