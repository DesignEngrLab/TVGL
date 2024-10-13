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

        [GlobalSetup]
        public void Setup()
        {

             c1 = new ComplexNumber(r100, r100);
             c2 = new ComplexNumber(r100, r100);
            //var ans1 = StarMath.ComplexNumberDivide(c1.Real, c1.Imaginary, c2.Real, c2.Imaginary);
            var ans2 = c1 / c2;
        }

        //[Benchmark]
        //public double[] OLD() => StarMath.ComplexNumberDivide(c1.Real, c1.Imaginary, c2.Real, c2.Imaginary);

        [Benchmark]
        public ComplexNumber NEW() => c1 / c2;



    }

}
