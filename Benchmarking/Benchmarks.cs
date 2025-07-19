using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using StarMathLib;
using System;
using TVGL;

namespace Benchmarking
{
    public class Benchmarks
    {
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;
        static double r100 => 200.0 * r.NextDouble() - 100.0;
        private Vector3 q;
        private double[,] matrix;
        private ComplexNumber c1, c2;
        private double rise, run;

        [GlobalSetup]
        public void Setup()
        {
                var angle = Math.PI * r1;
                var radius = 0.1 + 4 * r.NextDouble();
                var run = radius * Math.Cos(angle);
                var rise = radius * Math.Sin(angle);
            }

        [Benchmark]
        public double ATan2() => Math.Atan2(rise, run);

        [Benchmark]
        public double PseudoAngle() => Global.Pseudoangle(run, rise);



    }

}
