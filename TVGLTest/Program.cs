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
using Constants = TVGL.Voxelization.Constants;

namespace TVGLPresenterDX
{
    internal class Program
    {
        static readonly  Stopwatch stopwatch = new Stopwatch();

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
            var fileNames = dir.GetFiles("*SquareSupportWithAdditionsForSegmentationTesting*").ToArray();
            //Casing = 18
            //SquareSupport = 75
            for (var i = 0; i < fileNames.Count(); i += 76)
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
                SearchComparison(ts);

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

        public static void SearchComparison(TessellatedSolid ts)
        {
            //TestSearchAll(ts, out TimeSpan elapsedAll,
            //    out int requiredSetupsAll, out List<VoxelDirections> manufacturingPlanAll);
            //Console.WriteLine("Searching all Possible Combinations of Setups\nRequired Setups: {0}\n{1}", requiredSetupsAll, elapsedAll);

            TestSearchGreedy2(ts, out TimeSpan elapsedGreedy2,
                out int requiredSetupsGreedy2, out List<VoxelDirections> manufacturingPlanGreedy2);
            Console.WriteLine("Performing Greedy Search\nRequired Setups: {0}\n{1}", requiredSetupsGreedy2, elapsedGreedy2);

            //TestSearch5Axis(ts, out TimeSpan elapsed5Axis,
            //    out int requiredSetups5Axis, out List<VoxelDirections> manufacturingPlan5Axis);
            //Console.WriteLine("Searching all 5-Axis Combinations\nRequired Setups: {0}\n{1}", requiredSetups5Axis, elapsed5Axis);
        }

        public class Candidate
        {
            public double Volume = 0;
            public int RequiredSetups = 0;
            public List<VoxelDirections> ManfacturingPlan = new List<VoxelDirections>();

            public Candidate(Dictionary<VoxelDirections, VoxelizedSolid> ex, params VoxelDirections[] vd)
            {
                var vs = ex[vd[0]];
                for (int i = 0; i < vd.Length; i++)
                {
                    vs = vs.IntersectToNewSolid(ex[vd[i]]);
                }
                Volume = vs.Volume;
                RequiredSetups = vd.Length;
                ManfacturingPlan.AddRange(vd);
            }
            public Candidate(Dictionary<VoxelDirections, VoxelizedSolid> ex, List<VoxelDirections> vd)
            {
                var vs = ex[vd[0]];
                for (int i = 0; i < vd.Count; i++)
                {
                    vs = vs.IntersectToNewSolid(ex[vd[i]]);
                }
                Volume = vs.Volume;
                RequiredSetups = vd.Count;
                ManfacturingPlan.AddRange(vd);
            }
            public Candidate(Candidate cn, Dictionary<VoxelDirections, VoxelizedSolid> ex, params VoxelDirections[] vd)
            {
                var vs = ex[cn.ManfacturingPlan[0]];
                for (int i = 0; i < cn.ManfacturingPlan.Count; i++)
                {
                    vs = vs.IntersectToNewSolid(ex[cn.ManfacturingPlan[i]]);
                }
                for (int i = 0; i < vd.Length; i++)
                {
                    vs.IntersectToNewSolid(ex[vd[i]]);
                }
                Volume = vs.Volume;
                RequiredSetups = cn.RequiredSetups + vd.Length;
                ManfacturingPlan = cn.ManfacturingPlan;
                ManfacturingPlan.AddRange(vd);
            }

