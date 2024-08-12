using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using System;
using TVGL;

namespace Benchmarking
{
    public class Benchmarks
    {
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;
        private Vector3 q;

        [GlobalSetup]
        public void Setup()
        {
            q = new Vector3(r1, r1, r1);
        }

        [Benchmark]
        public Matrix4x4 TransformXYOld() => q.TransformToXYPlane(out _);

        [Benchmark]
        public Matrix4x4 TransformXYNew() => q.TransformToXYPlane(out _);



    }

}
