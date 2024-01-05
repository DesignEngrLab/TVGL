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
            for (int i = 8192; i > 0; i = (int)(i * 0.95))
            {
                var numtrials = 1000;
                var swDense = new Stopwatch();
                var numBytesDense = 0;
                var swSparse = new Stopwatch();
                var numBytesSparse = 0;
                for (int j = 0; j < numtrials; j++)
                {
                    var bytes = new byte[i];
                    //r.NextBytes(bytes);
                    var vD = new VoxelRowDense(i);
                    bytes.CopyTo(vD.values, 0);
                    var vS = VoxelizedSolid.CopyToSparse(vD);

                    //Console.WriteLine("dense: " + string.Join(',', vD.XIndices()));
                    //Console.WriteLine("spars: " + string.Join(',', vS.XIndices()));
                    var numRanges = 10;
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
                //Console.WriteLine("i: " + 8 * i+" Dense: t = " + swDense.ElapsedTicks + "; s = " + numBytesDense / ((double)numtrials));
                Console.WriteLine("" + 8 * i + "," + swDense.ElapsedTicks + "," + numBytesDense / ((double)numtrials));
                var avgNumBytesSparse = numBytesSparse / ((double)numtrials);
                var avgRanges = avgNumBytesSparse / 4;
                //Console.WriteLine("i: " + 8 * i + " Sparse: t = " + swSparse.ElapsedTicks + "; s = " + avgNumBytesSparse +" ("+avgRanges+")");
                Console.WriteLine(",,,,," + 8 * i + "," + swSparse.ElapsedTicks + "," + avgNumBytesSparse);
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
                var vs = VoxelizedSolid.CreateFrom(ts, 150);
                vs.HasUniformColor = true;
                vs.SolidColor = new Color(KnownColors.Black);
                ts.SolidColor = new Color(100, 200, 100, 50);
                //Console.WriteLine(sw.Elapsed.ToString());
                sw.Restart();
                //Console.WriteLine("extruding...");
                Presenter.ShowAndHang(vs.ConvertToTessellatedSolidRectilinear());
                //Presenter.ShowAndHang(new Solid[] { vs });
                //continue;
                var extrudeSolid = vs.DraftToNewSolid(CartesianDirections.YNegative);
                //Console.WriteLine(sw.Elapsed.ToString());
                Presenter.ShowAndHang(extrudeSolid.ConvertToTessellatedSolidRectilinear());
                vs.SolidColor = new Color(100, 20, 20, 250);

                sw.Restart();
                var erode = VoxelizedSolid.MinkowskiSubtractOne(vs);
                erode = VoxelizedSolid.MinkowskiSubtractOne(erode);
                erode = VoxelizedSolid.MinkowskiSubtractOne(erode);
                erode = VoxelizedSolid.MinkowskiSubtractOne(erode);
                Console.WriteLine("eroding old..." + sw.Elapsed.ToString());

                erode.SolidColor = new Color(100, 20, 20, 250);
                sw.Restart();
                Console.WriteLine("eroding new..." + sw.Elapsed.ToString());

                //if (erode.Equals(erodeNew)) Console.WriteLine("equal");
                //else throw new Exception();  //Console.WriteLine("not equal");
                //continue;
                Presenter.ShowAndHang(new[] { erode.ConvertToTessellatedSolidRectilinear() }); //, vs.ConvertToTessellatedSolidRectilinear() });

                var block = VoxelizedSolid.CreateFullBlock(extrudeSolid);
                (block, var _) = block.SliceOnPlane(new Plane(2, new Vector3(1, 1, 1)));
                Presenter.ShowAndHang(block.ConvertToTessellatedSolidRectilinear());
                Console.WriteLine(sw.Elapsed.ToString());

                //Presenter.ShowAndHang(extrudeSolid.ConvertToTessellatedSolidMarchingCubes(5));

                //Snapshot.Match(vs, SnapshotNameExtension.Create(name));
            }
        }

        public static void TestVoxelPrimitiveBoolOps()
        {
            Presenter.NVEnable();
            var vs = VoxelizedSolid.CreateFullBlock(0.08, new[] { Vector3.Zero, new Vector3(10, 10, 10) });
            var cyl1 = new Cylinder
            {
                Anchor = new Vector3(3, 5, 5),
                Axis = new Vector3(0, 0, 1),
                Radius = 2,
                MinDistanceAlongAxis = 5,
                MaxDistanceAlongAxis = 12,
                IsPositive = true
            };
            cyl1.Tessellate();
            foreach (var face in cyl1.Faces)
                face.Color = new Color(100, 250, 10, 10);
            var cyl2 = new Cylinder
            {
                Anchor = new Vector3(0, 5, 5),
                Axis = new Vector3(-.1, 0.1, 1).Normalize(),
                Radius = 2,
                MinDistanceAlongAxis = 5,
                MaxDistanceAlongAxis = 12,
                IsPositive = true
            };
            cyl2.Tessellate();
            foreach (var face in cyl2.Faces)
                face.Color = new Color(100, 10, 250, 10);
            var capsule = new Capsule(new Vector3(3, 3, 3), 3, new Vector3(2, 2, 15), 3, true);
            capsule.Tessellate();
            foreach (var face in capsule.Faces)
                face.Color = new Color(100, 10, 10, 250);
            capsule.Tessellate();

            //Presenter.ShowAndHang(vsTs.Faces.Concat(cyl1.Faces.Concat(cyl2.Faces.Concat(capsule.Faces))));
            vs.Subtract(new PrimitiveSurface[] {  cyl1 , capsule });//cyl1,
            var vsTs = vs.ConvertToTessellatedSolidRectilinear();
            foreach (var face in vsTs.Faces)
                face.Color = new Color(170, 100, 100, 100);
            Presenter.ShowAndHang(vsTs.Faces.Concat(cyl1.Faces.Concat(cyl2.Faces.Concat(capsule.Faces))));
            Presenter.ShowAndHang(vs.ConvertToTessellatedSolidRectilinear());
        }


    }
}
