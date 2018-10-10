using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Office.Interop.Excel;
using OxyPlot;
using OxyPlot.Axes;
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
            var fileNames = dir.GetFiles("*").OrderBy(x => random.Next()).ToArray();
            //var fileNames = dir.GetFiles("*SquareSupportWithAdditionsForSegmentationTesting*").ToArray();
            //var fileNames = dir.GetFiles("*Mic_Holder_SW*").ToArray();
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
                try
                {
                    SearchComparison(ts, filename);
                }
                catch
                {
                    continue;
                }

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

            var ind1 = fn.LastIndexOf('.');
            var ind2 = fn.LastIndexOf('\\');
            fn = fn.Remove(ind1).Remove(0, ind2 + 1);
            var userprof = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dpath = userprof + "\\MachinabilitySearch\\";
            Directory.CreateDirectory(dpath);
            var rootfn = dpath + fn + "_";

            var allSeries = new OxyPlot.Series.ScatterSeries { Title = "All Solutions", MarkerType = MarkerType.Circle, MarkerFill = OxyColors.Black };
            var paretoSeries = new OxyPlot.Series.ScatterSeries { Title = "Pareto Front", MarkerType = MarkerType.Diamond, MarkerFill = OxyColors.Red };
            var gSeries = new OxyPlot.Series.ScatterSeries { Title = "Greedy Search", MarkerType = MarkerType.Square, MarkerFill = OxyColors.Green };
            var g2Series = new OxyPlot.Series.ScatterSeries { Title = "Modified Greedy", MarkerType = MarkerType.Triangle, MarkerFill = OxyColors.Blue };

            var allPoints = new List<OxyPlot.Series.ScatterPoint>();
            var paretoPoints = new List<OxyPlot.Series.ScatterPoint>();
            var gPoints = new List<OxyPlot.Series.ScatterPoint>();
            var g2Points = new List<OxyPlot.Series.ScatterPoint>();

            TestSearchAll(vs, vd, out TimeSpan elapsedAll, out Candidate AllCand, out List<Candidate> cands);
            Console.WriteLine("Searching all Possible Combinations of Setups\nRequired Setups: {0}\n{1}", AllCand, elapsedAll);
            var reqdstps = AllCand.RequiredSetups;

            var paretofront = new List<Candidate>();
            foreach (Candidate candidate in cands)
            {
                var pareto = true;
                foreach (Candidate candidate1 in cands)
                {
                    if ((candidate.RequiredSetups == candidate1.RequiredSetups) &&
                        (Math.Abs(candidate.Volume - candidate1.Volume) < AllCand.Volume * 0.001))
                    {
                        continue;
                    }
                    else if (((candidate.RequiredSetups > candidate1.RequiredSetups) &&
                         (candidate.Volume >= candidate1.Volume)) ||
                        ((candidate.RequiredSetups >= candidate1.RequiredSetups) &&
                         (candidate.Volume > candidate1.Volume)))
                    {
                        allPoints.Add(new OxyPlot.Series.ScatterPoint(candidate.Volume, candidate.RequiredSetups, 12));
                        pareto = false;
                        break;
                    }
                }

                if (pareto)
                {
                    paretoPoints.Add(new OxyPlot.Series.ScatterPoint(candidate.Volume, candidate.RequiredSetups, 12));
                    paretofront.Add(candidate);
                }
            }

            TestSearchGreedy2(vs, vd, out TimeSpan elapsedGreedy2, out Candidate Greedy2, out List<Candidate> G2Cands);
            Console.WriteLine("Performing Modified Greedy Search\nRequired Setups: {0}\n{1}", Greedy2, elapsedGreedy2);

            TestSearchGreedy(vs, vd, out TimeSpan elapsedGreedy, out Candidate Greedy, out List<Candidate> GCands);
            Console.WriteLine("Performing Greedy Search\nRequired Setups: {0}\n{1}", Greedy, elapsedGreedy);

            //TestSearchBFS(vs, vd, out TimeSpan elapsedBFS, out Candidate BFS, out List<Candidate> BFSCands);
            //Console.WriteLine("Performing Best-First-Search\nRequired Setups: {0}\n{1}", BFS, elapsedBFS);

            //TestSearch5Axis(vs, vd, out TimeSpan elapsed5Axis, out Candidate Axis5);
            //Console.WriteLine("Searching all 5-Axis Combinations\nRequired Setups: {0}\n{1}", Axis5, elapsed5Axis);

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
                if ((pc.RequiredSetups == 1) || (pc.RequiredSetups == 6)) { continue;}
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

            var truedictionary = new Dictionary<Boolean, string>(){{false, "No"}, {true, "Yes"}};
            var AllPlot = new PlotModel { Title = "Pareto: " + fn + "\nRequired Setups: " + reqdstps.ToString() +
                                                          "\nDoes Greedy search reach Pareto Front: " + truedictionary[gp] +
                                                          "\nDoes modified Greedy search reach Parteo Front: " +
                                                          truedictionary[g2p]};

            //var customAxis = new OxyPlot.Axes.RangeColorAxis { Key = "customColors" };
            //customAxis.AddRange(0, 0.1, OxyPlot.OxyColors.Black);
            //customAxis.AddRange(1, 1.1, OxyPlot.OxyColors.Red);
            //customAxis.AddRange(2, 2.1, OxyPlot.OxyColors.Blue);
            //customAxis.AddRange(3, 3.1, OxyPlot.OxyColors.Blue);
            //AllPlot.Axes.Add(customAxis);

            AllPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Number of Machining setups" });
            AllPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Part volume post-machining [in^3]" });

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

        public struct Candidate
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
        }
        public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
        {
            public int Compare(TKey x, TKey y)
            {
                var result = x.CompareTo(y);
                return result == 0 ? 1 : result;
            }
        }

        public static void TestSearchGreedy2(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
            out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            //Take the intersects of all six direcions, and of + and - directions individually (i.e. +x with -x, +y with -y, +z with -z
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
                    candidates[i+1].Sort((x, y) => x.Volume.CompareTo(y.Volume));
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
                    candidates[i+1].Sort((x, y) => x.Volume.CompareTo(y.Volume));
                }
                else if (i == 3)
                {
                    candidates.Add(new List<Candidate>(new Candidate[] { complete }));
                    break;
                }
                i++;
            }

            cd = candidates[i][0];
            cds = new List<Candidate>();
            i = 0;
            foreach (List<Candidate> c1 in candidates)
            {
                foreach (Candidate c2 in c1)
                {
                    cds.Add(c2);
                    i++;
                }
            }

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
        }

        public static void TestSearchGreedy(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
            out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        {
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            //Take the intersects of all six direcions, and of + and - directions individually (i.e. +x with -x, +y with -y, +z with -z
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
            i = 0;
            foreach (List<Candidate> c1 in candidates)
            {
                foreach (Candidate c2 in c1)
                {
                    cds.Add(c2);
                    i++;
                }
            }

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
        }

        //public static void TestSearchBFS(VoxelizedSolid vs, Dictionary<VoxelDirections, VoxelizedSolid> vd,
        //    out TimeSpan elapsed, out Candidate cd, out List<Candidate> cds)
        //{
        //    Stopwatch Stopwatch = new Stopwatch();
        //    Stopwatch.Start();

        //    //Take the intersects of all six direcions, and of + and - directions individually (i.e. +x with -x, +y with -y, +z with -z
        //    var complete = new Candidate(vd, vd.Keys.ToArray());
        //    var targetVolume = complete.Volume;
        //    var tol = targetVolume * 0.001;

        //    Stopwatch.Stop();
        //    elapsed = Stopwatch.Elapsed;
        //}

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

            foreach (List<int> combination in combinations)
            {
                var indices = Enumerable.Range(0, combination.Count).Where(i => combination[i] == 1).ToList();
                var vds = new List<VoxelDirections>();
                foreach (int index in indices) { vds.Add(directions[index]); }
                intersections.Add(new Candidate(vd, vds.ToArray()));
            }

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