            public void AddStep(Dictionary<VoxelDirections, VoxelizedSolid> ex, VoxelDirections vd)
            {
                var vs = ex[ManfacturingPlan[0]];
                for (int i = 0; i < ManfacturingPlan.Count; i++)
                {
                    vs = vs.IntersectToNewSolid(ex[ManfacturingPlan[i]]);
                }
                vs = vs.IntersectToNewSolid(ex[vd]);
                Volume = vs.Volume;
                RequiredSetups++;
                ManfacturingPlan.Add(vd);
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

        public static void TestSearchGreedy2(TessellatedSolid ts, out TimeSpan elapsed,
            out int requiredSetups, out List<VoxelDirections> manufacturingPlan)
        {
            //Convert tesselated solid to voxelized solid
            var vs1 = new VoxelizedSolid(ts, 8);
            //Perform extrusions in all six directions
            var extrusions = new Dictionary<VoxelDirections, VoxelizedSolid>()
                {
                    { VoxelDirections.XNegative, vs1.ExtrudeToNewSolid(VoxelDirections.XNegative) },
                    { VoxelDirections.XPositive, vs1.ExtrudeToNewSolid(VoxelDirections.XPositive) },
                    { VoxelDirections.YNegative, vs1.ExtrudeToNewSolid(VoxelDirections.YNegative) },
                    { VoxelDirections.YPositive, vs1.ExtrudeToNewSolid(VoxelDirections.YPositive) },
                    { VoxelDirections.ZNegative, vs1.ExtrudeToNewSolid(VoxelDirections.ZNegative) },
                    { VoxelDirections.ZPositive, vs1.ExtrudeToNewSolid(VoxelDirections.ZPositive) }
                };

            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            //Take the intersects of all six direcions, and of + and - directions individually (i.e. +x with -x, +y with -y, +z with -z
            var complete = new Candidate(extrusions, extrusions.Keys.ToList());
            var targetVolume = complete.Volume;
            var candidates = new List<List<Candidate>>
            {
                new List<Candidate>(new Candidate[]
                {
                    new Candidate(extrusions, VoxelDirections.XNegative, VoxelDirections.XPositive),
                    new Candidate(extrusions, VoxelDirections.YNegative, VoxelDirections.YPositive),
                    new Candidate(extrusions, VoxelDirections.ZNegative, VoxelDirections.ZPositive)
                })
            };
            candidates[0].Sort((x, y) => x.Volume.CompareTo(y.Volume));

            int i = 0;
            while (Math.Abs(candidates[i][0].Volume - targetVolume) > 0.01)
            {
                Console.WriteLine("{0}", i);
                if (i < 2)
                {
                    candidates.Add(new List<Candidate>(new Candidate[]
                        {
                            new Candidate(candidates[i][0], extrusions, candidates[0][i+1].ManfacturingPlan[0]),
                            new Candidate(candidates[i][0], extrusions, candidates[0][i+1].ManfacturingPlan[1])
                        }));
                    candidates[i+1].Sort((x, y) => x.Volume.CompareTo(y.Volume));
                }
                else if (i == 2)
                {
                    candidates.Add(new List<Candidate>(new Candidate[]
                        {
                            new Candidate(candidates[i][0], extrusions, candidates[1][1].ManfacturingPlan[
                                candidates[1][1].ManfacturingPlan.Count-1]),
                            new Candidate(candidates[i][0], extrusions, candidates[2][1].ManfacturingPlan[
                                candidates[2][1].ManfacturingPlan.Count-1])
                        }));
                    candidates[i+1].Sort((x, y) => x.Volume.CompareTo(y.Volume));
                }
                else if (i == 3)
                {
                    candidates.Add(new List<Candidate>(new Candidate[] { complete }));
                }
                else { break; }
                i++;
            }

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
            requiredSetups = candidates[i][0].RequiredSetups;
            manufacturingPlan = candidates[i][0].ManfacturingPlan;
            return;
        }
        //public static void TestSearchGreedy(TessellatedSolid ts, out TimeSpan elapsed,
        //    out int requiredSetups, out List<VoxelDirections> manufacturingPLan)
        //{
        //    //Convert tesselated solid to voxelized solid
        //    var vs1 = new VoxelizedSolid(ts, 8);
        //    //Perform extrusions in all six directions
        //    var extrusions = new Dictionary<VoxelDirections, VoxelizedSolid>()
        //        {
        //            { VoxelDirections.XNegative, vs1.ExtrudeToNewSolid(VoxelDirections.XNegative) },
        //            { VoxelDirections.XPositive, vs1.ExtrudeToNewSolid(VoxelDirections.XPositive) },
        //            { VoxelDirections.YNegative, vs1.ExtrudeToNewSolid(VoxelDirections.YNegative) },
        //            { VoxelDirections.YPositive, vs1.ExtrudeToNewSolid(VoxelDirections.YPositive) },
        //            { VoxelDirections.ZNegative, vs1.ExtrudeToNewSolid(VoxelDirections.ZNegative) },
        //            { VoxelDirections.ZPositive, vs1.ExtrudeToNewSolid(VoxelDirections.ZPositive) }
        //        };
        //}
        //public static void TestSearchBF(TessellatedSolid ts, out TimeSpan elapsed,
        //    out int requiredSetups, out List<VoxelDirections> manufacturingPLan)
        //{
        //    //Convert tesselated solid to voxelized solid
        //    var vs1 = new VoxelizedSolid(ts, 8);
        //    //Perform extrusions in all six directions
        //    var extrusions = new Dictionary<VoxelDirections, VoxelizedSolid>()
        //        {
        //            { VoxelDirections.XNegative, vs1.ExtrudeToNewSolid(VoxelDirections.XNegative) },
        //            { VoxelDirections.XPositive, vs1.ExtrudeToNewSolid(VoxelDirections.XPositive) },
        //            { VoxelDirections.YNegative, vs1.ExtrudeToNewSolid(VoxelDirections.YNegative) },
        //            { VoxelDirections.YPositive, vs1.ExtrudeToNewSolid(VoxelDirections.YPositive) },
        //            { VoxelDirections.ZNegative, vs1.ExtrudeToNewSolid(VoxelDirections.ZNegative) },
        //            { VoxelDirections.ZPositive, vs1.ExtrudeToNewSolid(VoxelDirections.ZPositive) }
        //        };
        //}
        public static void TestSearchAll(TessellatedSolid ts, out TimeSpan elapsed,
            out int requiredSetups, out List<VoxelDirections> manufacturingPlan)
        {
            //Convert tesselated solid to voxelized solid
            var vs1 = new VoxelizedSolid(ts, 8);
            //Perform extrusions in all six directions
            var extrusions = new Dictionary<VoxelDirections, VoxelizedSolid>()
                {
                    { VoxelDirections.XNegative, vs1.ExtrudeToNewSolid(VoxelDirections.XNegative) },
                    { VoxelDirections.XPositive, vs1.ExtrudeToNewSolid(VoxelDirections.XPositive) },
                    { VoxelDirections.YNegative, vs1.ExtrudeToNewSolid(VoxelDirections.YNegative) },
                    { VoxelDirections.YPositive, vs1.ExtrudeToNewSolid(VoxelDirections.YPositive) },
                    { VoxelDirections.ZNegative, vs1.ExtrudeToNewSolid(VoxelDirections.ZNegative) },
                    { VoxelDirections.ZPositive, vs1.ExtrudeToNewSolid(VoxelDirections.ZPositive) }
                };

            var directions = new List<VoxelDirections>(new VoxelDirections[]
            {
                VoxelDirections.XNegative,
                VoxelDirections.XPositive,
                VoxelDirections.YNegative,
                VoxelDirections.YPositive,
                VoxelDirections.ZNegative,
                VoxelDirections.ZPositive
            });

            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var complete = new Candidate(extrusions, directions);

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

            requiredSetups = complete.RequiredSetups;
            combinations.RemoveAt(63);
            combinations.RemoveAt(0);
            var intersections = new List<Candidate>(63) { complete };

            foreach (List<int> combination in combinations)
            {
                var indices = Enumerable.Range(0, combination.Count).Where(i => combination[i] == 1).ToList();
                var keys = new List<VoxelDirections>();
                foreach (int index in indices) { directions.Add(keys[index]); }
                intersections.Add(new Candidate(extrusions, keys));
            }

            intersections.Sort((x, y) => x.Volume.CompareTo(y.Volume));
            var bests = intersections.FindAll(delegate(Candidate inter) { return Math.Abs(inter.Volume - intersections[0].Volume) < 0.01; });
            bests.Sort((x, y) => x.RequiredSetups.CompareTo(y.RequiredSetups));            

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
            requiredSetups = bests[0].RequiredSetups;
            manufacturingPlan = bests[0].ManfacturingPlan;
        }

        public static void TestSearch5Axis(TessellatedSolid ts, out TimeSpan elapsed,
            out int requiredSetups, out List<VoxelDirections> manufacturingPlan)
        {
            //Convert tesselated solid to voxelized solid
            var vs1 = new VoxelizedSolid(ts, 8);

            //Perform extrusions in all six directions
            var extrusions = new Dictionary<VoxelDirections, VoxelizedSolid>()
                {
                    { VoxelDirections.XNegative, vs1.ExtrudeToNewSolid(VoxelDirections.XNegative) },
                    { VoxelDirections.XPositive, vs1.ExtrudeToNewSolid(VoxelDirections.XPositive) },
                    { VoxelDirections.YNegative, vs1.ExtrudeToNewSolid(VoxelDirections.YNegative) },
                    { VoxelDirections.YPositive, vs1.ExtrudeToNewSolid(VoxelDirections.YPositive) },
                    { VoxelDirections.ZNegative, vs1.ExtrudeToNewSolid(VoxelDirections.ZNegative) },
                    { VoxelDirections.ZPositive, vs1.ExtrudeToNewSolid(VoxelDirections.ZPositive) }
                };
            Stopwatch Stopwatch = new Stopwatch();
            Stopwatch.Start();

            var setups = new List<Candidate>(7)
            {
                new Candidate(extrusions, VoxelDirections.XNegative, VoxelDirections.XPositive,
                VoxelDirections.YNegative, VoxelDirections.YPositive, VoxelDirections.ZNegative),

                new Candidate(extrusions, VoxelDirections.XNegative, VoxelDirections.XPositive,
                VoxelDirections.YNegative, VoxelDirections.YPositive, VoxelDirections.ZPositive),

                new Candidate(extrusions, VoxelDirections.XNegative, VoxelDirections.XPositive,
                VoxelDirections.YNegative, VoxelDirections.ZNegative, VoxelDirections.ZPositive),

                new Candidate(extrusions, VoxelDirections.XNegative, VoxelDirections.XPositive,
                VoxelDirections.YPositive, VoxelDirections.ZNegative, VoxelDirections.ZPositive),

                new Candidate(extrusions, VoxelDirections.XNegative, VoxelDirections.YNegative,
                VoxelDirections.YPositive, VoxelDirections.ZNegative, VoxelDirections.ZPositive),

                new Candidate(extrusions, VoxelDirections.XPositive, VoxelDirections.YNegative,
                VoxelDirections.YPositive, VoxelDirections.ZNegative, VoxelDirections.ZPositive),
            };

            setups.Sort((x, y) => x.Volume.CompareTo(y.Volume));

            Stopwatch.Stop();
            elapsed = Stopwatch.Elapsed;
            requiredSetups = setups[0].RequiredSetups;
            manufacturingPlan = setups[0].ManfacturingPlan;
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