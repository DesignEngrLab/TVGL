using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;


namespace TVGLUnitTestsAndBenchmarking
{
    public static class Voxels
    {
        public static void VoxelRowCompare()
        {
            var r = new Random();
            for (int i = 8191; i > 0; i =(int)(i*0.75))
            {
                var numtrials = 250;
                var swDense = new Stopwatch();
                var numBytesDense = 0;
                var swSparse = new Stopwatch();
                var numBytesSparse = 0;
                Console.WriteLine("i: " + 8 * i);
                for (int j = 0; j < numtrials; j++)
                {
                    var bytes = new byte[i];
                    //r.NextBytes(bytes);
                    var vD = new VoxelRowDense(i);
                    bytes.CopyTo(vD.values, 0);
                    var vS = VoxelizedSolid.CopyToSparse(vD);

                    //Console.WriteLine("dense: " + string.Join(',', vD.XIndices()));
                    //Console.WriteLine("spars: " + string.Join(',', vS.XIndices()));
                    var numRanges = 20;
                    var indices = Enumerable.Range(0, numRanges).Select(x => r.Next(8 * i)).OrderBy(x => x).ToArray();
                    var starts = indices.Take(numRanges / 2).OrderBy(x => r.Next()).ToArray();
                    var ends = indices.Skip(numRanges / 2).OrderBy(x => r.Next()).ToArray();
                    for (int k = 0; k < numRanges / 4; k += 2)
                    {
                        swDense.Start();
                        vD.TurnOnRange((ushort)starts[k], (ushort)ends[k]);
                        //vD[(ushort)indices[k]] = true;
                        //vD[(ushort)indices[k+1]] = true;
                        swDense.Stop();
                        swSparse.Start();
                        vS.TurnOnRange((ushort)starts[k], (ushort)ends[k]);
                        //vS[(ushort)indices[k]] = true;
                        //vS[(ushort)indices[k + 1]] = true;
                        swSparse.Stop();
                        if (!string.Join(',', vD.XIndices()).Equals(string.Join(',', vS.XIndices())))
                        {
                            Console.WriteLine("\n\n\n***** error *****\n\n");
                            Console.WriteLine("dense: " + string.Join(',', vD.XIndices()));
                            Console.WriteLine("spars: " + string.Join(',', vS.XIndices()));
                        }
                        swDense.Start();
                        vD.TurnOffRange((ushort)starts[k + 1], (ushort)ends[k + 1]);
                        swDense.Stop();
                        swSparse.Start();
                        vS.TurnOffRange((ushort)starts[k + 1], (ushort)ends[k + 1]);
                        swSparse.Stop();
                        if (!string.Join(',', vD.XIndices()).Equals(string.Join(',', vS.XIndices())))
                        {
                            Console.WriteLine("\n\n\n***** error *****\n\n");
                            Console.WriteLine("dense: " + string.Join(',', vD.XIndices()));
                            Console.WriteLine("spars: " + string.Join(',', vS.XIndices()));
                        }
                    }
                    for (int k = numRanges / 4; k < numRanges / 2; k += 2)
                    {
                        swDense.Start();
                        vD[(ushort)starts[k]] = true;
                        vD[(ushort)ends[k]] = true;
                        swDense.Stop();
                        swSparse.Start();
                        vS[(ushort)starts[k]] = true;
                        vS[(ushort)ends[k]] = true;
                        swSparse.Stop();
                        if (!string.Join(',', vD.XIndices()).Equals(string.Join(',', vS.XIndices())))
                        {
                            Console.WriteLine("\n\n\n***** error *****\n\n");
                            Console.WriteLine("dense: " + string.Join(',', vD.XIndices()));
                            Console.WriteLine("spars: " + string.Join(',', vS.XIndices()));
                        }
                        if (k + 1 >= numRanges / 2) continue;
                        swDense.Start();
                        vD[(ushort)starts[k + 1]] = false;
                        vD[(ushort)ends[k + 1]] = false;
                        swDense.Stop();
                        swSparse.Start();
                        vS[(ushort)starts[k + 1]] = false;
                        vS[(ushort)ends[k + 1]] = false;
                        swSparse.Stop();
                        if (!string.Join(',', vD.XIndices()).Equals(string.Join(',', vS.XIndices())))
                        {
                            Console.WriteLine("\n\n\n***** error *****\n\n");
                            Console.WriteLine("dense: " + string.Join(',', vD.XIndices()));
                            Console.WriteLine("spars: " + string.Join(',', vS.XIndices()));
                        }
                    }
                    numBytesDense += vD.values.Length;
                    numBytesSparse += 2 * vS.indices.Count;
                    //Console.WriteLine(vS.indices.Count/2);
                }
                Console.WriteLine("Dense: t = " + swDense.ElapsedTicks + "; s = " + numBytesDense / ((double)numtrials));
                Console.WriteLine("Sparse: t = " + swSparse.ElapsedTicks + "; s = " + numBytesSparse / ((double)numtrials));
            }
        }

        public static void TestVoxelization(DirectoryInfo dir)
        {
            Presenter.NVEnable();
            var random = new Random();
            var fileNames = dir.GetFiles("*.*"); //.OrderBy(x => random.Next()).ToArray();
                                                 //var fileNames = dir.GetFiles("*");
            for (var i = 1; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out TessellatedSolid ts);
                if (ts.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + ts.Errors.ToString());
                    continue;
                }
                //IO.Save(ts, dir + "/3_bananas");
                //Presenter.ShowAndHang(ts);
                var sw = Stopwatch.StartNew();
                Console.WriteLine("creating...");
                var vs = VoxelizedSolid.CreateFrom(ts, 80);
                vs.HasUniformColor = true;
                vs.SolidColor = new Color(KnownColors.Black);
                ts.SolidColor = new Color(100, 200, 100, 50);
                Console.WriteLine(sw.Elapsed.ToString());
                sw.Restart();
                Console.WriteLine("extruding...");
                //Presenter.ShowAndHang(vs.ConvertToTessellatedSolidRectilinear());
                //Presenter.ShowAndHang(new Solid[] { vs });
                //continue;
                var extrudeSolid = vs.DraftToNewSolid(CartesianDirections.YNegative);
                Console.WriteLine(sw.Elapsed.ToString());
                //Presenter.ShowAndHang(extrudeSolid.ConvertToTessellatedSolidRectilinear());
                Console.WriteLine("subtracting...");
                sw.Restart();
                vs.SolidColor = new Color(100, 20, 20, 250);

                var erode = VoxelizedSolid.MinkowskiSubtractOne(vs);
                erode.SolidColor = new Color(100, 20, 20, 250);

                var erodeNew = VoxelizedSolid.MinkowskiSubtractOneNew(vs);

                Console.WriteLine(sw.Elapsed.ToString());
                erodeNew.HasUniformColor = true;
                erodeNew.SolidColor = new Color(200, 250, 20, 20);
                Presenter.ShowAndHang(new[] { erode.ConvertToTessellatedSolidRectilinear(), erodeNew.ConvertToTessellatedSolidRectilinear() });

                var block = VoxelizedSolid.CreateFullBlock(extrudeSolid);
                (block, var _) = block.SliceOnPlane(new Plane(2, new Vector3(1, 1, 1)));
                Presenter.ShowAndHang(block.ConvertToTessellatedSolidRectilinear());
                Console.WriteLine(sw.Elapsed.ToString());

                //Presenter.ShowAndHang(extrudeSolid.ConvertToTessellatedSolidMarchingCubes(5));

                //Snapshot.Match(vs, SnapshotNameExtension.Create(name));
            }
        }


    }
}
